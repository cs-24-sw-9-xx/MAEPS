using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

using Maes.Map;
using Maes.Map.Generators;
using Maes.Robot;
using Maes.Simulation.Patrolling;
using Maes.Statistics.Snapshots;
using Maes.UI.Visualizers.Patrolling;
using Maes.UI.Visualizers.Patrolling.VisualizationModes;

using UnityEngine;

using ThreadPriority = System.Threading.ThreadPriority;

namespace Maes.Statistics.Trackers
{
    public sealed class PatrollingTracker : Tracker<PatrollingVisualizer, IPatrollingVisualizationMode>
    {
        public int WorstGraphIdleness { get; private set; }

        public float TotalDistanceTraveled { get; private set; }

        public float CurrentGraphIdleness { get; private set; }

        public float AverageGraphIdleness => _totalGraphIdleness / CurrentTick;

        public int CurrentCycle { get; private set; }

        public float? AverageGraphDiffLastTwoCyclesProportion { get; private set; }

        public int TotalCycles { get; }

        private readonly PatrollingSimulation _simulation;

        private readonly VertexDetails[] _vertices;
        private VertexVisualizer? _selectedVertex;

        private float _totalGraphIdleness;
        private float _lastCyclesTotalGraphIdleness;
        private int _lastAmountOfTicksSinceLastCycle;
        private float _lastCycleAverageGraphIdleness;
        private int _lastCycle;

        private readonly ArrayPool<WaypointSnapshot>? _waypointSnapshotPool;
        private readonly BlockingCollection<(PatrollingSnapshot, WaypointSnapshot[])> _snapshots = new();

        private readonly CsvDataWriter<PatrollingSnapshot> _patrollingSnapshotWriter;
        private readonly CsvDataWriter<WaypointSnapshot>[]? _waypointSnapShots;
        private readonly bool _saveWaypointData;

        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly Thread _writerThread;

        public PatrollingTracker(PatrollingSimulation simulation, SimulationMap<Tile> collisionMap,
            PatrollingVisualizer visualizer, PatrollingSimulationScenario scenario,
            PatrollingMap map, string statisticsFolderPath, bool saveWaypointData) : base(collisionMap, visualizer, scenario.RobotConstraints,
            tile => new Cell(isExplorable: !Tile.IsWall(tile.Type)), simulation.CommunicationManager)
        {
            _saveWaypointData = saveWaypointData;
            _simulation = simulation;
            _vertices = new VertexDetails[map.Vertices.Count];
            for (var i = 0; i < _vertices.Length; i++)
            {
                _vertices[i] = new VertexDetails(map.Vertices[i]);
                Debug.Assert(_vertices[i].Vertex.Id == i);
            }


            _visualizer.CreateVisualizers(_vertices, map);
            _visualizer.SetCommunicationZoneVertices(collisionMap, map, simulation.CommunicationManager);
            TotalCycles = scenario.TotalCycles;

            _visualizer.meshRenderer.enabled = false;
            _currentVisualizationMode = new NoneVisualizationMode();

            Directory.CreateDirectory(statisticsFolderPath);
            var patrollingFilename = Path.Join(statisticsFolderPath, "patrolling");
            _patrollingSnapshotWriter = new CsvDataWriter<PatrollingSnapshot>(patrollingFilename);

            if (saveWaypointData)
            {
                _waypointSnapshotPool = ArrayPool<WaypointSnapshot>.Create();
                var waypointFolderPath = Path.Join(statisticsFolderPath, "waypoints/");
                Directory.CreateDirectory(waypointFolderPath);

                _waypointSnapShots = _vertices.Select(v =>
                    new CsvDataWriter<WaypointSnapshot>(Path.Join(waypointFolderPath,
                        $"{v.Vertex.Position.x}_{v.Vertex.Position.y}"))).ToArray();
            }

            _writerThread = new Thread(WriterThread) { Priority = ThreadPriority.BelowNormal };
            _writerThread.Start(_cancellationTokenSource.Token); ;
        }

        private void WriterThread(object cancellationTokenObject)
        {
            var cancellationToken = (CancellationToken)cancellationTokenObject;
            try
            {
                while (true)
                {
                    var (patrollingSnapshot, waypointSnapshots) = _snapshots.Take(cancellationToken);

                    _patrollingSnapshotWriter.AddRecord(patrollingSnapshot);

                    if (!_saveWaypointData)
                    {
                        continue;
                    }

                    for (var i = 0; i < _vertices.Length; i++)
                    {
                        _waypointSnapShots![i].AddRecord(waypointSnapshots[i]);
                    }

                    _waypointSnapshotPool!.Return(waypointSnapshots, clearArray: false);
                }
            }
            catch (InvalidOperationException)
            {
                // BlockingCollection.Take throws this exception when CompleteAdding() has been called and there are no items in the collection.
                // What a stupid exception to throw.

                // Well we are done now so finish up.
                _patrollingSnapshotWriter.Finish();
                _patrollingSnapshotWriter.Dispose();

                if (_saveWaypointData)
                {
                    foreach (var csvWriter in _waypointSnapShots!)
                    {
                        csvWriter.Finish();
                        csvWriter.Dispose();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // We got cancelled on twitter.
                // Don't write the statistics as retaliation.
                _patrollingSnapshotWriter.Dispose();

                if (_saveWaypointData)
                {
                    foreach (var csvWriter in _waypointSnapShots!)
                    {
                        csvWriter.Dispose();
                    }
                }
            }
        }

        public void OnReachedVertex(int vertexId)
        {
            var vertexDetails = _vertices[vertexId];

            var atTick = _simulation.SimulatedLogicTicks;

            var idleness = atTick - vertexDetails.Vertex.LastTimeVisitedTick;
            vertexDetails.MaxIdleness = Mathf.Max(vertexDetails.MaxIdleness, idleness);
            vertexDetails.Vertex.VisitedAtTick(atTick);

            WorstGraphIdleness = Mathf.Max(WorstGraphIdleness, vertexDetails.MaxIdleness);
            CurrentCycle = _vertices.Select(v => v.Vertex.NumberOfVisits).Min();

            if (_currentVisualizationMode is PatrollingTargetWaypointVisualizationMode)
            {
                _visualizer.ShowDefaultColor(vertexDetails.Vertex);
            }
        }

        public override void FinishStatistics()
        {
            _snapshots.CompleteAdding();
            _writerThread.Join();
        }

        protected override void OnLogicUpdate(List<MonaRobot> robots)
        {
            SetTotalDistanceTraveled(robots);
            var worstGraphIdleness = 0;
            var graphIdlenessSum = 0;
            foreach (var vertex in _vertices)
            {
                var idleness = CurrentTick - vertex.Vertex.LastTimeVisitedTick;
                if (worstGraphIdleness < idleness)
                {
                    worstGraphIdleness = idleness;
                }

                graphIdlenessSum += idleness;
            }

            WorstGraphIdleness = worstGraphIdleness;
            CurrentGraphIdleness = (float)graphIdlenessSum / _vertices.Length;
            _totalGraphIdleness += CurrentGraphIdleness;

            if (_lastCycle != CurrentCycle)
            {
                var lastTick = CurrentTick - _lastAmountOfTicksSinceLastCycle;
                var totalGraphIdlenessCycle = _totalGraphIdleness - _lastCyclesTotalGraphIdleness;
                var averageGraphIdlenessCycle = totalGraphIdlenessCycle / lastTick;

                if (CurrentCycle > 1)
                {
                    var cycleAvg = Math.Abs(_lastCycleAverageGraphIdleness - averageGraphIdlenessCycle) /
                                   _lastCycleAverageGraphIdleness;
                    AverageGraphDiffLastTwoCyclesProportion = cycleAvg;
                }

                _lastCycle = CurrentCycle;
                _lastCyclesTotalGraphIdleness = _totalGraphIdleness;
                _lastAmountOfTicksSinceLastCycle = CurrentTick;
                _lastCycleAverageGraphIdleness = averageGraphIdlenessCycle;
            }
        }

        protected override void UpdateCoverageStatus(MonaRobot robot)
        {
            // Don't do anything
        }

        private void SetTotalDistanceTraveled(List<MonaRobot> robots)
        {
            float sum = 0;
            foreach (var robot in robots)
            {
                sum += robot.Controller.TotalDistanceTraveled;
            }

            TotalDistanceTraveled = sum;
        }

        public override void SetVisualizedRobot(MonaRobot? robot)
        {
            _selectedRobot = robot;
        }

        protected override void CreateSnapShot(CommunicationSnapshot communicationSnapshot)
        {
            var patrollingSnapshot =
                new PatrollingSnapshot(communicationSnapshot, CurrentGraphIdleness, WorstGraphIdleness,
                    TotalDistanceTraveled, AverageGraphIdleness, CurrentCycle, _simulation.NumberOfActiveRobots);

            var waypointSnapshots = Array.Empty<WaypointSnapshot>();
            if (_saveWaypointData)
            {
                waypointSnapshots = _waypointSnapshotPool!.Rent(_vertices.Length);

                for (var i = 0; i < _vertices.Length; i++)
                {
                    var vertex = _vertices[i].Vertex;
                    waypointSnapshots[i] = new WaypointSnapshot(CurrentTick, CurrentTick - vertex.LastTimeVisitedTick,
                        vertex.NumberOfVisits);
                }
            }

            _snapshots.Add((patrollingSnapshot, waypointSnapshots));
        }

        protected override void SetVisualizationMode(IPatrollingVisualizationMode newMode)
        {
            _visualizer.ResetWaypointsColor();
            _visualizer.ResetRobotHighlighting(_simulation.Robots, _selectedRobot);
            _visualizer.ResetCellColor();
            base.SetVisualizationMode(newMode);
        }

        public void ShowWaypointHeatMap()
        {
            _visualizer.meshRenderer.enabled = false;
            SetVisualizationMode(new WaypointHeatMapVisualizationMode());
        }

        public void ShowNone()
        {
            _visualizer.meshRenderer.enabled = false;
            SetVisualizationMode(new NoneVisualizationMode());
        }

        public void ShowAllRobotsHighlighting()
        {
            _selectedRobot = null;
            _visualizer.meshRenderer.enabled = false;
            SetVisualizationMode(new AllRobotsHighlightingVisualizationMode(_simulation.Robots));
            SetRobotHighlightingSize(4f);
        }

        private void SetRobotHighlightingSize(float highlightingSize)
        {
            foreach (var robot in _simulation.Robots)
            {
                robot.outLine.OutlineWidth = highlightingSize;
            }
        }

        public void ShowTargetWaypointSelected()
        {
            _visualizer.meshRenderer.enabled = false;
            if (_selectedRobot == null)
            {
                throw new Exception(
                    "Cannot change to 'ShowTargetWaypointSelected' Visualization mode when no robot is selected");
            }

            SetVisualizationMode(new PatrollingTargetWaypointVisualizationMode(_selectedRobot));
        }

        public void ShowSelectedRobotCommunicationRange()
        {
            if (_selectedRobot == null)
            {
                Debug.Log("Cannot show robot communication range when no robot is selected");
                return;
            }

            _visualizer.meshRenderer.enabled = true;
            SetVisualizationMode(new SelectedRobotCommunicationRangeVisualizationMode(_selectedRobot));
        }

        public void ShowCommunicationZone()
        {
            if (_selectedVertex == null)
            {
                Debug.Log("Cannot show communication zone when no vertex is selected");
                return;
            }

            _visualizer.meshRenderer.enabled = true;
            SetVisualizationMode(
                new CommunicationZoneVisualizationMode(_selectedVertex.VertexDetails.Vertex.Id));
        }

        public void ShowSelectedRobotVerticesColors()
        {
            if (_selectedRobot == null)
            {
                Debug.Log("Cannot show partitioning highlighting when no robot is selected");
                return;
            }

            _visualizer.meshRenderer.enabled = false;
            SetVisualizationMode(new SelectedRobotShowVerticesColorsVisualizationMode(_selectedRobot));
        }

        public void ShowAllRobotsVerticesColors()
        {
            _visualizer.meshRenderer.enabled = false;
            SetVisualizationMode(new AllRobotsShowVerticesColorsVisualizationMode(_simulation.Robots));
        }

        public void SetVisualizedVertex(VertexVisualizer? newSelectedVertex)
        {
            _selectedVertex = newSelectedVertex;
            if (_selectedVertex != null)
            {
                ShowCommunicationZone();
            }
            else
            {
                // Revert to none visualization when vertex is deselected
                ShowNone();
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _cancellationTokenSource.Cancel();
                _writerThread.Join();
                _cancellationTokenSource.Dispose();
                _snapshots.Dispose();
            }
        }
    }
}