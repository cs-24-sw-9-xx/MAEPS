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
// Henrik van Peet.
// 
// Original repository: https://github.com/Molitany/MAES

using System.Collections.Generic;
using System.Linq;

using Maes.Map;
using Maes.Map.PathFinding;

using UnityEngine;

namespace Maes.Utilities
{
    public static class ExtensionUtils
    {
        private const int TrianglesPerCell = 8;

        /// <summary>
        /// Extension method for converting a SLAM tile to a TNF cell
        /// </summary>
        public static (Vector2Int, float) ToTnfCell(this KeyValuePair<Vector2Int, SlamMap.SlamTileStatus> tile)
        {
            return (tile.Key, tile.Value.ToTnfCellValue());
        }

        public static float ToTnfCellValue(this SlamMap.SlamTileStatus status)
        {
            return status switch
            {
                SlamMap.SlamTileStatus.Unseen => .5f,
                _ => 0f
            };
        }

        public static float GetAngleRelativeToX(this Vector2Int vector)
        {
            return GetAngleRelativeToX((Vector2)vector);
        }

        public static float GetAngleRelativeToX(this Vector2 vector)
        {
            var angle = Vector2.SignedAngle(Vector2.right, vector);
            if (angle < 0)
            {
                angle = 360 + angle;
            }

            return angle;
        }

        public static void DrawDebugLineFromRobot(this Vector2Int tile, IPathFindingMap map, Color color, float duration = 2)
        {
            DrawDebugLineFromRobot((Vector2)tile, map, color, duration);
        }

        public static void DrawDebugLineFromRobot(this Vector2 tile, IPathFindingMap map, Color color, float duration = 2)
        {
            var robot = map.TileToWorld(map.GetCurrentPosition());
            var point1 = map.TileToWorld(tile);
            Debug.DrawLine(robot, point1, color, duration);
        }

        public static Dictionary<int, HashSet<int>> CellIndexToTriangleIndexes<TCell>(this SimulationMap<TCell> simulationMap)
        {
            var cellIndexToTriangleIndexes = new Dictionary<int, HashSet<int>>();

            var triangleIndexes = new HashSet<int>();
            var cellIndex = 0;
            foreach (var (index, _) in simulationMap)
            {
                triangleIndexes.Add(index);
                if ((index + 1) % TrianglesPerCell == 0)
                {
                    cellIndexToTriangleIndexes[cellIndex++] = new HashSet<int>(triangleIndexes);
                    triangleIndexes.Clear();
                }
            }

            return cellIndexToTriangleIndexes;
        }

        public static bool SetEquals<TKey, TValue>(this Dictionary<TKey, TValue> first, Dictionary<TKey, TValue> second)
        {
            return first.Count == second.Count && !first.Except(second).Any();
        }

        public static bool SetEquals<T>(this IEnumerable<T> first, IEnumerable<T> second)
        {
            // TODO: This is pretty slow
            return new HashSet<T>(first).SetEquals(second);
        }
    }
}