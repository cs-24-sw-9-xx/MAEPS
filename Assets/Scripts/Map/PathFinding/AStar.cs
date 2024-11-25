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
using Maes.Utilities.Priority_Queue;

using UnityEngine;

using static Maes.Map.SlamMap;

namespace Maes.Map.PathFinding
{
    public class AStar : IPathFinder
    {
        private class AStarTile
        {
            public readonly int X, Y;
            public readonly float Heuristic;
            public readonly float Cost;
            public readonly float TotalCost;

            private readonly AStarTile? _parent;
            private readonly int _depth;

            public AStarTile(int x, int y, AStarTile? parent, float heuristic, float cost)
            {
                X = x;
                Y = y;
                _parent = parent;
                Heuristic = heuristic;
                Cost = cost;
                TotalCost = cost + heuristic;
                _depth = parent?._depth + 1 ?? 0;
            }

            public Vector2Int[] Path()
            {
                var path = new Vector2Int[_depth + 1];
                var i = _depth;

                var current = this;
                while (current != null)
                {
                    path[i--] = new Vector2Int(current.X, current.Y);
                    current = current._parent;
                }

                return path;
            }
        }

        public Vector2Int[]? GetOptimisticPath(Vector2Int startCoordinate, Vector2Int targetCoordinate, IPathFindingMap pathFindingMap, bool acceptPartialPaths = false)
        {
            return GetPath(startCoordinate, targetCoordinate, pathFindingMap, beOptimistic: true, acceptPartialPaths: acceptPartialPaths);
        }

        public Vector2Int[]? GetPath(Vector2Int startCoordinate, Vector2Int targetCoordinate, IPathFindingMap pathFindingMap, bool beOptimistic = false, bool acceptPartialPaths = false)
        {
            var candidates = new SimplePriorityQueue<AStarTile, float>();
            var bestCandidateOnTile = new Dictionary<Vector2Int, AStarTile>();
            var startTileHeuristic = OctileHeuristic(startCoordinate, targetCoordinate);
            var startingTile = new AStarTile(startCoordinate.x, startCoordinate.y, null, startTileHeuristic, 0);
            candidates.Enqueue(startingTile, startingTile.TotalCost);
            bestCandidateOnTile[startCoordinate] = startingTile;

            if (!IsAnyNeighborStatus(targetCoordinate, pathFindingMap, SlamTileStatus.Open).HasValue)
            {
                var nearestTile = GetNearestTileFloodFill(pathFindingMap, targetCoordinate, SlamTileStatus.Open);
                targetCoordinate = nearestTile ?? targetCoordinate;
            }

            var loopCount = 0;
            while (candidates.Count > 0)
            {
                var currentTile = candidates.Dequeue();
                var currentCoordinate = new Vector2Int(currentTile.X, currentTile.Y);

                // Skip if a better candidate has been added to the queue since this was added
                if (bestCandidateOnTile.TryGetValue(currentCoordinate, out var betterCandidate) && betterCandidate != currentTile)
                {
                    continue;
                }

                if (currentCoordinate == targetCoordinate)
                {
                    return currentTile.Path();
                }

                foreach (var dir in CardinalDirection.GetCardinalAndOrdinalDirections())
                {
                    var candidateCoord = currentCoordinate + dir.Vector;
                    // Only consider non-solid tiles
                    if (IsSolid(candidateCoord, pathFindingMap, beOptimistic) && candidateCoord != targetCoordinate)
                    {
                        continue;
                    }

                    if (dir.IsDiagonal())
                    {
                        // To travel diagonally, the two neighbouring tiles must also be free
                        if (IsSolid(currentCoordinate + dir.Previous().Vector, pathFindingMap, beOptimistic)
                        || IsSolid(currentCoordinate + dir.Next().Vector, pathFindingMap, beOptimistic))
                        {
                            continue;
                        }
                    }

                    var cost = currentTile.Cost + Vector2Int.Distance(currentCoordinate, candidateCoord);
                    var heuristic = OctileHeuristic(candidateCoord, targetCoordinate);
                    var candidateCost = cost + heuristic;
                    // Check if this path is 'cheaper' than any previous path to this candidate tile 
                    if (!bestCandidateOnTile.TryGetValue(candidateCoord, out var bestCandidate) || bestCandidate.TotalCost > candidateCost)
                    {
                        var newTile = new AStarTile(candidateCoord.x, candidateCoord.y, currentTile, heuristic, cost);
                        // Save this as the new best candidate for this tile
                        bestCandidateOnTile[candidateCoord] = newTile;
                        candidates.Enqueue(newTile, newTile.TotalCost);
                    }
                }

                if (loopCount > 100000)
                {
                    Debug.Log($"A star loop count exceeded 100000, stopping pathfinding prematurely. [{startCoordinate} -> {targetCoordinate}]");
                    return null;
                }


                loopCount++;
            }

            if (acceptPartialPaths)
            {
                // Find lowest heuristic tile, as it is closest to the target
                Vector2Int? lowestHeuristicKey = null;
                var lowestHeuristic = float.MaxValue;
                foreach (var kv in bestCandidateOnTile)
                {
                    if (kv.Value.Heuristic < lowestHeuristic)
                    {
                        if (kv.Key == startCoordinate)
                        {
                            continue;
                        }

                        lowestHeuristic = kv.Value.Heuristic;
                        lowestHeuristicKey = kv.Key;
                    }
                }

                var closestTile = bestCandidateOnTile[lowestHeuristicKey!.Value];
                return GetPath(startCoordinate, new Vector2Int(closestTile.X, closestTile.Y),
                    pathFindingMap, beOptimistic);
            }
            return null;
        }

        private AStarTile DequeueBestCandidate(List<AStarTile> candidates)
        {
            var bestCandidate = candidates.First();
            foreach (var current in candidates.Skip(1))
            {
                if (Mathf.Abs(current.TotalCost - bestCandidate.TotalCost) < 0.01f)
                {
                    // Total cost is the same, compare by heuristic instead
                    if (current.Heuristic < bestCandidate.Heuristic)
                    {
                        bestCandidate = current;
                    }
                }
                else if (current.TotalCost < bestCandidate.TotalCost)
                {
                    bestCandidate = current;
                }
            }

            candidates.Remove(bestCandidate);
            return bestCandidate;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsSolid(Vector2Int coord, IPathFindingMap map, bool optimistic)
        {
            return optimistic
                ? map.IsOptimisticSolid(coord)
                : map.IsSolid(coord);
        }

        private static float OctileHeuristic(Vector2Int from, Vector2Int to)
        {
            var xDif = Math.Abs(from.x - to.x);
            var yDif = Math.Abs(from.y - to.y);

            var minDif = Math.Min(xDif, yDif);
            var maxDif = Math.Max(xDif, yDif);

            var heuristic = maxDif - minDif + minDif * Mathf.Sqrt(2f);
            return heuristic;
        }

        // Converts the given A* path to PathSteps (containing a line and a list of all tiles intersected in this path)
        public List<PathStep> PathToSteps(Vector2Int[] path)
        {
            if (path.Length == 1)
            {
                return new List<PathStep> { new(path[0], path[0], new HashSet<Vector2Int> { path[0] }) };
            }

            var steps = new List<PathStep>();

            var stepStart = path[0];
            var currentTile = path[1];
            var crossedTiles = new HashSet<Vector2Int>();
            var currentDirection = CardinalDirection.FromVector(currentTile - stepStart);
            AddIntersectingTiles(stepStart, currentDirection, crossedTiles);

            foreach (var nextTile in path.AsSpan(2))
            {
                var newDirection = CardinalDirection.FromVector(nextTile - currentTile);
                if (newDirection != currentDirection)
                {
                    // New path step reached
                    steps.Add(new PathStep(stepStart, currentTile, crossedTiles));
                    crossedTiles = new HashSet<Vector2Int>();
                    stepStart = currentTile;
                    currentDirection = newDirection;
                }
                AddIntersectingTiles(currentTile, currentDirection, crossedTiles);
                currentTile = nextTile;
            }

            steps.Add(new PathStep(stepStart, currentTile, crossedTiles));
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

        public static void AddIntersectingTiles(Vector2Int from, CardinalDirection direction, HashSet<Vector2Int> tiles)
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

        public Vector2Int? GetNearestTileFloodFill(IPathFindingMap pathFindingMap, Vector2Int targetCoordinate, SlamTileStatus lookupStatus, HashSet<Vector2Int>? excludedTiles = null)
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

                var directions = CardinalDirection.GetCardinalDirections();
                foreach (var dir in directions)
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

        public Vector2Int? IsAnyNeighborStatus(Vector2Int targetCoordinate, IPathFindingMap pathFindingMap, SlamTileStatus status, bool optimistic = false)
        {
            var directions = CardinalDirection.GetCardinalDirections();
            foreach (var dir in directions)
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