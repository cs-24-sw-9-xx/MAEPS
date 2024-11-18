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
        public int WorstGraphIdleness { get; private set; }

        // TODO: TotalDistanceTraveled is not set any where in the code, don't know how to calculate it yet
        public float TotalDistanceTraveled { get; private set; }

        public float CurrentGraphIdleness { get; private set; }

        public float AverageGraphIdleness => _totalGraphIdleness / _ticks;

        public int CompletedCycles { get; private set; }

        public float? AverageGraphDiffLastTwoCyclesProportion => null; // This was broken anyway.

        public BaseChart Chart { get; set; } = null!;

        public DataZoom Zoom { get; set; } = null!;
        
        public bool PlotTotalDistanceTraveled = false;
        
        public bool PlotAverageIdleness = false;
        
        public bool PlotCurrentIdleness = false;
        
        public bool PlotWorstIdleness = false;
        
        public int PlottingFrequency = 50;

        private int _lastPlottedSnapshot = 0;
        
        //TODO: TotalCycles is not set any where in the code
        public int TotalCycles { get; }
        public bool StopAfterDiff { get; set; }

        public readonly List<PatrollingSnapShot> SnapShots = new();
        public readonly Dictionary<Vector2Int, List<WaypointSnapShot>> WaypointSnapShots;

        private readonly Dictionary<int, VertexDetails> _vertices;

        private float _totalGraphIdleness;
        private int _ticks;

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

        // Hack: Cursed way of updating ui using unitys update event.
        public void UIUpdate()
        {
            // TODO: Fix graph data limit.
            if (Chart.gameObject.activeSelf && SnapShots.Count > 0)
            {
                //Update zoom to only follow the most recent data.
                if (Zoom.start < 50 && Chart.series[0].data.Count >= 200)
                {
                    Zoom.start = 55;
                    Zoom.end = 95;
                }
                
                // TODO: Re-plot graph with relevant data when the graph settings change. 
                for (var i = _lastPlottedSnapshot; i <= SnapShots.Count; i++)
                {
                    if (SnapShots[i].Tick % PlottingFrequency == 0)
                    {
                        PlotData(SnapShots[i]);
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

        protected override void OnLogicUpdate(IReadOnlyList<MonaRobot> robots)
        {
            var worstGraphIdleness = 0;
            var graphIdlenessSum = 0;
            foreach (var vertex in _vertices)
            {
                var idleness = _currentTick - vertex.Value.Vertex.LastTimeVisitedTick;
                if (worstGraphIdleness < idleness)
                {
                    worstGraphIdleness = idleness;
                }

                graphIdlenessSum += idleness;
            }

            WorstGraphIdleness = worstGraphIdleness;
            CurrentGraphIdleness = (float)graphIdlenessSum / _vertices.Count;
            _totalGraphIdleness += CurrentGraphIdleness;
            _ticks++;

            // TODO: Plot the correct data and fix data limit.
            if (_currentTick % 50 == 0 && Chart != null && Chart.gameObject.activeSelf)
            {
                //Update zoom to only follow the most recent data.
                if (Zoom.start < 50 && Chart.series[0].data.Count >= 200)
                {
                    Zoom.start = 55;
                    Zoom.end = 95;
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
        
        private void PlotData(PatrollingSnapShot snapShot)
        {
            if (PlotWorstIdleness)
            {
                Chart.AddData(0, snapShot.Tick, snapShot.WorstGraphIdleness);
            }

            if (PlotCurrentIdleness)
            {
                Chart.AddData(1, snapShot.Tick, snapShot.GraphIdleness);
            }

            if (PlotAverageIdleness)
            {
                Chart.AddData(2, snapShot.Tick, snapShot.GraphIdleness);
            }

            if (PlotTotalDistanceTraveled)
            {
                Chart.AddData(3, snapShot.Tick, snapShot.TotalDistanceTraveled);
            }
        }
    }
}