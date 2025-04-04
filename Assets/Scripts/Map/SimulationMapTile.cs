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

using System;

namespace Maes.Map
{
    public readonly struct SimulationMapTile<TCell>
    {
        // A tile is a rectangle consisting of 8 triangle shaped cells.
        // The triangles are arranged in 4 different orientations
        // The triangles are indexed and arranged as shown in this very pretty illustration:
        ///  |4/5|6\7|
        ///  |0\1|2/3|
        private readonly TCell[] _triangleCells;

        public SimulationMapTile(Func<TCell> cellFactory)
        {
            _triangleCells = new TCell[8];
            for (var i = 0; i < 8; i++)
            {
                _triangleCells[i] = cellFactory();
            }
        }

        public void SetCellValue(int index, TCell newCellValue)
        {
            _triangleCells[index] = newCellValue;
        }

        private SimulationMapTile(TCell[] cells)
        {
            _triangleCells = cells;
        }


        public SimulationMapTile<TNewCell> FMap<TNewCell>(Func<TCell, TNewCell> mapper)
        {
            var mappedCells = new TNewCell[8];
            for (var i = 0; i < _triangleCells.Length; i++)
            {
                mappedCells[i] = mapper(_triangleCells[i]);
            }

            return new SimulationMapTile<TNewCell>(mappedCells);
        }

        public void ForEachCell(Action<TCell> cellAction)
        {
            foreach (var cell in _triangleCells)
            {
                cellAction(cell);
            }
        }

        public bool IsTrueForAll(Func<TCell, bool> predicate)
        {
            foreach (var cell in _triangleCells)
            {
                if (!predicate(cell))
                {
                    return false;
                }
            }

            return true;
        }

        public TCell GetTriangleCellByCoordinateDecimals(float xDecimals, float yDecimals)
        {
            return _triangleCells[CoordinateDecimalsToTriangleIndex(xDecimals, yDecimals)];
        }

        public void SetTriangleCellByCoordinateDecimals(float xDecimals, float yDecimals, TCell newCell)
        {
            _triangleCells[CoordinateDecimalsToTriangleIndex(xDecimals, yDecimals)] = newCell;
        }


        public int CoordinateDecimalsToTriangleIndex(float xDecimal, float yDecimal)
        {
#if DEBUG
            if (xDecimal < 0.0f || xDecimal > 1.0f || yDecimal < 0.0f || yDecimal > 1.0f)
            {
                throw new ArgumentException("Coordinate decimals must be between 0.0 and 1.0. " +
                                            "Coordinates were: (" + xDecimal + ", " + yDecimal + " )");
            }
#endif

            if (yDecimal < 0.5)
            {
                // Bottom left quadrant
                if (xDecimal + yDecimal < 0.5f)
                {
                    return 0;
                }

                if (xDecimal + yDecimal >= 0.5f && xDecimal < 0.5f)
                {
                    return 1;
                }
                // Bottom right quadrant
                if (xDecimal - yDecimal < 0.5f)
                {
                    return 2;
                }

                return 3;
            }
            else
            {
                // Top left quadrant
                if (yDecimal - xDecimal > 0.5f && xDecimal < 0.5f)
                {
                    return 4;
                }

                if (yDecimal - xDecimal <= 0.5f && xDecimal < 0.5f)
                {
                    return 5;
                }
                // Top right quadrant
                if (xDecimal + yDecimal < 1.5f)
                {
                    return 6;
                }

                return 7;
            }
        }

        public TCell[] GetTriangles()
        {
            return _triangleCells;
        }
    }
}