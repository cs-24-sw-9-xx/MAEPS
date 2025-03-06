using Maes.Map;
using Maes.Statistics;
using Maes.UI.Visualizers.Exploration;

using UnityEngine;

namespace Maes.UI.Visualizers.Patrolling.VisualizationModes
{
    public class PatrollingCoverageHeatMapVisualizationMode : IPatrollingVisualizationMode
    {
        private readonly SimulationMap<Cell> _map;
        private readonly int _logicTicksBeforeCold = GlobalSettings.TicksBeforeWaypointCoverageHeatMapCold;

        public PatrollingCoverageHeatMapVisualizationMode(SimulationMap<Cell> map)
        {
            _map = map;
        }

        public void UpdateVisualization(PatrollingVisualizer visualizer, int currentTick)
        {
            visualizer.SetAllColors(_map, cell => CellToColor(cell, currentTick));
        }

        private Color32 CellToColor(Cell cell, int currentTick)
        {
            if (!cell.CanBeCovered)
            {
                return ExplorationVisualizer.SolidColor;
            }

            if (!cell.IsCovered)
            {
                return ExplorationVisualizer.StandardCellColor;
            }

            var ticksSinceLastCovered = currentTick - cell.LastCoverageTimeInTicks;
            var coldness = Mathf.Min((float)ticksSinceLastCovered / (float)_logicTicksBeforeCold, 1.0f);
            return Color32.Lerp(ExplorationVisualizer.WarmColor, ExplorationVisualizer.ColdColor, coldness);
        }
    }
}