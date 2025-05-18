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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

using Maes.Utilities;

using UnityEngine;

using static Maes.Map.SlamMap;

namespace Maes.Map.PathFinding
{
    public sealed class MyAStar : IPathFinder
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2Int[]? GetNonBrokenPath<TMap>(Vector2Int startCoordinate, Vector2Int targetCoordinate,
            TMap pathFindingMap, bool beOptimistic = false, bool acceptPartialPaths = false)
        where TMap : IPathFindingMap
        {
            return NewAStar.FindPath(startCoordinate, targetCoordinate, pathFindingMap, beOptimistic, acceptPartialPaths, dependOnBrokenBehaviour: false)?.ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2Int[]? GetOptimisticPath<TMap>(Vector2Int startCoordinate, Vector2Int targetCoordinate, TMap pathFindingMap, bool acceptPartialPaths = false)
            where TMap : IPathFindingMap
        {
            return GetPath(startCoordinate, targetCoordinate, pathFindingMap, beOptimistic: true, acceptPartialPaths: acceptPartialPaths);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2Int[]? GetPath<TMap>(Vector2Int startCoordinate, Vector2Int targetCoordinate, TMap pathFindingMap, bool beOptimistic = false, bool acceptPartialPaths = false)
            where TMap : IPathFindingMap
        {
            return NewAStar.FindPath(startCoordinate, targetCoordinate, pathFindingMap, beOptimistic: beOptimistic, acceptPartialPaths: acceptPartialPaths, dependOnBrokenBehaviour: pathFindingMap.BrokenCollisionMap)?.ToArray();
        }

        // Converts the given A* path to PathSteps (containing a line and a list of all tiles intersected in this path)
        public PathStep[] PathToSteps(Vector2Int[] path)
        {
            if (path.Length == 1)
            {
                return new[] { new PathStep(path[0], path[0], new HashSet<Vector2Int> { path[0] }) };
            }

            var steps = new PathStep[path.Length - 1];

            var stepStart = path[0];
            var currentTile = path[1];
            var crossedTiles = new HashSet<Vector2Int>();
            var currentDirection = CardinalDirection.FromVector(currentTile - stepStart);
            AddIntersectingTiles(stepStart, currentDirection, crossedTiles);

            var span = path.AsSpan(2);
            for (var i = 0; i < span.Length; i++)
            {
                var nextTile = span[i];
                var newDirection = CardinalDirection.FromVector(nextTile - currentTile);
                if (newDirection != currentDirection)
                {
                    // New path step reached
                    steps[i] = new PathStep(stepStart, currentTile, crossedTiles);
                    crossedTiles = new HashSet<Vector2Int>();
                    stepStart = currentTile;
                    currentDirection = newDirection;
                }

                AddIntersectingTiles(currentTile, currentDirection, crossedTiles);
                currentTile = nextTile;
            }

            steps[^1] = new PathStep(stepStart, currentTile, crossedTiles);
            return steps;
        }

        public static List<PathStep> PathToStepsCheap(Vector2Int[] path)
        {
            if (path.Length == 1)
            {
                return new List<PathStep> { new(path[0], path[0], null!) }; // HACK: set to null to avoid allocations.
            }

            var steps = new List<PathStep>();

            var stepStart = path[0];
            var currentTile = path[1];
            var currentDirection = CardinalDirection.FromVector(currentTile - stepStart);

            foreach (var nextTile in path.AsSpan(2))
            {
                var newDirection = CardinalDirection.FromVector(nextTile - currentTile);
                if (newDirection != currentDirection)
                {
                    // New path step reached
                    steps.Add(new PathStep(stepStart, currentTile, null!)); // HACK: set to null to avoid allocations.
                    stepStart = currentTile;
                    currentDirection = newDirection;
                }
                currentTile = nextTile;
            }

            steps.Add(new PathStep(stepStart, currentTile, null!)); // HACK: set to null to avoid allocations.
            return steps;
        }

        private static void AddIntersectingTiles(Vector2Int from, CardinalDirection direction, HashSet<Vector2Int> tiles)
        {
            tiles.Add(from);
            tiles.Add(from + direction.Vector);
            if (direction.IsDiagonal())
            {
                // Two of the neighbouring tiles are also intersected when traversing tiles diagonally 
                tiles.Add(from + direction.Next().Vector);
                tiles.Add(from + direction.Previous().Vector);
            }
        }

        public Vector2Int? GetNearestTileFloodFill<TMap>(TMap pathFindingMap, Vector2Int targetCoordinate, SlamTileStatus lookupStatus, HashSet<Vector2Int>? excludedTiles = null)
            where TMap : IPathFindingMap
        {
            var targetQueue = new Queue<Vector2Int>();
            var visitedTargetsList = new HashSet<Vector2Int>();
            targetQueue.Enqueue(targetCoordinate);

            while (targetQueue.Any())
            {
                var target = targetQueue.Dequeue();
                var neighborHit = IsAnyNeighborStatus(target, pathFindingMap, lookupStatus);
                if (neighborHit.HasValue && (excludedTiles == null || !excludedTiles.Contains(neighborHit.Value)))
                {
                    return neighborHit.Value;
                }

                foreach (var dir in CardinalDirection.CardinalDirections)
                {
                    if (!pathFindingMap.IsWithinBounds(target + dir.Vector)
                        || (excludedTiles != null && excludedTiles.Contains(target + dir.Vector))
                        || pathFindingMap.GetTileStatus(target + dir.Vector) == SlamTileStatus.Solid)
                    {
                        continue;
                    }

                    neighborHit = IsAnyNeighborStatus(target + dir.Vector, pathFindingMap, lookupStatus);
                    if (neighborHit.HasValue && pathFindingMap.IsWithinBounds(target + dir.Vector))
                    {
                        return neighborHit.Value;
                    }

                    if (visitedTargetsList.Contains(target + dir.Vector) || !pathFindingMap.IsWithinBounds(target + dir.Vector))
                    {
                        continue;
                    }
                    targetQueue.Enqueue(target + dir.Vector);
                    visitedTargetsList.Add(target + dir.Vector);
                }
            }

            return null;
        }

        public Vector2Int? IsAnyNeighborStatus<TMap>(Vector2Int targetCoordinate, TMap pathFindingMap, SlamTileStatus status, bool optimistic = false)
            where TMap : IPathFindingMap
        {
            foreach (var dir in CardinalDirection.CardinalDirections)
            {
                if (!pathFindingMap.IsWithinBounds(targetCoordinate + dir.Vector))
                {
                    continue;
                }

                if (pathFindingMap.GetTileStatus(targetCoordinate + dir.Vector, optimistic) == status)
                {
                    return targetCoordinate + dir.Vector;
                }
            }
            return null;
        }
    }
}