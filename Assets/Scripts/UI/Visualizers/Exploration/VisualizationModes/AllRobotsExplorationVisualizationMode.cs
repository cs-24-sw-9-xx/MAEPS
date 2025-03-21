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

using System.Collections.Generic;

using Maes.Map;
using Maes.Robot;
using Maes.Statistics;

using UnityEngine;

namespace Maes.UI.Visualizers.Exploration.VisualizationModes
{
    internal class AllRobotsExplorationVisualizationMode : IExplorationVisualizationMode
    {
        private static readonly Visualizer.CellToColor CellToColorDelegate = ExplorationCellToColor;

        private readonly SimulationMap<Cell> _explorationMap;
        private readonly HashSet<(int, Cell)> _newlyExploredCells = new();
        private bool _hasBeenInitialized;

        public AllRobotsExplorationVisualizationMode(SimulationMap<Cell> explorationMap)
        {
            _explorationMap = explorationMap;
        }

        public void RegisterNewlyExploredCells(MonaRobot robot, List<(int, Cell)> exploredCells)
        {
            foreach (var cellWithIndex in exploredCells)
            {
                _newlyExploredCells.Add(cellWithIndex);
            }
        }

        public void RegisterNewlyCoveredCells(MonaRobot robot, List<(int, Cell)> coveredCells)
        {
            /* Ignore coverage */
        }

        public void UpdateVisualization(ExplorationVisualizer visualizer, int currentTick)
        {
            if (_hasBeenInitialized)
            {
                visualizer.UpdateColors(_newlyExploredCells, CellToColorDelegate);
                _newlyExploredCells.Clear();
            }
            else
            {
                // In the first iteration of this visualizer overwrite all colors of previous visualization mode
                visualizer.SetAllColors(_explorationMap, CellToColorDelegate);
                _hasBeenInitialized = true;
            }
        }

        private static Color32 ExplorationCellToColor(Cell cell)
        {
            if (!cell.IsExplorable)
            {
                return Visualizer.SolidColor;
            }

            return (cell.IsExplored) ? ExplorationVisualizer.ExploredColor : Visualizer.StandardCellColor;
        }
    }
}