using System;
using System.Collections.Generic;

using Maes.Map;
using Maes.Map.Generators;
using Maes.Robot;
using Maes.Statistics.Snapshots;
using Maes.UI.Visualizers;

namespace Maes.Statistics.Trackers
{
    public abstract class Tracker<TVisualizer, TVisualizationMode> : ITracker
        where TVisualizer : IVisualizer
        where TVisualizationMode : class, IVisualizationMode<TVisualizer>
    {
        protected readonly CoverageCalculator _coverageCalculator;

        protected readonly TVisualizer _visualizer;

        protected readonly SimulationMap<Cell> _map;
        public delegate void VisualizationModeConsumer(TVisualizationMode mode);
        public event VisualizationModeConsumer? OnVisualizationModeChanged;

        protected MonaRobot? _selectedRobot;

        public int CurrentTick { get; private set; }

        private bool _isFirstTick = true;
        protected RobotConstraints _constraints;

        // Set by derived class constructor.
        protected TVisualizationMode _currentVisualizationMode = null!;

        private readonly CoverageCalculator.MiniTileConsumer _preCoverageTileConsumerDelegate;

        private readonly CommunicationManager _communicationManager;

        protected Tracker(SimulationMap<Tile> collisionMap, TVisualizer visualizer, RobotConstraints constraints, Func<Tile, Cell> mapper, CommunicationManager communicationManager)
        {
            _visualizer = visualizer;
            _constraints = constraints;
            _map = collisionMap.FMap(mapper);

#if MAEPS_GUI
            _visualizer.SetSimulationMap(_map);
#endif

            _coverageCalculator = new CoverageCalculator(_map, collisionMap);
            _preCoverageTileConsumerDelegate = PreCoverageTileConsumer;

            _communicationManager = communicationManager;
        }

        public void UIUpdate()
        {
            _currentVisualizationMode.UpdateVisualization(_visualizer, CurrentTick);
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
                && CurrentTick != 0
                && CurrentTick % GlobalSettings.TicksPerStatsSnapShot == 0)
            {
                var latestSnapshot = _communicationManager.CommunicationTracker.LatestSnapshot;
                CreateSnapShot(latestSnapshot);
            }
            CurrentTick++;
        }

        public abstract void FinishStatistics();

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

        protected abstract void CreateSnapShot(CommunicationSnapshot communicationSnapshot);

        protected virtual void SetVisualizationMode(TVisualizationMode newMode)
        {
            _currentVisualizationMode = newMode;
            _currentVisualizationMode.UpdateVisualization(_visualizer, CurrentTick);
            OnVisualizationModeChanged?.Invoke(_currentVisualizationMode);
        }

        protected virtual void UpdateCoverageStatus(MonaRobot robot)
        {
            var robotPos = robot.transform.position;

            // Find each mini tile (two triangle cells) covered by the robot and execute the following function on it
            _coverageCalculator.UpdateRobotCoverage(robotPos, CurrentTick, _preCoverageTileConsumerDelegate);

            AfterUpdateCoverageStatus(robot);
        }



        protected virtual void PreCoverageTileConsumer(int index1, Cell triangle1, int index2, Cell triangle2) { }

        protected virtual void AfterUpdateCoverageStatus(MonaRobot robot) { }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // TODO release managed resources here
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}