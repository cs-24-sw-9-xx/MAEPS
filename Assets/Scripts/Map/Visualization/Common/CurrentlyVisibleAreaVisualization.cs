using Maes.Robot;
using Maes.Statistics;

using UnityEngine;

namespace Maes.Map.Visualization.Common
{
    internal class CurrentlyVisibleAreaVisualization<TCell, TVisualizer> : IVisualizationMode<TCell, TVisualizer>
        where TCell : ICell
        where TVisualizer : Visualizer<TCell>
    {

        private readonly SimulationMap<TCell> _map;
        private readonly Robot2DController _selectedRobot;

        public CurrentlyVisibleAreaVisualization(SimulationMap<TCell> map, Robot2DController selectedRobot)
        {
            _selectedRobot = selectedRobot;
            _map = map;
        }

        public void UpdateVisualization(TVisualizer visualizer, int currentTick)
        {
            visualizer.SetAllColors(_map, ExplorationCellToColor);
        }

        private Color32 ExplorationCellToColor(int index)
        {
            return _selectedRobot.SlamMap.CurrentlyVisibleTriangles.Contains(index) ?
                Visualizer<TCell>.VisibleColor : Visualizer<TCell>.StandardCellColor;
        }
    }
}