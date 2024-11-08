// Copyright 2022 MAES
// 
// This file is part of MAES
// 
// MAES is free software: you can redistribute it and/or modify it under
// the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your option)
// any later version.
// 
// MAES is distributed in the hope that it will be useful, but WITHOUT
// ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
// or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General
// Public License for more details.
// 
// You should have received a copy of the GNU General Public License along
// with MAES. If not, see http://www.gnu.org/licenses/.
// 
// Contributors: Malte Z. Andreasen, Philip I. Holler and Magnus K. Jensen
// 
// Original repository: https://github.com/MalteZA/MAES

using Maes.Statistics;

using UnityEngine;

namespace Maes.Map.Visualization.Patrolling {
    internal class PatrollingHeatMapVisualizationMode : IPatrollingVisualizationMode {

        private readonly SimulationMap<PatrollingCell> _map;
        private readonly int _logicTicksBeforeCold = GlobalSettings.TicksBeforeWaypointCoverageHeatMapCold;

        public PatrollingHeatMapVisualizationMode(SimulationMap<PatrollingCell> map) {
            _map = map;
        }

        public void UpdateVisualization(PatrollingVisualizer visualizer, int currentTick) {
            // The entire map has to be replaced every tick since all colors are time dependent
            visualizer.SetAllColors(_map, (cell) => CellToColor(cell, currentTick));
        }
        
        private Color32 CellToColor(PatrollingCell cell, int currentTick) {
            if (!cell.IsExplorable) return ExplorationVisualizer.SolidColor;
            if (!cell.IsExplored) return ExplorationVisualizer.StandardCellColor;
 
            var ticksSinceLastExplored = currentTick - cell.LastExplorationTimeInTicks;
            float coldness = Mathf.Min((float) ticksSinceLastExplored / (float) _logicTicksBeforeCold, 1.0f);
            return Color32.Lerp(ExplorationVisualizer.WarmColor, ExplorationVisualizer.ColdColor, coldness);
        }
    }
}