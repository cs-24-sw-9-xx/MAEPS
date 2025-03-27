using System;
using System.Collections.Generic;
using System.Linq;

using Maes.Map;
using Maes.Map.Generators;
using Maes.Robot;
using Maes.Simulation.Patrolling;
using Maes.Statistics.Patrolling;
using Maes.UI.Visualizers.Patrolling;
using Maes.UI.Visualizers.Patrolling.VisualizationModes;

using UnityEngine;

using XCharts.Runtime;

namespace Maes.Statistics.Trackers
{
    public sealed class PatrollingTracker : Tracker<PatrollingVisualizer, IPatrollingVisualizationMode>
    {
        private PatrollingSimulation Simulation { get; }
        public int WorstGraphIdleness { get; private set; }

        // TODO: TotalDistanceTraveled is not set any where in the code, don't know how to calculate it yet
        public float TotalDistanceTraveled { get; private set; }

        public float CurrentGraphIdleness { get; private set; }

        public float AverageGraphIdleness => _totalGraphIdleness / CurrentTick;

        public int CurrentCycle { get; private set; }

        public float? AverageGraphDiffLastTwoCyclesProportion { get; private set; }

        public BaseChart Chart { get; set; } = null!;

        public DataZoom Zoom { get; set; } = null!;

        public int PlottingFrequency = 50;

        private int _lastPlottedSnapshot;

        //TODO: TotalCycles is not set any where in the code
        public int TotalCycles { get; }

        public readonly List<PatrollingSnapShot> SnapShots = new();
        public readonly Dictionary<Vector2Int, List<WaypointSnapShot>> WaypointSnapShots;

        private readonly Dictionary<int, VertexDetails> _vertices;
        private VertexVisualizer? _selectedVertex;

        private float _totalGraphIdleness;
        private float _lastCyclesTotalGraphIdleness;
        private int _lastAmountOfTicksSinceLastCycle;
        private float _lastCycleAverageGraphIdleness;
        private int _lastCycle;
        private readonly SimulationMap<Tile> _collisionMap;

        public PatrollingTracker(PatrollingSimulation simulation, SimulationMap<Tile> collisionMap,
            PatrollingVisualizer visualizer, PatrollingSimulationScenario scenario,
            PatrollingMap map) : base(collisionMap, visualizer, scenario.RobotConstraints,
            tile => new Cell(isExplorable: !Tile.IsWall(tile.Type)))
        {
            Simulation = simulation;
            _vertices = map.Vertices.ToDictionary(vertex => vertex.Id, vertex => new VertexDetails(vertex));
            _visualizer.CreateVisualizers(_vertices, map);
            _visualizer.SetCommunicationZoneVertices(collisionMap, map, simulation.CommunicationManager);
            TotalCycles = scenario.TotalCycles;
            WaypointSnapShots =
                _vertices.Values.ToDictionary(k => k.Vertex.Position, _ => new List<WaypointSnapShot>());

            _visualizer.meshRenderer.enabled = false;
            _currentVisualizationMode = new NoneVisualizationMode();
            _collisionMap = collisionMap;
        }

        public void OnReachedVertex(int vertexId, int atTick)
        {
            if (!_vertices.TryGetValue(vertexId, out var vertexDetails))
            {
                throw new InvalidOperationException("Invalid vertex");
            }

            var idleness = atTick - vertexDetails.Vertex.LastTimeVisitedTick;
            vertexDetails.MaxIdleness = Mathf.Max(vertexDetails.MaxIdleness, idleness);
            vertexDetails.Vertex.VisitedAtTick(atTick);

            WorstGraphIdleness = Mathf.Max(WorstGraphIdleness, vertexDetails.MaxIdleness);
            CurrentCycle = _vertices.Values.Select(v => v.Vertex.NumberOfVisits).Min();

            if (_currentVisualizationMode is PatrollingTargetWaypointVisualizationMode)
            {
                _visualizer.ShowDefaultColor(vertexDetails.Vertex);
            }
        }

        // Hack: Cursed way of updating ui using unitys update event.
        public override void UIUpdate()
        {
            base.UIUpdate();
            // TODO: Fix graph data limit.
            if (Chart != null && Chart.gameObject.activeSelf && SnapShots.Count > 0)
            {
                //Update zoom to only follow the most recent data.
                if (Zoom.start < 50 && Chart.series[0].data.Count >= 200)
                {
                    Zoom.start = 50;
                    Zoom.end = 100;
                }

                for (var i = _lastPlottedSnapshot; i < SnapShots.Count; i++)
                {
                    if (SnapShots[i].Tick % PlottingFrequency == 0)
                    {
                        Chart.AddData(0, SnapShots[i].Tick, SnapShots[i].WorstGraphIdleness);
                        Chart.AddData(1, SnapShots[i].Tick, SnapShots[i].GraphIdleness);
                        Chart.AddData(2, SnapShots[i].Tick, SnapShots[i].AverageGraphIdleness);
                        Chart.AddData(3, SnapShots[i].Tick, SnapShots[i].TotalDistanceTraveled);
                    }
                }

                _lastPlottedSnapshot = SnapShots.Count;
                Chart.RefreshDataZoom();
            }
            else
            {
                _lastPlottedSnapshot = 0;
            }
        }

        protected override void OnLogicUpdate(List<MonaRobot> robots)
        {
            SetTotalDistanceTraveled(robots);
            var worstGraphIdleness = 0;
            var graphIdlenessSum = 0;
            foreach (var vertex in _vertices)
            {
                var idleness = CurrentTick - vertex.Value.Vertex.LastTimeVisitedTick;
                if (worstGraphIdleness < idleness)
                {
                    worstGraphIdleness = idleness;
                }

                graphIdlenessSum += idleness;
            }

            WorstGraphIdleness = worstGraphIdleness;
            CurrentGraphIdleness = (float)graphIdlenessSum / _vertices.Count;
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
                    Debug.Log($"Average Graph Diff Last Two Cycles Proportion: {cycleAvg}");
                    AverageGraphDiffLastTwoCyclesProportion = cycleAvg;
                }

                _lastCycle = CurrentCycle;
                _lastCyclesTotalGraphIdleness = _totalGraphIdleness;
                _lastAmountOfTicksSinceLastCycle = CurrentTick;
                _lastCycleAverageGraphIdleness = averageGraphIdlenessCycle;
            }
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

        protected override void CreateSnapShot()
        {
            SnapShots.Add(new PatrollingSnapShot(CurrentTick, CurrentGraphIdleness, WorstGraphIdleness,
                TotalDistanceTraveled, AverageGraphIdleness, CurrentCycle, Simulation.NumberOfActiveRobots));

            foreach (var vertex in _vertices.Values)
            {
                WaypointSnapShots[vertex.Vertex.Position].Add(new WaypointSnapShot(CurrentTick,
                    CurrentTick - vertex.Vertex.LastTimeVisitedTick, vertex.Vertex.NumberOfVisits));
            }
        }

        protected override void SetVisualizationMode(IPatrollingVisualizationMode newMode)
        {
            _visualizer.ResetWaypointsColor();
            _visualizer.ResetRobotHighlighting(Simulation.Robots, _selectedRobot);
            _visualizer.ResetCellColor();
            base.SetVisualizationMode(newMode);
        }

        public void ShowWaypointHeatMap()
        {
            _visualizer.meshRenderer.enabled = false;
            SetVisualizationMode(new WaypointHeatMapVisualizationMode());
        }

        public void ShowAllRobotCoverageHeatMap()
        {
            _visualizer.meshRenderer.enabled = true;
            SetVisualizationMode(new PatrollingCoverageHeatMapVisualizationMode(_map));
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
            SetVisualizationMode(new AllRobotsHighlightingVisualizationMode(Simulation.Robots));
            SetRobotHighlightingSize(4f);
        }

        private void SetRobotHighlightingSize(float highlightingSize)
        {
            foreach (var robot in Simulation.Robots)
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
            SetVisualizationMode(new SelectedRobotCommunicationRangeVisualizationMode(_selectedRobot, _collisionMap));
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
                new CommunicationZoneVisualizationMode(_visualizer, _selectedVertex.VertexDetails.Vertex.Id));
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
            SetVisualizationMode(new AllRobotsShowVerticesColorsVisualizationMode(Simulation.Robots));
        }

        public void InitIdleGraph()
        {
            Chart.RemoveData();
            var xAxis = Chart.EnsureChartComponent<XAxis>();
            xAxis.splitNumber = 10;
            xAxis.minMaxType = Axis.AxisMinMaxType.MinMaxAuto;
            xAxis.type = Axis.AxisType.Value;

            var yAxis = Chart.EnsureChartComponent<YAxis>();
            yAxis.splitNumber = 10;
            yAxis.type = Axis.AxisType.Value;
            yAxis.minMaxType = Axis.AxisMinMaxType.MinMaxAuto;

            var worstIdlenessSeries = Chart.AddSerie<Line>("Worst");
            worstIdlenessSeries.symbol.size = 2;

            var currentIdlenessSeries = Chart.AddSerie<Line>("Current");
            currentIdlenessSeries.symbol.size = 2;

            var averageIdlenessSeries = Chart.AddSerie<Line>("Average");
            averageIdlenessSeries.symbol.size = 2;

            var totalDistanceTraveledSeries = Chart.AddSerie<Line>("Distance");
            totalDistanceTraveledSeries.symbol.size = 2;

            Chart.EnsureChartComponent<Legend>();

            Zoom.enable = true;
            Zoom.filterMode = DataZoom.FilterMode.Filter;
            Zoom.start = 0;
            Zoom.end = 100;

            Chart.RefreshChart();
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
    }
}