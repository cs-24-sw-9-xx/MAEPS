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
        private const float TurningPenalty = 0.5f;

        private static readonly Vector2Int Top = new Vector2Int(0, 1);
        private static readonly Vector2Int TopRight = new Vector2Int(1, 1);
        private static readonly Vector2Int Right = new Vector2Int(1, 0);
        private static readonly Vector2Int BottomRight = new Vector2Int(1, -1);
        private static readonly Vector2Int Bottom = new Vector2Int(0, -1);
        private static readonly Vector2Int BottomLeft = new Vector2Int(-1, -1);
        private static readonly Vector2Int Left = new Vector2Int(-1, 0);
        private static readonly Vector2Int TopLeft = new Vector2Int(-1, 1);

        /// <summary>
        /// Find a path from <paramref name="start"/> to <paramref name="goal"/>.
        /// </summary>
        /// <param name="start">The coordinate to start from.</param>
        /// <param name="goal">The coordinate to find a path to.</param>
        /// <param name="map">The map to pathfind on.</param>
        /// <param name="beOptimistic">Whether or not to treat unseen tiles as walkable.</param>
        /// <param name="acceptPartialPaths">Whether or not to return the path to the closest coordinate to the target if no path was found to the target.</param>
        /// <param name="dependOnBrokenBehaviour">The goal might be in a wall, allow pathing to it anyway.</param>
        /// <typeparam name="TMap">The type of <paramref name="map"/>.</typeparam>
        /// <returns>The path or <see langword="null"/> if no path was found.</returns>
        public static List<Vector2Int>? FindPath<TMap>(Vector2Int start, Vector2Int goal, TMap map, bool beOptimistic, bool acceptPartialPaths, bool dependOnBrokenBehaviour)
            where TMap : IPathFindingMap
        {
            Func<Vector2Int, bool> isSolid = beOptimistic ? map.IsOptimisticSolid : map.IsSolid;

            var closestDistance = float.PositiveInfinity;
            var closest = Vector2Int.zero;

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

                var currentParent = cameFrom.GetValueOrNull(current);

                var top = current + Top;
                var topRight = current + TopRight;
                var right = current + Right;
                var bottomRight = current + BottomRight;
                var bottom = current + Bottom;
                var bottomLeft = current + BottomLeft;
                var left = current + Left;
                var topLeft = current + TopLeft;

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

                if (!wallTop || (dependOnBrokenBehaviour && top == goal))
                {
                    ProcessNeighbor(current, top, currentParent, false, anyNeighboringWalls);
                }

                if ((!wallTopRight || (dependOnBrokenBehaviour && topRight == goal)) && !wallTop && !wallRight)
                {
                    ProcessNeighbor(current, topRight, currentParent, true, anyNeighboringWalls);
                }

                if (!wallRight || (dependOnBrokenBehaviour && right == goal))
                {
                    ProcessNeighbor(current, right, currentParent, false, anyNeighboringWalls);
                }

                if ((!wallBottomRight || (dependOnBrokenBehaviour && bottomRight == goal)) && !wallRight && !wallBottom)
                {
                    ProcessNeighbor(current, bottomRight, currentParent, true, anyNeighboringWalls);
                }

                if (!wallBottom || (dependOnBrokenBehaviour && bottom == goal))
                {
                    ProcessNeighbor(current, bottom, currentParent, false, anyNeighboringWalls);
                }

                if ((!wallBottomLeft || (dependOnBrokenBehaviour && bottomLeft == goal)) && !wallBottom && !wallLeft)
                {
                    ProcessNeighbor(current, bottomLeft, currentParent, true, anyNeighboringWalls);
                }

                if (!wallLeft || (dependOnBrokenBehaviour && left == goal))
                {
                    ProcessNeighbor(current, left, currentParent, false, anyNeighboringWalls);
                }

                if ((!wallTopLeft || (dependOnBrokenBehaviour && topLeft == goal)) && !wallTop && !wallLeft)
                {
                    ProcessNeighbor(current, topLeft, currentParent, true, anyNeighboringWalls);
                }
            }

            if (acceptPartialPaths && !float.IsPositiveInfinity(closestDistance))
            {
                return FindPath(start, closest, map, beOptimistic, acceptPartialPaths: false, dependOnBrokenBehaviour: false);
            }

            return null;

            void ProcessNeighbor(Vector2Int current, Vector2Int neighbor, Vector2Int? currentParent, bool diagonal, bool neighboringWalls)
            {
                var turningPenalty = 0f;
                if (currentParent != null)
                {
                    var lastDirection = current - currentParent;
                    var currentDirection = neighbor - current;
                    turningPenalty = lastDirection == currentDirection ? 0f : TurningPenalty;
                }

                var weight = (diagonal ? math.SQRT2 : 1f) * (neighboringWalls ? 2f : 1f) + turningPenalty;
                var tentativeGScore = gScore.GetValueOrDefault(current, float.PositiveInfinity) + weight;
                var neighborGScore = gScore.GetValueOrDefault(neighbor, float.PositiveInfinity);

                if (tentativeGScore < neighborGScore)
                {
                    var heuristic = Heuristic(neighbor, goal);
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeGScore;
                    fScore[neighbor] = gScore[current] + heuristic;

                    openList.Enqueue(neighbor, fScore[neighbor]);

                    if (acceptPartialPaths && (heuristic < closestDistance))
                    {
                        closestDistance = heuristic;
                        closest = neighbor;
                    }
                }
            }
        }

        /// <summary>
        /// Find a path from <paramref name="start"/> to <paramref name="goal"/>.
        /// </summary>
        /// <param name="start">The coordinate to start from.</param>
        /// <param name="goal">The coordinate to find a path to.</param>
        /// <param name="map">The map to pathfind on.</param>
        /// <param name="beOptimistic">Whether or not to treat unseen tiles as walkable.</param>
        /// <param name="acceptPartialPaths">Whether or not to return the path to the closest coordinate to the target if no path was found to the target.</param>
        /// <param name="dependOnBrokenBehaviour">The goal might be in a wall, allow pathing to it anyway.</param>
        /// <typeparam name="TMap">The type of <paramref name="map"/>.</typeparam>
        /// <returns>The path or <see langword="null"/> if no path was found.</returns>
        public static List<Vector2Int>? FindPath(Vector2Int start, Vector2Int goal, Bitmap map, bool acceptPartialPaths, bool dependOnBrokenBehaviour)
        {

            var closestDistance = float.PositiveInfinity;
            var closest = Vector2Int.zero;

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

                var currentParent = cameFrom.GetValueOrNull(current);

                var top = current + Top;
                var topRight = current + TopRight;
                var right = current + Right;
                var bottomRight = current + BottomRight;
                var bottom = current + Bottom;
                var bottomLeft = current + BottomLeft;
                var left = current + Left;
                var topLeft = current + TopLeft;

                var wallTop = map.Contains(top);
                var wallTopRight = map.Contains(topRight);
                var wallRight = map.Contains(right);
                var wallBottomRight = map.Contains(bottomRight);
                var wallBottom = map.Contains(bottom);
                var wallBottomLeft = map.Contains(bottomLeft);
                var wallLeft = map.Contains(left);
                var wallTopLeft = map.Contains(topLeft);

                var anyNeighboringWalls =
                    wallTop || wallTopRight || wallRight || wallBottomRight || wallBottom || wallBottomLeft || wallLeft || wallTopLeft;

                if (!wallTop || (dependOnBrokenBehaviour && top == goal))
                {
                    ProcessNeighbor(current, top, currentParent, false, anyNeighboringWalls);
                }

                if ((!wallTopRight || (dependOnBrokenBehaviour && topRight == goal)) && !wallTop && !wallRight)
                {
                    ProcessNeighbor(current, topRight, currentParent, true, anyNeighboringWalls);
                }

                if (!wallRight || (dependOnBrokenBehaviour && right == goal))
                {
                    ProcessNeighbor(current, right, currentParent, false, anyNeighboringWalls);
                }

                if ((!wallBottomRight || (dependOnBrokenBehaviour && bottomRight == goal)) && !wallRight && !wallBottom)
                {
                    ProcessNeighbor(current, bottomRight, currentParent, true, anyNeighboringWalls);
                }

                if (!wallBottom || (dependOnBrokenBehaviour && bottom == goal))
                {
                    ProcessNeighbor(current, bottom, currentParent, false, anyNeighboringWalls);
                }

                if ((!wallBottomLeft || (dependOnBrokenBehaviour && bottomLeft == goal)) && !wallBottom && !wallLeft)
                {
                    ProcessNeighbor(current, bottomLeft, currentParent, true, anyNeighboringWalls);
                }

                if (!wallLeft || (dependOnBrokenBehaviour && left == goal))
                {
                    ProcessNeighbor(current, left, currentParent, false, anyNeighboringWalls);
                }

                if ((!wallTopLeft || (dependOnBrokenBehaviour && topLeft == goal)) && !wallTop && !wallLeft)
                {
                    ProcessNeighbor(current, topLeft, currentParent, true, anyNeighboringWalls);
                }
            }

            if (acceptPartialPaths && !float.IsPositiveInfinity(closestDistance))
            {
                return FindPath(start, closest, map, acceptPartialPaths: false, dependOnBrokenBehaviour: false);
            }

            return null;

            void ProcessNeighbor(Vector2Int current, Vector2Int neighbor, Vector2Int? currentParent, bool diagonal, bool neighboringWalls)
            {
                var turningPenalty = 0f;
                if (currentParent != null)
                {
                    var lastDirection = current - currentParent;
                    var currentDirection = neighbor - current;
                    turningPenalty = lastDirection == currentDirection ? 0f : TurningPenalty;
                }

                var weight = (diagonal ? math.SQRT2 : 1f) * (neighboringWalls ? 2f : 1f) + turningPenalty;
                var tentativeGScore = gScore.GetValueOrDefault(current, float.PositiveInfinity) + weight;
                var neighborGScore = gScore.GetValueOrDefault(neighbor, float.PositiveInfinity);

                if (tentativeGScore < neighborGScore)
                {
                    var heuristic = Heuristic(neighbor, goal);
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeGScore;
                    fScore[neighbor] = gScore[current] + heuristic;

                    openList.Enqueue(neighbor, fScore[neighbor]);

                    if (acceptPartialPaths && (heuristic < closestDistance))
                    {
                        closestDistance = heuristic;
                        closest = neighbor;
                    }
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