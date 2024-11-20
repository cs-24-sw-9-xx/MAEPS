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
        protected readonly RayTracingMap<TCell> _rayTracingMap;
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

        private readonly CoverageCalculator<TCell>.MiniTileConsumer _preCoverageTileConsumerDelegate;

        protected Tracker(SimulationMap<Tile> collisionMap, TVisualizer visualizer, RobotConstraints constraints, Func<Tile, TCell> mapper)
        {
            _visualizer = visualizer;
            _constraints = constraints;
            _map = collisionMap.FMap(mapper);

            _visualizer.SetSimulationMap(_map, collisionMap.ScaledOffset);
            _rayTracingMap = new RayTracingMap<TCell>(_map);

            _coverageCalculator = new CoverageCalculator<TCell>(_map, collisionMap);
            _preCoverageTileConsumerDelegate = PreCoverageTileConsumer;
        }

        public void UIUpdate()
        {
            _currentVisualizationMode.UpdateVisualization(_visualizer, _currentTick);
        }

        public void LogicUpdate(List<MonaRobot> robots)
        {
            OnBeforeLogicUpdate(robots);

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

            if (GlobalSettings.ShouldWriteCsvResults
                && _currentTick != 0
                && _currentTick % GlobalSettings.TicksPerStatsSnapShot == 0)
            {
                CreateSnapShot();
            }
            _currentTick++;
        }

        protected virtual void OnBeforeLogicUpdate(List<MonaRobot> robots) { }
        protected virtual void OnLogicUpdate(List<MonaRobot> robots) { }

        public abstract void SetVisualizedRobot(MonaRobot? robot);

        protected virtual void OnAfterFirstTick(List<MonaRobot> robots)
        {
            foreach (var robot in robots)
            {
                UpdateCoverageStatus(robot);
            }
        }

        protected abstract void CreateSnapShot();

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
            _coverageCalculator.UpdateRobotCoverage(robotPos, _currentTick, _preCoverageTileConsumerDelegate);

            AfterUpdateCoverageStatus(robot);
        }



        protected virtual void PreCoverageTileConsumer(int index1, TCell triangle1, int index2, TCell triangle2) { }

        protected virtual void AfterUpdateCoverageStatus(MonaRobot robot) { }
    }
}