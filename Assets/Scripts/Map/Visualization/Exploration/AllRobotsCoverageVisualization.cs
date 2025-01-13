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

using Maes.Robot;
using Maes.Statistics;

using UnityEngine;

namespace Maes.Map.Visualization.Exploration
{
    internal class AllRobotsCoverageVisualization : IExplorationVisualizationMode
    {
        private static readonly Visualizer.CellToColor ExplorationCellToColorDelegate = ExplorationCellToColor;

        private readonly SimulationMap<Cell> _explorationMap;
        private readonly HashSet<(int, Cell)> _newlyCoveredCells = new();
        private bool _hasBeenInitialized;

        public AllRobotsCoverageVisualization(SimulationMap<Cell> explorationMap)
        {
            _explorationMap = explorationMap;
        }

        public void RegisterNewlyExploredCells(MonaRobot robot, List<(int, Cell)> exploredCells)
        {
            /* Ignore exploration */
        }

        public void RegisterNewlyCoveredCells(MonaRobot robot, List<(int, Cell)> coveredCells)
        {
            foreach (var cellWithIndex in coveredCells)
            {
                _newlyCoveredCells.Add(cellWithIndex);
            }
        }

        public void UpdateVisualization(ExplorationVisualizer visualizer, int currentTick)
        {
            if (_hasBeenInitialized)
            {
                visualizer.UpdateColors(_newlyCoveredCells, ExplorationCellToColorDelegate);
                _newlyCoveredCells.Clear();
            }
            else
            {
                // In the first iteration of this visualizer overwrite all colors of previous visualization mode
                visualizer.SetAllColors(_explorationMap, ExplorationCellToColorDelegate);
                _hasBeenInitialized = true;
            }
        }

        private static Color32 ExplorationCellToColor(Cell cell)
        {
            if (!cell.CanBeCovered)
            {
                return ExplorationVisualizer.SolidColor;
            }

            return (cell.IsCovered) ? ExplorationVisualizer.CoveredColor : ExplorationVisualizer.StandardCellColor;
        }
    }
}