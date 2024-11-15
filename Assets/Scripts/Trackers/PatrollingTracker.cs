using System;
using System.Collections.Generic;
using System.Linq;

using Maes.Map;
using Maes.Map.MapGen;
using Maes.Map.Visualization.Patrolling;
using Maes.Robot;
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
        private PatrollingMap Map { get; }
        private Dictionary<Vector2Int, VertexDetails> Vertices { get; }

        public int WorstGraphIdleness { get; private set; }
        // TODO: TotalDistanceTraveled is not set any where in the code, don't know how to calculate it yet
        public float TotalDistanceTraveled { get; private set; }
        public float CurrentGraphIdleness { get; private set; }
        public float AverageGraphIdleness => GraphIdlenessList.Count != 0 ? GraphIdlenessList.Average() : 0;
        public int CompletedCycles { get; private set; }
        public float? AverageGraphDiffLastTwoCyclesProportion => GraphIdlenessList.Count >= 2 ? Mathf.Abs(GraphIdlenessList[^1] - GraphIdlenessList[^2]) / GraphIdlenessList[^2] : null;
        public ScatterChart Chart { get; set; }

        private List<float> GraphIdlenessList { get; } = new();
        //TODO: TotalCycles is not set any where in the code
        public int TotalCycles { get; }
        public bool StopAfterDiff { get; set; }

        public readonly List<PatrollingSnapShot> SnapShots = new();
        public readonly Dictionary<Vector2Int, List<WaypointSnapShot>> WaypointSnapShots;

        public PatrollingTracker(SimulationMap<Tile> collisionMap, PatrollingVisualizer visualizer, PatrollingSimulationScenario scenario,
            PatrollingMap map) : base(collisionMap, visualizer, scenario.RobotConstraints, tile => new PatrollingCell(isExplorable: !Tile.IsWall(tile.Type)))
        {
            Map = map;
            Vertices = map.Vertices.ToDictionary(vertex => vertex.Position, vertex => new VertexDetails(vertex));
            TotalCycles = scenario.TotalCycles;
            StopAfterDiff = scenario.StopAfterDiff;
            WaypointSnapShots = Vertices.Keys.ToDictionary(k => k, _ => new List<WaypointSnapShot>());

            _visualizer.meshRenderer.enabled = false;
            _currentVisualizationMode = new WaypointHeatMapVisualizationMode();
        }

        public void OnReachedVertex(Vertex vertex, int atTick)
        {
            if (!Vertices.TryGetValue(vertex.Position, out var vertexDetails))
            {
                return;
            }

            var idleness = atTick - vertexDetails.LastTimeVisitedTick;
            vertexDetails.MaxIdleness = Mathf.Max(vertexDetails.MaxIdleness, idleness);
            vertexDetails.VisitedAtTick(atTick);

            WorstGraphIdleness = Mathf.Max(WorstGraphIdleness, vertexDetails.MaxIdleness);
            SetCompletedCycles();

            if (_currentVisualizationMode is PatrollingTargetWaypointVisualizationMode)
            {
                _visualizer.ShowDefaultColor(vertex);
            }
        }

        protected override void OnLogicUpdate(IReadOnlyList<MonaRobot> robots)
        {
            var eachVertexIdleness = GetEachVertexIdleness();

            WorstGraphIdleness = eachVertexIdleness.Max();
            CurrentGraphIdleness = eachVertexIdleness.Average(n => (float)n);
            GraphIdlenessList.Add(CurrentGraphIdleness);

            // Example: How to plot the data
            // TODO: Plot the correct data and fix data limit.
            if (_currentTick % 250 == 0 && Chart.series.Count < 60000)
            {
                Chart.AddXAxisData("" + _currentTick);
                Chart.AddData(0, WorstGraphIdleness);
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

            foreach (var vertex in Vertices)
            {
                WaypointSnapShots[vertex.Key].Add(new WaypointSnapShot(_currentTick, _currentTick - vertex.Value.LastTimeVisitedTick, vertex.Value.NumberOfVisits));
            }
        }

        private IReadOnlyList<int> GetEachVertexIdleness()
        {
            return Vertices.Values.Select(vertex => _currentTick - vertex.LastTimeVisitedTick).ToArray();
        }

        private void SetCompletedCycles()
        {
            CompletedCycles = Vertices.Values.Select(v => v.NumberOfVisits).Min();
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