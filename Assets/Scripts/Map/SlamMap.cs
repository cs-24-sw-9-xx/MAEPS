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
// Contributors: Rasmus Borrisholt Schmidt, Andreas Sebastian Sørensen, Thor Beregaard, Malte Z. Andreasen, Philip I. Holler, Magnus K. Jensen,
//               Casper Nyvang Sørensen, Christian Ziegler Sejersen, Henrik Van Peet, Jakob Meyer Olsen, Mads Beyer Mogensen and Puvikaran Santhirasegaram 
// 
// Original repository: https://github.com/Molitany/MAES

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

using Maes.Map.Generators;
using Maes.Map.PathFinding;
using Maes.Robot;
using Maes.Utilities;

using UnityEngine;

using Random = System.Random;

namespace Maes.Map
{
    public sealed class SlamMap : ISlamAlgorithm, IPathFindingMap
    {
        [ForbiddenKnowledge]
        public SimulationMap<Tile> CollisionMap { get; }

        [ForbiddenKnowledge]
        public bool BrokenCollisionMap => CollisionMap.BrokenCollisionMap;

        [ForbiddenKnowledge]
        public int LastUpdateTick { get; private set; }

        [ForbiddenKnowledge]
        public int Width { get; }

        [ForbiddenKnowledge]
        public int Height { get; }

        private SlamTileStatus[,] _tiles;
        private readonly VisibleTile[,] _currentlyVisibleTiles;

        [ForbiddenKnowledge]
        public HashSet<int> CurrentlyVisibleTriangles => _currentlyVisibleTriangles ??= new HashSet<int>();

        private HashSet<int>? _currentlyVisibleTriangles;
        private readonly IPathFinder _pathFinder;

        private readonly Vector2 _offset;
        private readonly RobotConstraints _robotConstraints;
        private float _lastInaccuracyX;
        private float _lastInaccuracyY;

        // Represents the current approximate position of the given robot
        public Vector2 ApproximatePosition { get; private set; }
        private float _robotAngle;
        private readonly Random _random;

        // Low resolution map
        public CoarseGrainedMap CoarseMap { get; }
        // Low resolution map only considering what is visible now
        private readonly VisibleTilesCoarseMap _visibleTilesCoarseMap;

        private int _visibleTilesGeneration;

        [ForbiddenKnowledge]
        public SlamMap(SimulationMap<Tile> collisionMap, RobotConstraints robotConstraints, int randomSeed)
        {
            CollisionMap = collisionMap;
            _robotConstraints = robotConstraints;
            Width = collisionMap.WidthInTiles * 2;
            Height = collisionMap.HeightInTiles * 2;
            _offset = collisionMap.ScaledOffset;

            _random = new Random(randomSeed);
            _pathFinder = new MyAStar();

            _tiles = robotConstraints.MapKnown ? SetTilesAsKnownMap(collisionMap) : EmptyMap();
            _currentlyVisibleTiles = new VisibleTile[Width, Height];
            CoarseMap = new CoarseGrainedMap(this, collisionMap.WidthInTiles, collisionMap.HeightInTiles, _offset, robotConstraints.MapKnown);
            _visibleTilesCoarseMap = new VisibleTilesCoarseMap(this, collisionMap.WidthInTiles,
                collisionMap.HeightInTiles, _offset);
        }

        private SlamTileStatus[,] SetTilesAsKnownMap(SimulationMap<Tile> collisionMap)
        {
            var tiles = new SlamTileStatus[Width, Height];
            for (var x = 0; x < Width; x++)
            {
                for (var y = 0; y < Height; y++)
                {
                    var tile = collisionMap.GetTileByLocalCoordinate(x / 2, y / 2);
                    var triangles = tile.GetTriangles();
                    var xIndex = x % 2;
                    var yIndex = y % 2;

                    var index = (xIndex * 2) + (yIndex * 4);
                    var slice = triangles.Skip(index).Take(2);
                    tiles[x, y] = slice.Any(t => Tile.IsWall(t.Type)) ? SlamTileStatus.Solid : SlamTileStatus.Open;
                }
            }

            return tiles;
        }

        private SlamTileStatus[,] EmptyMap()
        {
            var tiles = new SlamTileStatus[Width, Height];
            for (var x = 0; x < Width; x++)
            {
                for (var y = 0; y < Height; y++)
                {
                    tiles[x, y] = SlamTileStatus.Unseen;
                }
            }

            return tiles;
        }

        [ForbiddenKnowledge]
        public Vector2Int TriangleIndexToCoordinate(int triangleIndex)
        {
            var collisionTileIndex = triangleIndex / 8;
            var localTriangleIndex = triangleIndex % 8;
            var collisionX = collisionTileIndex % CollisionMap.WidthInTiles;
            var collisionY = collisionTileIndex / CollisionMap.WidthInTiles;
            // Y offset is 1 if the triangle is the in upper half of the tile
            var yOffset = localTriangleIndex > 3 ? 1 : 0;
            // X offset is 1 if the triangle is in the right half of tile
            var xOffset = (localTriangleIndex % 4 > 1) ? 1 : 0;
            return new Vector2Int((collisionX * 2) + xOffset, (collisionY * 2) + yOffset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [ForbiddenKnowledge]
        public static Vector2Int LocalCoordinateToPathFindingCoordinate(Vector2Int localCoordinate)
        {
            return localCoordinate / 2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [ForbiddenKnowledge]
        public void SetExploredByCoordinate(Vector2Int localCoordinate, bool isOpen, int tick)
        {
            var x = localCoordinate.x;
            var y = localCoordinate.y;
            SetExploredByCoordinate(x, y, isOpen, tick);
        }

        [ForbiddenKnowledge]
        public void SetExploredByCoordinate(int localX, int localY, bool isOpen, int tick)
        {
            ref var status = ref _tiles[localX, localY];

            if (status != SlamTileStatus.Solid)
            {
                var newStatus = isOpen ? SlamTileStatus.Open : SlamTileStatus.Solid;
                if (status != newStatus)
                {
                    status = newStatus;
                    LastUpdateTick = tick;
                }
            }
        }

        public Vector2Int GetCurrentPosition(bool dependOnBrokenPosition = true)
        {
            var currentPosition = ApproximatePosition;
            // Since the resolution of the slam map is double, we round to nearest half
            // This is done by multiplying by 2 and then rounding to nearest number.
            // Dividing by two then gives us number with a possible fraction of 0.5
            // We then multiply by 2 again to get to the right slam tile
            var x = (int)Math.Round(currentPosition.x * 2, MidpointRounding.AwayFromZero);
            var y = (int)Math.Round(currentPosition.y * 2, MidpointRounding.AwayFromZero);
            var slamX = x - ((int)_offset.x * 2);
            var slamY = y - ((int)_offset.y * 2);

            return new Vector2Int(slamX, slamY);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [ForbiddenKnowledge]
        public void ResetRobotVisibility()
        {
            _visibleTilesGeneration++;
            _currentlyVisibleTriangles?.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SlamTileStatus GetVisibleTileStatus(int x, int y)
        {
            ref var visibleTile = ref _currentlyVisibleTiles[x, y];

            if (visibleTile.Generation != _visibleTilesGeneration)
            {
                return SlamTileStatus.Unseen;
            }

            return visibleTile.TileStatus;
        }

        public List<Vector2Int> GetVisibleTiles()
        {
            var visibleTilesList = new List<Vector2Int>();
            for (var x = 0; x < Width; x++)
            {
                for (var y = 0; y < Height; y++)
                {
                    ref var tile = ref _currentlyVisibleTiles[x, y];
                    if (tile.Generation == _visibleTilesGeneration && tile.TileStatus != SlamTileStatus.Unseen)
                    {
                        visibleTilesList.Add(new Vector2Int(x, y));
                    }
                }
            }

            return visibleTilesList;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [ForbiddenKnowledge]
        public void SetCurrentlyVisibleByTriangle(int triangleIndex, Vector2Int localCoordinate, bool isOpen)
        {
            var x = localCoordinate.x;
            var y = localCoordinate.y;

            SetCurrentlyVisibleByTriangle(triangleIndex, x, y, isOpen);
        }

        [ForbiddenKnowledge]
        public void SetCurrentlyVisibleByTriangle(int triangleIndex, int localX, int localY, bool isOpen)
        {
            _currentlyVisibleTriangles?.Add(triangleIndex);

            ref var visibleTile = ref _currentlyVisibleTiles[localX, localY];
            if (visibleTile.Generation != _visibleTilesGeneration || visibleTile.TileStatus != SlamTileStatus.Solid)
            {
                var newStatus = isOpen ? SlamTileStatus.Open : SlamTileStatus.Solid;
                visibleTile = new VisibleTile(_visibleTilesGeneration, newStatus);
                var coarseX = localX >> 1;
                var coarseY = localY >> 1;
                CoarseMap.UpdateTile(coarseX, coarseY, newStatus);
            }
        }

        public SlamTileStatus GetVisibleTileByTriangleIndex(int triangleIndex)
        {
            var localCoordinate = TriangleIndexToCoordinate(triangleIndex);
            var visibleTile = _currentlyVisibleTiles[localCoordinate.x, localCoordinate.y];
            return visibleTile.Generation == _visibleTilesGeneration ? visibleTile.TileStatus : SlamTileStatus.Unseen;
        }

        public SlamTileStatus GetTileByTriangleIndex(int triangleIndex)
        {
            var localCoordinate = TriangleIndexToCoordinate(triangleIndex);
            return _tiles[localCoordinate.x, localCoordinate.y];
        }

        public enum SlamTileStatus
        {
            Unseen,
            Open,
            Solid
        }

        [ForbiddenKnowledge]
        public void UpdateApproxPosition(Vector2 worldPosition)
        {
            // TODO: This looks like a bug
            if (Math.Abs(_robotConstraints.SlamPositionInaccuracy) < 0.0000001f)
            {
                ApproximatePosition = worldPosition;
                return;
            }


            var sign = _random.Next(2) == 1 ? -1 : 1;
            var multiplier = _random.NextDouble() * sign;
            var newInaccuracy = _lastInaccuracyX + (multiplier * (_robotConstraints.SlamPositionInaccuracy / 10f));
            newInaccuracy = Math.Clamp(newInaccuracy, -_robotConstraints.SlamPositionInaccuracy, _robotConstraints.SlamPositionInaccuracy);
            var newXAprox = (float)newInaccuracy + worldPosition.x;
            _lastInaccuracyX = (float)newInaccuracy;

            sign = _random.Next(2) == 1 ? -1 : 1;
            multiplier = _random.NextDouble() * sign;
            newInaccuracy = _lastInaccuracyY + (multiplier * (_robotConstraints.SlamPositionInaccuracy / 10f));
            newInaccuracy = Math.Clamp(newInaccuracy, -_robotConstraints.SlamPositionInaccuracy,
                _robotConstraints.SlamPositionInaccuracy);
            var newYAprox = (float)newInaccuracy + worldPosition.y;
            _lastInaccuracyY = (float)newInaccuracy;

            ApproximatePosition = new Vector2(newXAprox, newYAprox);
        }

        // Synchronizes the given slam maps to create a new one
        public static void Synchronize(List<SlamMap> maps, int tick)
        {
            var globalMap = new SlamTileStatus[maps[0].Width, maps[0].Height];

            foreach (var map in maps)
            {
                for (var x = 0; x < map.Width; x++)
                {
                    for (var y = 0; y < map.Height; y++)
                    {
                        if (map._tiles[x, y] != SlamTileStatus.Unseen && globalMap[x, y] != SlamTileStatus.Solid)
                        {
                            globalMap[x, y] = map._tiles[x, y];
                        }
                    }
                }
            }

            foreach (var map in maps)
            {
                map._tiles = (SlamTileStatus[,])globalMap.Clone();
                map.LastUpdateTick = tick;
            }

            // Synchronize coarse maps
            CoarseGrainedMap.Synchronize(maps.Select(m => m.CoarseMap).ToList(), globalMap);
        }


        /// <summary>
        /// Synchronizes one map with a list of other <see cref="SlamMap"/>s.
        /// </summary>s
        /// 
        public static void Combine(SlamMap target, List<SlamMap> others, int tick)
        {
            var globalMap = new SlamTileStatus[target.Width, target.Height];

            foreach (var other in others)
            {
                for (var x = 0; x < target.Width; x++)
                {
                    for (var y = 0; y < target.Height; y++)
                    {
                        if (other._tiles[x, y] != SlamTileStatus.Unseen)
                        {
                            globalMap[x, y] = target._tiles[x, y];
                        }
                    }
                }
            }

            target._tiles = (SlamTileStatus[,])globalMap.Clone();
            target.LastUpdateTick = tick;
            CoarseGrainedMap.Combine(target.CoarseMap, others.Select(o => o.CoarseMap).ToList(), globalMap);
        }

        public Dictionary<Vector2Int, SlamTileStatus> GetExploredTiles()
        {
            var res = new Dictionary<Vector2Int, SlamTileStatus>();

            for (var x = 0; x < Width; x++)
            {
                for (var y = 0; y < Height; y++)
                {
                    if (_tiles[x, y] != SlamTileStatus.Unseen)
                    {
                        res[new Vector2Int(x, y)] = _tiles[x, y];
                    }
                }
            }

            return res;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SlamTileStatus GetTileStatus(Vector2Int tile, bool optimistic = false)
        {
            return _tiles[tile.x, tile.y];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SlamTileStatus GetTileStatus(int x, int y)
        {
            return _tiles[x, y];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2Int? GetNearestTileFloodFill(Vector2Int targetCoordinate, SlamTileStatus lookupStatus, HashSet<Vector2Int>? excludedTiles = null)
        {
            return _pathFinder.GetNearestTileFloodFill(this, targetCoordinate, lookupStatus, excludedTiles);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetRobotAngleDeg()
        {
            return _robotAngle;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [ForbiddenKnowledge]
        public void SetApproxRobotAngle(float robotAngle)
        {
            _robotAngle = robotAngle;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsWithinBounds(Vector2Int slamCoordinate)
        {
            var x = slamCoordinate.x;
            var y = slamCoordinate.y;

            return IsWithinBounds(x, y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsWithinBounds(int x, int y)
        {
            return x > 0 && x < Width &&
                   y > 0 && y < Height;
        }

        public bool IsOptimisticSolid(Vector2Int coordinate)
        {
            var x = coordinate.x * 2;
            var y = coordinate.y * 2;

            // Tiles outside map bounds are considered solid
            if (!IsWithinBounds(x, y))
            {
                return true;
            }

            var status = AggregateStatusOptimistic(_tiles[x, y], _tiles[x + 1, y]);
            status = AggregateStatusOptimistic(status, _tiles[x, y + 1]);
            status = AggregateStatusOptimistic(status, _tiles[x + 1, y + 1]);
            return status == SlamTileStatus.Solid || status == SlamTileStatus.Unseen;

        }

        // Combines two SlamTileStatus in a 'optimistic' fashion.
        // If any status is solid both are consider solid. Otherwise if any status is open both are considered open 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static SlamTileStatus AggregateStatusOptimistic(SlamTileStatus status1, SlamTileStatus status2)
        {
            if (status1 == SlamTileStatus.Solid || status2 == SlamTileStatus.Solid)
            {
                return SlamTileStatus.Solid;
            }

            if (status1 == SlamTileStatus.Open || status2 == SlamTileStatus.Open)
            {
                return SlamTileStatus.Open;
            }

            return SlamTileStatus.Unseen;
        }

        /// <summary>
        /// Gets the solid state of a tile
        /// </summary>
        /// <param name="coordinate">COARSEGRAINED coordinate</param>
        /// <returns></returns>
        public bool IsSolid(Vector2Int coordinate)
        {
            var x = coordinate.x * 2;
            var y = coordinate.y * 2;

            // Tiles outside map bounds are considered solid
            if (!IsWithinBounds(x, y))
            {
                return true;
            }

            var isTraversable = _tiles[x, y] == SlamTileStatus.Open
            && _tiles[x + 1, y] == SlamTileStatus.Open
            && _tiles[x, y + 1] == SlamTileStatus.Open
            && _tiles[x + 1, y + 1] == SlamTileStatus.Open;
            return !isTraversable;
        }

        public bool IsUnseenSemiOpen(Vector2Int nextCoordinate, Vector2Int currentCoordinate)
        {
            return true;
        }

        // Returns position of the given tile relative to the current position of the robot
        public RelativePosition GetRelativeSlamPosition(Vector2Int slamTileTarget)
        {
            // Convert to local coordinate
            var robotPosition = GetCurrentPosition();
            var distance = Vector2.Distance(robotPosition, slamTileTarget);
            var angle = Vector2.SignedAngle(Geometry.DirectionAsVector(GetRobotAngleDeg()), slamTileTarget - robotPosition);
            return new RelativePosition(distance, angle);
        }

        [ForbiddenKnowledge]
        public float CellSize => 1f;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2Int[]? GetPath(Vector2Int coarseTileFrom, Vector2Int coarseTileTo, bool acceptPartialPaths = false)
        {
            var path = _pathFinder.GetPath(coarseTileFrom, coarseTileTo, this, acceptPartialPaths: acceptPartialPaths);

            if (path == null)
            {
                return null;
            }

            // Due to rounding errors when converting slam tiles to path tiles, the target may not be correct
            // This replaces the final tile with the actual target.
            path[^1] = coarseTileTo;

            return path;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2Int[]? GetOptimisticPath(Vector2Int coarseTileFrom, Vector2Int coarseTileTo, bool acceptPartialPaths = false)
        {
            var path = _pathFinder.GetOptimisticPath(coarseTileFrom, coarseTileTo, this, acceptPartialPaths: acceptPartialPaths);

            if (path == null)
            {
                return null;
            }

            // Due to rounding errors when converting slam tiles to path tiles, the target may not be correct
            // This replaces the final tile with the actual target.
            path[^1] = coarseTileTo;

            return path;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SlamTileStatus[,] GetTileStatuses()
        {
            return _tiles;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public VisibleTilesCoarseMap GetVisibleTilesCoarseMap()
        {
            return _visibleTilesCoarseMap;
        }

        public Vector2 SlamToWorldCoordinate(Vector2Int slamCoordinate)
        {
            var slamToWorld = new Vector2Int(slamCoordinate.x / 2, slamCoordinate.y / 2);
            var offset = new Vector2(0.5f, 0.5f);
            var worldCoordinate = slamToWorld + CollisionMap.ScaledOffset + offset;
            if (!IsWithinBounds(slamCoordinate))
            {
                throw new ArgumentException("The given coordinate " + slamCoordinate
                                                                    + "(World coordinate:" + worldCoordinate + " )"
                                                                    + " is not within map bounds.");
            }
            return worldCoordinate;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 TileToWorld(Vector2 tile)
        {
            var worldTile = tile / 2;
            return new Vector3(worldTile.x, worldTile.y, -0.01f) + (Vector3)_offset;
        }

        private readonly struct VisibleTile : IEquatable<VisibleTile>
        {
            public readonly int Generation;
            public readonly SlamTileStatus TileStatus;

            public VisibleTile(int generation, SlamTileStatus tileStatus)
            {
                Generation = generation;
                TileStatus = tileStatus;
            }

            public bool Equals(VisibleTile other)
            {
                return Generation == other.Generation && TileStatus == other.TileStatus;
            }

            public override bool Equals(object? obj)
            {
                return obj is VisibleTile other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Generation, (int)TileStatus);
            }

            public static bool operator ==(VisibleTile left, VisibleTile right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(VisibleTile left, VisibleTile right)
            {
                return !left.Equals(right);
            }
        }
    }
}