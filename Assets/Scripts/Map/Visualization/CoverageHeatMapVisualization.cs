using System;
using System.Collections.Generic;
using Maes.Robot;
using Maes.Statistics;
using UnityEngine;

namespace Maes.Map.Visualization {
    public class CoverageHeatMapVisualization : VisualizationMode {

        private SimulationMap<ExplorationCell> _explorationMap;
        private int _logicTicksBeforeCold = GlobalSettings.TicksBeforeCoverageHeatMapCold;

        public CoverageHeatMapVisualization(SimulationMap<ExplorationCell> explorationMap) {
            _explorationMap = explorationMap;
        }

        public void RegisterNewlyExploredCells(MonaRobot robot, IEnumerable<(int, ExplorationCell)> exploredCells) {
            /* Ignore exploration data */
        }

        public void RegisterNewlyCoveredCells(MonaRobot robot, IEnumerable<(int, ExplorationCell)> coveredCells) {
            /* No need for newly covered cells as the entire map is replaced every tick */
        }

        public void UpdateVisualization(ExplorationVisualizer visualizer, int currentTick) {
            // The entire map has to be replaced every tick since all colors are time dependent
            visualizer.SetAllColors(_explorationMap, (cell) => ExplorationCellToColor(cell, currentTick));
        }
        
        private Color32 ExplorationCellToColor(ExplorationCell cell, int currentTick) {
            if (!cell.CanBeCovered) return ExplorationVisualizer.SolidColor;
            if (!cell.IsCovered) return ExplorationVisualizer.StandardCellColor;
            
            // The color of every single cell is updated every tick (this is very slow on larger maps)
            // If needed this could possibly be optimized to only update the entire map every 10 ticks
            // and only update the currently visible cells the other 9 ticks.
            // (This would require that this class had access to all currently visible cells (not just newly explored cells))   
            var ticksSinceLastCovered = currentTick - cell.LastCoverageTimeInTicks;
            float coldness = Mathf.Min((float) ticksSinceLastCovered / (float) _logicTicksBeforeCold, 1.0f);
            return Color32.Lerp(ExplorationVisualizer.WarmColor, ExplorationVisualizer.ColdColor, coldness);
        }
    }
}