using Maes.Robot;

using UnityEngine;

namespace Maes.UI.Visualizers.Patrolling.VisualizationModes
{
    public class SelectedRobotCommunicationRangeVisualizationMode : IPatrollingVisualizationMode
    {
        private readonly MonaRobot _robot;
        private readonly Vector2Int _lastPosition;

        public SelectedRobotCommunicationRangeVisualizationMode(MonaRobot robot)
        {
            _robot = robot;
            _lastPosition = new Vector2Int(int.MaxValue, int.MaxValue);
        }

        public void UpdateVisualization(PatrollingVisualizer visualizer, int currentTick)
        {
            UpdateMapColor(visualizer);
        }

        // Performance optimization: only update the color of cell that changed since last update.
        // Note: This is a debug feature, so performance is not critical.
        private void UpdateMapColor(PatrollingVisualizer visualizer)
        {
            var position = _robot.Controller.SlamMap.CoarseMap.GetCurrentPosition();
            // Communication is based on the tile the robot is on,
            // so if the robot hasn't moved to a new tile we don't need to update the map.
            if (position == _lastPosition)
            {
                return;
            }

            var communicationRangeBitmap = _robot.Controller.CommunicationManager.CalculateCommunicationZone(position);
            visualizer.SetAllColors(communicationRangeBitmap, PatrollingVisualizer.CommunicationColor, Visualizer.StandardCellColor);
            communicationRangeBitmap.Dispose();
        }
    }
}