using System.Collections.Generic;

using Maes.Map;
using Maes.Map.Generators;
using Maes.Robot;

namespace Maes.UI.Visualizers.Patrolling.VisualizationModes
{
    public class SelectedRobotCommunicationRangeVisualizationMode : IPatrollingVisualizationMode
    {
        private readonly MonaRobot _robot;
        private readonly SimulationMap<Tile> _simulationMap;
        private HashSet<int> _triangleIndexes;

        public SelectedRobotCommunicationRangeVisualizationMode(MonaRobot robot, SimulationMap<Tile> simulationMap)
        {
            _robot = robot;
            _simulationMap = simulationMap;
            _triangleIndexes = new HashSet<int>();
        }

        public void UpdateVisualization(PatrollingVisualizer visualizer, int currentTick)
        {
            UpdateMapColor(visualizer);
        }

        // Performance optimization: only update the color of cell that changed since last update.
        // Note: This is a debug feature, so performance is not critical.
        private void UpdateMapColor(PatrollingVisualizer visualizer)
        {
            _triangleIndexes = new HashSet<int>();
            var position = _robot.Controller.SlamMap.CoarseMap.GetCurrentPosition();
            var communicationRangeBitmap = _robot.Controller.CommunicationManager.CalculateCommunicationZone(position);
            visualizer.SetAllColors(communicationRangeBitmap, PatrollingVisualizer.CommunicationColor, Visualizer.StandardCellColor);
            communicationRangeBitmap.Dispose();
        }
    }
}