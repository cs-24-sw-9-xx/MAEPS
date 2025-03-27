using System.Collections.Generic;

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
        private HashSet<int> _triangleIndexes;

        public SelectedRobotCommunicationRangeVisualizationMode(MonaRobot robot, SimulationMap<Tile> simulationMap)
        {
            _robot = robot;
            _simulationMap = simulationMap;
        }

        public void UpdateVisualization(PatrollingVisualizer visualizer, int currentTick)
        {
            UpdateMapColor(visualizer);
        }

        // Todo: Only color the cells that changes since last update. 
        private void UpdateMapColor(PatrollingVisualizer visualizer)
        {
            _triangleIndexes = new HashSet<int>();
            var position = _robot.Controller.SlamMap.CoarseMap.GetCurrentPosition();
            _communicationRangeBitmap = _robot.Controller.CommunicationManager.CalculateCommunicationZone(position);
            var cellIndexTriangleIndexes = _simulationMap.CellIndexToTriangleIndexes();
            foreach (var tile in _communicationRangeBitmap)
            {
                var index = tile.x + tile.y * _communicationRangeBitmap.Width;
                _triangleIndexes.UnionWith(cellIndexTriangleIndexes[index]);
            }
            visualizer.SetAllColors(CellIndexToColor);
        }

        private Color32 CellIndexToColor(int cellIndex)
        {
            return _triangleIndexes.Contains(cellIndex)
               ? PatrollingVisualizer.CommunicationColor
               : Visualizer.StandardCellColor;
        }
    }
}