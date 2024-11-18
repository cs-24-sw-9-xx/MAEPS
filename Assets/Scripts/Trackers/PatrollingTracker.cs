using System;
using System.Collections.Generic;
using System.Linq;

using Maes.Map;
using Maes.Map.MapGen;
using Maes.Map.Visualization.Patrolling;
using Maes.Robot;
using Maes.Simulation;
using Maes.Simulation.SimulationScenarios;
using Maes.Statistics;
using Maes.Statistics.Patrolling;

using UnityEngine;

using XCharts.Runtime;

namespace Maes.Trackers
{
    // TODO: Change Tile to another type, Implemented in the next PR
    public class PatrollingTracker : Tracker<PatrollingCell, PatrollingVisualizer, IPatrollingVisualizationMode>
    {
        private PatrollingSimulation PatrollingSimulation { get; }
        private PatrollingMap Map { get; }
        private Dictionary<Vector2Int, VertexDetails> Vertices { get; }

        public int WorstGraphIdleness { get; private set; }

        // TODO: TotalDistanceTraveled is not set any where in the code, don't know how to calculate it yet
        public float TotalDistanceTraveled { get; private set; }

        public float CurrentGraphIdleness { get; private set; }

        public float AverageGraphIdleness => _graphIdlenessList.Count != 0 ? _graphIdlenessList.Average() : 0;

        public int CompletedCycles { get; private set; }
        
        public float? AverageGraphDiffLastTwoCyclesProportion => GraphIdlenessList.Count >= 2 ? Mathf.Abs(GraphIdlenessList[^1] - GraphIdlenessList[^2]) / GraphIdlenessList[^2] : null;

        public ScatterChart Chart { get; set; } = null!;

        public DataZoom Zoom { get; set; } = null!;

        private List<float> GraphIdlenessList { get; } = new();
        //TODO: TotalCycles is not set any where in the code
        public int TotalCycles { get; }
        public bool StopAfterDiff { get; set; }

        public readonly List<PatrollingSnapShot> SnapShots = new();
        public readonly Dictionary<Vector2Int, List<WaypointSnapShot>> WaypointSnapShots;

        private readonly Dictionary<int, VertexDetails> _vertices;

        private readonly List<float> _graphIdlenessList = new();

        public PatrollingTracker(SimulationMap<Tile> collisionMap, PatrollingVisualizer visualizer, PatrollingSimulationScenario scenario,
            PatrollingMap map) : base(collisionMap, visualizer, scenario.RobotConstraints, tile => new PatrollingCell(isExplorable: !Tile.IsWall(tile.Type)))
        {
            _vertices = map.Vertices.ToDictionary(vertex => vertex.Id, vertex => new VertexDetails(vertex));
            TotalCycles = scenario.TotalCycles;
            StopAfterDiff = scenario.StopAfterDiff;
            WaypointSnapShots = _vertices.Values.ToDictionary(k => k.Vertex.Position, _ => new List<WaypointSnapShot>());

            _visualizer.meshRenderer.enabled = false;
            _currentVisualizationMode = new WaypointHeatMapVisualizationMode();
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
            SetCompletedCycles();

            if (_currentVisualizationMode is PatrollingTargetWaypointVisualizationMode)
            {
                _visualizer.ShowDefaultColor(vertexDetails.Vertex);
            }
        }

        protected override void OnLogicUpdate(IReadOnlyList<MonaRobot> robots)
        {
            var eachVertexIdleness = GetEachVertexIdleness();

            WorstGraphIdleness = eachVertexIdleness.Max();
            CurrentGraphIdleness = eachVertexIdleness.Average(n => (float)n);
            _graphIdlenessList.Add(CurrentGraphIdleness);
            
            // TODO: Plot the correct data and fix data limit.
            if (_currentTick % 100 == 0 && Chart.gameObject.activeSelf)
            {
                if(Chart.series[0].data.Count >= 250)
                {
                    Zoom.start = 55;
                    Zoom.end = 95;
                    Chart.RefreshDataZoom();
                }
                
                Chart.AddData(0, _currentTick, WorstGraphIdleness);
                Chart.RefreshDataZoom();
            }
        }

        public override void SetVisualizedRobot(MonaRobot? robot)
        {
            _selectedRobot = robot;
            if (_selectedRobot != null)
            {
                _visualizer.meshRenderer.enabled = true;
                SetVisualizationMode(new CurrentlyVisibleAreaVisualizationPatrollingMode(_map, _selectedRobot.Controller));
            }
            else
            {
                _visualizer.meshRenderer.enabled = true;
                // Revert to waypoint heatmap visualization when current robot is deselected
                // while visualization mode is based on the selected robot
                SetVisualizationMode(new WaypointHeatMapVisualizationMode());
            }
        }

        protected override void CreateSnapShot()
        {
            SnapShots.Add(new PatrollingSnapShot(_currentTick, CurrentGraphIdleness, WorstGraphIdleness, TotalDistanceTraveled, CompletedCycles));

            foreach (var vertex in _vertices.Values)
            {
                WaypointSnapShots[vertex.Vertex.Position].Add(new WaypointSnapShot(_currentTick, _currentTick - vertex.Vertex.LastTimeVisitedTick, vertex.Vertex.NumberOfVisits));
            }
        }

        private IReadOnlyList<int> GetEachVertexIdleness()
        {
            return _vertices.Values.Select(vertex => _currentTick - vertex.Vertex.LastTimeVisitedTick).ToArray();
        }

        private void SetCompletedCycles()
        {
            CompletedCycles = _vertices.Values.Select(v => v.Vertex.NumberOfVisits).Min();
        }

        protected override void SetVisualizationMode(IPatrollingVisualizationMode newMode)
        {
            _visualizer.ResetWaypointsColor();
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

        public void ShowAllRobotPatrollingHeatMap()
        {
            _visualizer.meshRenderer.enabled = true;
            SetVisualizationMode(new PatrollingHeatMapVisualizationMode(_map));
        }

        public void ShowTargetWaypointSelected()
        {
            _visualizer.meshRenderer.enabled = false;
            if (_selectedRobot == null)
            {
                throw new Exception("Cannot change to 'ShowTargetWaypointSelected' Visualization mode when no robot is selected");
            }

            SetVisualizationMode(new PatrollingTargetWaypointVisualizationMode(_selectedRobot));
        }

        public void ShowVisibleSelected()
        {
            _visualizer.meshRenderer.enabled = false;
            if (_selectedRobot == null)
            {
                throw new Exception("Cannot change to 'ShowVisibleSelected' Visualization mode when no robot is selected");
            }

            _visualizer.meshRenderer.enabled = true;
            SetVisualizationMode(new CurrentlyVisibleAreaVisualizationPatrollingMode(_map, _selectedRobot.Controller));
        }
    }
}