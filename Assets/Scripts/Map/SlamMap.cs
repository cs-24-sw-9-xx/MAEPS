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
using Maes.Map.MapGen;
using Maes.Map.PathFinding;
using Maes.Robot;
using Maes.Utilities;
using UnityEngine;
using Random = System.Random;

namespace Maes.Map
{
    public class SlamMap : ISlamAlgorithm, IPathFindingMap
    {
        // Size of a tile in world space
        private readonly float _tileSize;
        private readonly int _widthInTiles, _heightInTiles;

        private SlamTileStatus[,] _tiles;
        public Dictionary<Vector2Int, SlamTileStatus> CurrentlyVisibleTiles;
        public readonly HashSet<int> CurrentlyVisibleTriangles = new();
        private readonly SimulationMap<Tile> _collisionMap;
        private readonly IPathFinder _pathFinder;

        private readonly Vector2 _offset;
        private readonly RobotConstraints _robotConstraints;
        private float _lastInaccuracyX ;
        private float _lastInaccuracyY;

        // Represents the current approximate position of the given robot
        public Vector2 ApproximatePosition { get; private set; }
        private float _robotAngle;
        private readonly Random _random;

        // Low resolution map
        internal readonly CoarseGrainedMap CoarseMap;
        // Low resolution map only considering what is visible now
        private readonly VisibleTilesCoarseMap _visibleTilesCoarseMap;

        public SlamMap(SimulationMap<Tile> collisionMap, RobotConstraints robotConstraints, int randomSeed)
        {
            _collisionMap = collisionMap;
            _robotConstraints = robotConstraints;
            _widthInTiles = collisionMap.WidthInTiles * 2;
            _heightInTiles = collisionMap.HeightInTiles * 2;
            _offset = collisionMap.ScaledOffset;

            CurrentlyVisibleTiles = new Dictionary<Vector2Int, SlamTileStatus>();
            _random = new Random(randomSeed);
            _pathFinder = new AStar();

            _tiles = robotConstraints.MapKnown ? SetTilesAsKnownMap(collisionMap) : EmptyMap();
            CoarseMap = new CoarseGrainedMap(this, collisionMap.WidthInTiles, collisionMap.HeightInTiles, _offset, robotConstraints.MapKnown);
            _visibleTilesCoarseMap = new VisibleTilesCoarseMap(this, collisionMap.WidthInTiles,
                collisionMap.HeightInTiles, _offset);
        }
        
        private SlamTileStatus[,] SetTilesAsKnownMap(SimulationMap<Tile> collisionMap)
        {
            var tiles = new SlamTileStatus[_widthInTiles, _heightInTiles];
            for (var x = 0; x < _widthInTiles; x++)
            {
                for (var y = 0; y < _heightInTiles; y++)
                {
                    var tile = collisionMap.GetTileByLocalCoordinate(x / 2, y / 2);
                    var triangles = tile.GetTriangles();
                    var xIndex = x % 2;
                    var yIndex = y % 2;

                    var index = xIndex * 2 + yIndex * 4;
                    var slice = triangles.Skip(index).Take(2);
                    tiles[x, y] = slice.Any(t => Tile.IsWall(t.Type)) ? SlamTileStatus.Solid : SlamTileStatus.Open;
                }
            }

            return tiles;
        }
        
        private SlamTileStatus[,] EmptyMap()
        {
            var tiles = new SlamTileStatus[_widthInTiles, _heightInTiles];
            for (var x = 0; x < _widthInTiles; x++)
                for (var y = 0; y < _heightInTiles; y++)
                    tiles[x, y] = SlamTileStatus.Unseen;

            return tiles;
        }

        public Vector2Int TriangleIndexToCoordinate(int triangleIndex)
        {
            var collisionTileIndex = triangleIndex / 8;
            var localTriangleIndex = triangleIndex % 8;
            var collisionX = collisionTileIndex % _collisionMap.WidthInTiles;
            var collisionY = collisionTileIndex / _collisionMap.WidthInTiles;
            // Y offset is 1 if the triangle is the in upper half of the tile
            var yOffset = localTriangleIndex > 3 ? 1 : 0;
            // X offset is 1 if the triangle is in the right half of tile
            var xOffset = (localTriangleIndex % 4 > 1) ? 1 : 0;
            return new Vector2Int((collisionX * 2) + xOffset, (collisionY * 2) + yOffset);
        }

        public Vector2Int LocalCoordinateToPathFindingCoordinate(Vector2Int localCoordinate)
        {
            return localCoordinate / 2;
        }

        public void SetExploredByTriangle(int triangleIndex, bool isOpen)
        {
            var localCoordinate = TriangleIndexToCoordinate(triangleIndex);
            if (_tiles[localCoordinate.x, localCoordinate.y] != SlamTileStatus.Solid)
                _tiles[localCoordinate.x, localCoordinate.y] = isOpen ? SlamTileStatus.Open : SlamTileStatus.Solid;
        }

        public Vector2Int GetCurrentPosition()
        {
            var currentPosition = GetApproxPosition();
            // Since the resolution of the slam map is double, we round to nearest half
            // This is done by multiplying by 2 and then rounding to nearest number.
            // Dividing by two then gives us number with a possible fraction of 0.5
            // We then multiply by 2 again to get to the right slam tile
            var x = (int)Math.Round(currentPosition.x * 2, MidpointRounding.AwayFromZero);
            var y = (int)Math.Round(currentPosition.y * 2, MidpointRounding.AwayFromZero);
            var slamX = x - (int)_offset.x * 2;
            var slamY = y - (int)_offset.y * 2;

            return new Vector2Int(slamX, slamY);
        }

        public void ResetRobotVisibility()
        {
            CurrentlyVisibleTiles = new Dictionary<Vector2Int, SlamTileStatus>();
            CurrentlyVisibleTriangles.Clear();
        }

        public void SetCurrentlyVisibleByTriangle(int triangleIndex, bool isOpen)
        {
            var localCoordinate = TriangleIndexToCoordinate(triangleIndex);
            CurrentlyVisibleTriangles.Add(triangleIndex);

            if (!CurrentlyVisibleTiles.ContainsKey(localCoordinate))
            {
                var newStatus = isOpen ? SlamTileStatus.Open : SlamTileStatus.Solid;
                CurrentlyVisibleTiles[localCoordinate] = newStatus;
                CoarseMap.UpdateTile(CoarseMap.FromSlamMapCoordinate(localCoordinate), newStatus);
            }
            else if (CurrentlyVisibleTiles[localCoordinate] != SlamTileStatus.Solid)
            {
                var newStatus = isOpen ? SlamTileStatus.Open : SlamTileStatus.Solid;
                CurrentlyVisibleTiles[localCoordinate] = newStatus;
                CoarseMap.UpdateTile(CoarseMap.FromSlamMapCoordinate(localCoordinate), newStatus);
            }

        }

        public SlamTileStatus GetVisibleTileByTriangleIndex(int triangleIndex)
        {
            var localCoordinate = TriangleIndexToCoordinate(triangleIndex);
            return CurrentlyVisibleTiles.GetValueOrDefault(localCoordinate, SlamTileStatus.Unseen);
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

        public void UpdateApproxPosition(Vector2 worldPosition)
        {
            if (Math.Abs(_robotConstraints.SlamPositionInaccuracy) < 0.0000001f)
            {
                this.ApproximatePosition = worldPosition;
                return;
            }


            var sign = _random.Next(2) == 1 ? -1 : 1;
            var multiplier = _random.NextDouble() * sign;
            var newInaccuracy = _lastInaccuracyX + multiplier * (_robotConstraints.SlamPositionInaccuracy / 10f);
            newInaccuracy = MathUtilities.Clamp(newInaccuracy, -_robotConstraints.SlamPositionInaccuracy,
                _robotConstraints.SlamPositionInaccuracy);
            var newXAprox = (float)newInaccuracy + worldPosition.x;
            _lastInaccuracyX = (float)newInaccuracy;

            sign = _random.Next(2) == 1 ? -1 : 1;
            multiplier = _random.NextDouble() * sign;
            newInaccuracy = _lastInaccuracyY + multiplier * (_robotConstraints.SlamPositionInaccuracy / 10f);
            newInaccuracy = MathUtilities.Clamp(newInaccuracy, -_robotConstraints.SlamPositionInaccuracy,
                _robotConstraints.SlamPositionInaccuracy);
            var newYAprox = (float)newInaccuracy + worldPosition.y;
            _lastInaccuracyY = (float)newInaccuracy;

            this.ApproximatePosition = new Vector2(newXAprox, newYAprox);
        }

        // Synchronizes the given slam maps to create a new one
        public static void Synchronize(List<SlamMap> maps)
        {
            var globalMap = new SlamTileStatus[maps[0]._widthInTiles, maps[0]._heightInTiles];

            foreach (var map in maps)
            {
                for (int x = 0; x < map._widthInTiles; x++)
                {
                    for (int y = 0; y < map._heightInTiles; y++)
                    {
                        if (map._tiles[x, y] != SlamTileStatus.Unseen && globalMap[x, y] != SlamTileStatus.Solid)
                            globalMap[x, y] = map._tiles[x, y];
                    }
                }
            }

            foreach (var map in maps)
                map._tiles = (SlamTileStatus[,])globalMap.Clone();

            // Synchronize coarse maps
            CoarseGrainedMap.Synchronize(maps.Select(m => m.CoarseMap).ToList(), globalMap);
        }


        /// <summary>
        /// Synchronizes one map with a list of other <see cref="SlamMap"/>s.
        /// </summary>s
        /// 
        public static void Combine(SlamMap target, List<SlamMap> others)
        {
            var globalMap = new SlamTileStatus[target._widthInTiles, target._heightInTiles];

            foreach (var other in others)
            {
                for (int x = 0; x < target._widthInTiles; x++)
                {
                    for (int y = 0; y < target._heightInTiles; y++)
                    {
                        if (other._tiles[x, y] != SlamTileStatus.Unseen)
                            globalMap[x, y] = target._tiles[x, y];
                    }
                }
            }

            target._tiles = (SlamTileStatus[,])globalMap.Clone();
            CoarseGrainedMap.Combine(target.CoarseMap, others.Select(o => o.GetCoarseMap()).ToList(), globalMap);
        }

        public Vector2 GetApproxPosition()
        {
            return ApproximatePosition;
        }

        public Dictionary<Vector2Int, SlamTileStatus> GetExploredTiles()
        {
            var res = new Dictionary<Vector2Int, SlamTileStatus>();

            for (int x = 0; x < _widthInTiles; x++)
            {
                for (int y = 0; y < _heightInTiles; y++)
                {
                    if (_tiles[x, y] != SlamTileStatus.Unseen)
                        res[new Vector2Int(x, y)] = _tiles[x, y];
                }
            }

            return res;
        }

        public Dictionary<Vector2Int, SlamTileStatus> GetCurrentlyVisibleTiles()
        {
            return CurrentlyVisibleTiles;
        }

        public SlamTileStatus GetTileStatus(Vector2Int tile, bool optimistic = false)
        {
            return _tiles[tile.x, tile.y];
        }

        public Vector2Int? GetNearestTileFloodFill(Vector2Int targetCoordinate, SlamTileStatus lookupStatus, HashSet<Vector2Int>? excludedTiles = null)
        {
            return _pathFinder.GetNearestTileFloodFill(this, targetCoordinate, lookupStatus, excludedTiles);
        }

        public float GetRobotAngleDeg()
        {
            return _robotAngle;
        }

        public void SetApproxRobotAngle(float robotAngle)
        {
            _robotAngle = robotAngle;
        }

        public bool IsWithinBounds(Vector2Int slamCoordinate)
        {
            return slamCoordinate.x > 0 && slamCoordinate.x < _widthInTiles &&
                   slamCoordinate.y > 0 && slamCoordinate.y < _heightInTiles;
        }

        public bool IsCoordWithinBounds(Vector2Int coordinate)
        {
            return false;
        }

        public bool IsOptimisticSolid(Vector2Int coordinate)
        {
            var slamCoordinate = coordinate * 2;
            slamCoordinate = new Vector2Int(slamCoordinate.x, slamCoordinate.y);

            if (IsWithinBounds(slamCoordinate))
            {
                var status = AggregateStatusOptimistic(_tiles[slamCoordinate.x, slamCoordinate.y], _tiles[slamCoordinate.x + 1, slamCoordinate.y]);
                status = AggregateStatusOptimistic(status, _tiles[slamCoordinate.x, slamCoordinate.y + 1]);
                status = AggregateStatusOptimistic(status, _tiles[slamCoordinate.x + 1, slamCoordinate.y + 1]);
                return status == SlamTileStatus.Solid || status == SlamTileStatus.Unseen;
            }

            // Tiles outside map bounds are considered solid
            return true;
        }

        // Combines two SlamTileStatus in a 'optimistic' fashion.
        // If any status is solid both are consider solid. Otherwise if any status is open both are considered open 
        private SlamTileStatus AggregateStatusOptimistic(SlamTileStatus status1, SlamTileStatus status2)
        {
            if (status1 == SlamTileStatus.Solid || status2 == SlamTileStatus.Solid)
                return SlamTileStatus.Solid;
            if (status1 == SlamTileStatus.Open || status2 == SlamTileStatus.Open)
                return SlamTileStatus.Open;

            return SlamTileStatus.Unseen;
        }

        /// <summary>
        /// Gets the solid state of a tile
        /// </summary>
        /// <param name="coordinate">COARSEGRAINED coordinate</param>
        /// <returns></returns>
        public bool IsSolid(Vector2Int coordinate)
        {
            var slamCoordinate = coordinate * 2;

            if (IsWithinBounds(slamCoordinate))
            {
                var isTraversable = _tiles[slamCoordinate.x, slamCoordinate.y] == SlamTileStatus.Open;
                isTraversable &= _tiles[slamCoordinate.x + 1, slamCoordinate.y] == SlamTileStatus.Open;
                isTraversable &= _tiles[slamCoordinate.x, slamCoordinate.y + 1] == SlamTileStatus.Open;
                isTraversable &= _tiles[slamCoordinate.x + 1, slamCoordinate.y + 1] == SlamTileStatus.Open;
                return !isTraversable;
            }

            // Tiles outside map bounds are considered solid
            return true;
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

        public float CellSize()
        {
            return 1f;
        }

        public List<Vector2Int>? GetPath(Vector2Int coarseTileFrom, Vector2Int coarseTileTo, bool acceptPartialPaths = false)
        {
            var path = _pathFinder.GetPath(coarseTileFrom, coarseTileTo, this, acceptPartialPaths);

            if (path == null)
                return null;

            // Due to rounding errors when converting slam tiles to path tiles, the target may not be correct
            // This replaces the final tile with the actual target.
            path[^1] = coarseTileTo;

            return path;
        }

        public List<Vector2Int>? GetOptimisticPath(Vector2Int coarseTileFrom, Vector2Int coarseTileTo, bool acceptPartialPaths = false)
        {
            var path = _pathFinder.GetOptimisticPath(coarseTileFrom, coarseTileTo, this, acceptPartialPaths);

            if (path == null)
                return null;

            // Due to rounding errors when converting slam tiles to path tiles, the target may not be correct
            // This replaces the final tile with the actual target.
            path[^1] = coarseTileTo;

            return path;
        }

        public CoarseGrainedMap GetCoarseMap()
        {
            return CoarseMap;
        }

        public SlamTileStatus[,] GetTileStatuses() => _tiles;

        public VisibleTilesCoarseMap GetVisibleTilesCoarseMap()
        {
            return _visibleTilesCoarseMap;
        }
        public Vector2 SlamToWorldCoordinate(Vector2Int slamCoordinate)
        {
            var slamToWorld = new Vector2Int(slamCoordinate.x / 2, slamCoordinate.y / 2);
            var offset = new Vector2(0.5f, 0.5f);
            var worldCoordinate = slamToWorld + _collisionMap.ScaledOffset + offset;
            if (!IsWithinBounds(slamCoordinate))
            {
                throw new ArgumentException("The given coordinate " + slamCoordinate
                                                                    + "(World coordinate:" + worldCoordinate + " )"
                                                                    + " is not within map bounds.");
            }
            return worldCoordinate;
        }

        public Vector3 TileToWorld(Vector2 tile)
        {
            var WorldTile = tile / 2;
            return new Vector3(WorldTile.x, WorldTile.y, -0.01f) + (Vector3)_offset;
        }

    }
}
