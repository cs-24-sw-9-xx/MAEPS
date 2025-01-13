using Maes.Robot;
using Maes.Statistics;

using UnityEngine;

namespace Maes.Map.Visualization.Common
{
    internal abstract class CurrentlyVisibleAreaVisualization<TVisualizer> : IVisualizationMode<TVisualizer>
        where TVisualizer : Visualizer
    {
        private readonly SimulationMap<Cell> _map;
        private readonly Robot2DController _selectedRobot;
        private readonly Visualizer.CellIndexToColor _explorationCellToColorDelegate;

        protected CurrentlyVisibleAreaVisualization(SimulationMap<Cell> map, Robot2DController selectedRobot)
        {
            _selectedRobot = selectedRobot;
            _map = map;
            _explorationCellToColorDelegate = ExplorationCellToColor;
        }

        public void UpdateVisualization(TVisualizer visualizer, int currentTick)
        {
            visualizer.SetAllColors(_map, _explorationCellToColorDelegate);
        }

        private Color32 ExplorationCellToColor(int index)
        {
            return _selectedRobot.SlamMap.CurrentlyVisibleTriangles.Contains(index) ?
                Visualizer.VisibleColor : Visualizer.StandardCellColor;
        }
    }
}