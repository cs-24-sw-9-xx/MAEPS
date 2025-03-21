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
    internal class SelectedRobotSlamMapVisualizationMode : IExplorationVisualizationMode
    {
        private readonly SlamMap _map;
        private readonly Visualizer.CellIndexToColor _explorationCellToColorDelegate;

        public SelectedRobotSlamMapVisualizationMode(Robot2DController robot)
        {
            _map = robot.SlamMap;
            _explorationCellToColorDelegate = ExplorationCellToColor;
        }

        public void RegisterNewlyExploredCells(MonaRobot robot, List<(int, Cell)> exploredCells)
        {
            // Ignore since entire map is replaced every tick
        }

        public void RegisterNewlyCoveredCells(MonaRobot robot, List<(int, Cell)> coveredCells)
        {
            // Ignore since entire map is replaced every tick
        }

        public void UpdateVisualization(ExplorationVisualizer visualizer, int currentTick)
        {
            visualizer.SetAllColors(_explorationCellToColorDelegate);
        }

        private Color32 ExplorationCellToColor(int index)
        {
            var coordinate = _map.TriangleIndexToCoordinate(index);
            var status = _map.GetTileStatus(coordinate);

            return status switch
            {
                SlamMap.SlamTileStatus.Open => ExplorationVisualizer.SlamSeenColor,
                SlamMap.SlamTileStatus.Solid => Visualizer.SolidColor,
                _ => Visualizer.StandardCellColor
            };
        }
    }
}