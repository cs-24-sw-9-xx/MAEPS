using Maes.Map;
using Maes.Map.Generators;
using Maes.Robot;
using Maes.Utilities;

using UnityEngine;

namespace Maes.UI.Visualizers.Patrolling.VisualizationModes
{
    public class SelectedRobotCommunicationRangeVisualizationMode : IPatrollingVisualizationMode
    {
        private readonly MonaRobot _robot;
        private readonly SimulationMap<Tile> _simulationMap;
        private Bitmap _communicationRangeBitmap;

        public SelectedRobotCommunicationRangeVisualizationMode(MonaRobot robot, SimulationMap<Tile> simulationMap)
        {
            _robot = robot;
            _simulationMap = simulationMap;
        }

        public void UpdateVisualization(PatrollingVisualizer visualizer, int currentTick)
        {
            UpdateMapColor(visualizer);
        }

        private void UpdateMapColor(PatrollingVisualizer visualizer)
        {
            var position = _robot.Controller.SlamMap.GetCurrentPosition();
            _communicationRangeBitmap = _robot.Controller.CommunicationManager.CalculateCommunicationZone(_simulationMap, position);
            visualizer.SetAllColors(_communicationRangeBitmap, BooleanToColor);
        }

        private Color32 BooleanToColor(bool isContainedInMap)
        {
            return isContainedInMap
               ? PatrollingVisualizer.CommunicationColor
               : Visualizer.StandardCellColor;
        }
    }
}