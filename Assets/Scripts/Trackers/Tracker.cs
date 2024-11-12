using System;
using System.Collections.Generic;

using Maes.Map;
using Maes.Map.MapGen;
using Maes.Map.Visualization;
using Maes.Robot;
using Maes.Statistics;
using Maes.Visualizers;

namespace Maes.Trackers
{
    public abstract class Tracker<TCell, TVisualizer, TVisualizationMode> : ITracker
        where TCell : Cell
        where TVisualizer : IVisualizer<TCell>
        where TVisualizationMode : class, IVisualizationMode<TCell, TVisualizer>
    {
        protected readonly CoverageCalculator<TCell> _coverageCalculator;

        protected readonly TVisualizer _visualizer;

        protected readonly SimulationMap<TCell> _map;
        private readonly RayTracingMap<TCell> _rayTracingMap;
        private readonly int _explorationMapWidth;
        private readonly int _explorationMapHeight;

        public delegate void VisualizationModeConsumer(TVisualizationMode mode);
        public event VisualizationModeConsumer? OnVisualizationModeChanged;

        protected MonaRobot? _selectedRobot;

        protected int _currentTick;

        private bool _isFirstTick = true;
        protected RobotConstraints _constraints;

        // Set by derived class constructor.
        protected TVisualizationMode _currentVisualizationMode = null!;

        protected Tracker(SimulationMap<Tile> collisionMap, TVisualizer visualizer, RobotConstraints constraints, Func<Tile, TCell> mapper)
        {
            _visualizer = visualizer;
            _constraints = constraints;
            _map = collisionMap.FMap(mapper);

            _visualizer.SetSimulationMap(_map, collisionMap.ScaledOffset);
            _rayTracingMap = new RayTracingMap<TCell>(_map);

            _coverageCalculator = new CoverageCalculator<TCell>(_map, collisionMap);
        }

        public void LogicUpdate(IReadOnlyList<MonaRobot> robots)
        {
            // The user can specify the tick interval at which the slam map is updated. 
            var shouldUpdateSlamMap = _constraints.AutomaticallyUpdateSlam &&
                                      _currentTick % _constraints.SlamUpdateIntervalInTicks == 0;
            PerformRayTracing(robots, shouldUpdateSlamMap);

            // In the first tick, the robot does not have a position in the slam map.
            if (!_isFirstTick)
            {
                OnAfterFirstTick(robots);
            }
            else
            {
                _isFirstTick = false;
            }

            if (_constraints.AutomaticallyUpdateSlam)
            {
                // Always update estimated robot position and rotation
                // regardless of whether the slam map was updated this tick
                foreach (var robot in robots)
                {
                    var slamMap = robot.Controller.SlamMap;
                    slamMap.UpdateApproxPosition(robot.transform.position);
                    slamMap.SetApproxRobotAngle(robot.Controller.GetForwardAngleRelativeToXAxis());
                }
            }

            OnLogicUpdate(robots);

            _currentVisualizationMode.UpdateVisualization(_visualizer, _currentTick);
            _currentTick++;

            if (GlobalSettings.ShouldWriteCsvResults
                && _currentTick != 0
                && _currentTick % GlobalSettings.TicksPerStatsSnapShot == 0)
            {
                CreateSnapShot();
            }
        }

        protected virtual void OnLogicUpdate(IReadOnlyList<MonaRobot> robots) { }

        public abstract void SetVisualizedRobot(MonaRobot? robot);

        protected virtual void OnAfterFirstTick(IReadOnlyList<MonaRobot> robots)
        {
            foreach (var robot in robots)
            {
                UpdateCoverageStatus(robot);
            }
        }

        protected abstract void CreateSnapShot();

        // Updates both exploration tracker and robot slam maps
        private void PerformRayTracing(IReadOnlyList<MonaRobot> robots, bool shouldUpdateSlamMap)
        {
            var visibilityRange = _constraints.SlamRayTraceRange;

            foreach (var robot in robots)
            {
                SlamMap? slamMap = null;

                if (shouldUpdateSlamMap)
                {
                    slamMap = robot.Controller.SlamMap;
                    slamMap.ResetRobotVisibility();
                }

                // Use amount of traces specified by user, or calculate circumference and use trace at interval of 4
                var tracesPerMeter = 2f;
                var traces = _constraints.SlamRayTraceCount ?? (int)(Math.PI * 2f * _constraints.SlamRayTraceRange * tracesPerMeter);
                var traceIntervalDegrees = 360f / traces;
                for (var i = 0; i < traces; i++)
                {
                    var angle = i * traceIntervalDegrees;
                    // Avoid ray casts that can be parallel to the lines of a triangle
                    if (angle % 45 == 0)
                    {
                        angle += 0.5f;
                    }

                    _rayTracingMap.Raytrace(robot.transform.position, angle, visibilityRange, (index, cell) =>
                    {
                        if (cell.IsExplorable)
                        {
                            if (!cell.IsExplored)
                            {
                                cell.LastExplorationTimeInTicks = _currentTick;
                                cell.ExplorationTimeInTicks += 1;
                                OnNewlyExploredTriangles(index, cell);
                            }

                            cell.RegisterExploration(_currentTick);
                        }

                        // Update robot slam map if present (slam map only non-null if 'shouldUpdateSlamMap' is true)
                        slamMap?.SetExploredByTriangle(triangleIndex: index, isOpen: cell.IsExplorable);
                        slamMap?.SetCurrentlyVisibleByTriangle(triangleIndex: index, isOpen: cell.IsExplorable);

                        return cell.IsExplorable;
                    });
                }

                AfterRayTracingARobot(robot);
            }
        }

        protected virtual void OnNewlyExploredTriangles(int index, TCell cell) { }

        protected virtual void AfterRayTracingARobot(MonaRobot robot) { }

        protected virtual void SetVisualizationMode(TVisualizationMode newMode)
        {
            _currentVisualizationMode = newMode;
            _currentVisualizationMode.UpdateVisualization(_visualizer, _currentTick);
            OnVisualizationModeChanged?.Invoke(_currentVisualizationMode);
        }

        protected virtual void UpdateCoverageStatus(MonaRobot robot)
        {
            var robotPos = robot.transform.position;

            // Find each mini tile (two triangle cells) covered by the robot and execute the following function on it
            _coverageCalculator.UpdateRobotCoverage(robotPos, _currentTick, preCoverageTileConsumer);

            AfterUpdateCoverageStatus(robot);
        }

        protected virtual CoverageCalculator<TCell>.MiniTileConsumer preCoverageTileConsumer => (index1, triangle1, index2, triangle2) => { };

        protected virtual void AfterUpdateCoverageStatus(MonaRobot robot) { }
    }
}