// Copyright 2025 MAEPS
// 
// This file is part of MAEPS
// 
// MAEPS is free software: you can redistribute it and/or modify it under
// the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your option)
// any later version.
// 
// MAEPS is distributed in the hope that it will be useful, but WITHOUT
// ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
// or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General
// Public License for more details.
// 
// You should have received a copy of the GNU General Public License along
// with MAEPS. If not, see http://www.gnu.org/licenses/.
// 
// Contributors: Mads beyer Mogensen
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Maes.Utilities;

using Unity.Mathematics;

using UnityEngine;

namespace Maes.Map.PathFinding
{
    public static class NewAStar
    {
        // TODO: Penalize direction changes
        // TODO: Support partial paths
        public static List<Vector2Int>? FindPath<TMap>(Vector2Int start, Vector2Int goal, TMap map, bool beOptimistic)
            where TMap : IPathFindingMap
        {
            Func<Vector2Int, bool> isSolid = beOptimistic ? map.IsOptimisticSolid : map.IsSolid;

            var openList = new PriorityQueue<Vector2Int, float>();

            var cameFrom = new Dictionary<Vector2Int, Vector2Int>();

            var gScore = new Dictionary<Vector2Int, float>();
            gScore.Add(start, 0);

            var fScore = new Dictionary<Vector2Int, float>();
            fScore.Add(start, Heuristic(start, goal));

            openList.Enqueue(start, fScore[start]);

            while (openList.Count > 0)
            {
                var current = openList.Dequeue();

                if (current == goal)
                {
                    return ReconstructPath(cameFrom, current);
                }

                var top = new Vector2Int(current.x, current.y + 1);
                var topRight = new Vector2Int(current.x + 1, current.y + 1);
                var right = new Vector2Int(current.x + 1, current.y);
                var bottomRight = new Vector2Int(current.x + 1, current.y - 1);
                var bottom = new Vector2Int(current.x, current.y - 1);
                var bottomLeft = new Vector2Int(current.x - 1, current.y - 1);
                var left = new Vector2Int(current.x - 1, current.y);
                var topLeft = new Vector2Int(current.x - 1, current.y + 1);

                var wallTop = isSolid(top);
                var wallTopRight = isSolid(topRight);
                var wallRight = isSolid(right);
                var wallBottomRight = isSolid(bottomRight);
                var wallBottom = isSolid(bottom);
                var wallBottomLeft = isSolid(bottomLeft);
                var wallLeft = isSolid(left);
                var wallTopLeft = isSolid(topLeft);

                var anyNeighboringWalls =
                    wallTop || wallTopRight || wallRight || wallBottomRight || wallBottom || wallBottomLeft || wallLeft || wallTopLeft;

                if (!wallTop)
                {
                    ProcessNeighbor(current, top, false, anyNeighboringWalls);
                }

                if (!wallTopRight && !wallTop && !wallRight)
                {
                    ProcessNeighbor(current, topRight, true, anyNeighboringWalls);
                }

                if (!wallRight)
                {
                    ProcessNeighbor(current, right, false, anyNeighboringWalls);
                }

                if (!wallBottomRight && !wallRight && !wallBottom)
                {
                    ProcessNeighbor(current, bottomRight, true, anyNeighboringWalls);
                }

                if (!wallBottom)
                {
                    ProcessNeighbor(current, bottom, false, anyNeighboringWalls);
                }

                if (!wallBottomLeft && !wallBottom && !wallLeft)
                {
                    ProcessNeighbor(current, bottomLeft, true, anyNeighboringWalls);
                }

                if (!wallLeft)
                {
                    ProcessNeighbor(current, left, false, anyNeighboringWalls);
                }

                if (!wallTopLeft && !wallTop && !wallLeft)
                {
                    ProcessNeighbor(current, topLeft, true, anyNeighboringWalls);
                }
            }

            return null;

            void ProcessNeighbor(Vector2Int current, Vector2Int neighbor, bool diagonal, bool neighboringWalls)
            {
                var weight = (diagonal ? math.SQRT2 : 1f) * (neighboringWalls ? 2f : 1f);
                var tentativeGScore = gScore.GetValueOrDefault(current, float.PositiveInfinity) + weight;
                var neighborGScore = gScore.GetValueOrDefault(neighbor, float.PositiveInfinity);

                if (tentativeGScore < neighborGScore)
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeGScore;
                    fScore[neighbor] = gScore[current] + Heuristic(neighbor, goal);

                    openList.Enqueue(neighbor, fScore[neighbor]);
                }
            }
        }

        private static List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
        {
            var totalPath = new List<Vector2Int>() { current };

            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                totalPath.Add(current);
            }

            totalPath.Reverse();

            return totalPath;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float Heuristic(Vector2Int current, Vector2Int goal)
        {
            return Vector2Int.Distance(current, goal);
        }
    }
}