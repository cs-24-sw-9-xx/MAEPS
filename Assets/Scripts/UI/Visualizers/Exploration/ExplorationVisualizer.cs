// Copyright 2024 MAES
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
// Contributors: Rasmus Borrisholt Schmidt, Andreas Sebastian Sørensen, Thor Beregaard, Malte Z. Andreasen, Philip I. Holler and Magnus K. Jensen,
// 
// Original repository: https://github.com/Molitany/MAES

using System.Collections.Generic;

using Maes.Statistics;

using UnityEngine;

namespace Maes.UI.Visualizers.Exploration
{
    public class ExplorationVisualizer : Visualizer
    {
        public static readonly Color32 ExploredColor = new(32, 130, 57, 255);
        public static readonly Color32 CoveredColor = new(32, 80, 240, 255);
        public static readonly Color32 SlamSeenColor = new(50, 120, 180, 255);
        public static readonly Color32 WarmColor = new(200, 60, 60, 255);
        public static readonly Color32 ColdColor = new(50, 120, 180, 255);

        protected override Color32 InitializeCellColor(Cell cell)
        {
            var color = SolidColor;
            if (cell.IsExplorable)
            {
                color = cell.IsExplored ? ExploredColor : StandardCellColor;
            }

            return color;
        }

        /// <summary>
        /// Updates the colors of the triangles corresponding to the given list of exploration cells.
        /// </summary>
        public void UpdateColors(HashSet<(int, Cell)> cellsWithIndices, CellToColor cellToColor)
        {
            foreach (var (index, cell) in cellsWithIndices)
            {
                var vertexIndex = index * 3;
                var color = cellToColor(cell);
                _colors[vertexIndex] = color;
                _colors[vertexIndex + 1] = color;
                _colors[vertexIndex + 2] = color;
            }

            _mesh.colors32 = _colors;
        }
    }
}