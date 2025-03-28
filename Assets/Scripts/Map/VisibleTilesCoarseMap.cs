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

using Maes.Map.PathFinding;

using UnityEngine;

namespace Maes.Map
{
    public sealed class VisibleTilesCoarseMap : IPathFindingMap
    {
        private readonly SlamMap _slamMap;
        private readonly Vector2 _offset;
        private readonly MyAStar _aStar;

        public bool BrokenCollisionMap => _slamMap.BrokenCollisionMap;
        public int LastUpdateTick => _slamMap.LastUpdateTick;
        public int Width { get; }
        public int Height { get; }


        public VisibleTilesCoarseMap(SlamMap slamMap, int width, int height, Vector2 offset)
        {
            _slamMap = slamMap;
            Width = width;
            Height = height;
            _offset = offset;
            _aStar = new MyAStar();
        }

        public Vector2Int[]? GetPath(Vector2Int coarseTileFrom, Vector2Int coarseTileTo, bool acceptPartialPaths = false)
        {
            var path = _aStar.GetPath(coarseTileFrom, coarseTileTo, this, true, acceptPartialPaths);

            if (path == null)
            {
                return null;
            }

            // Due to rounding errors when converting slam tiles to path tiles, the target may not be correct
            // This replaces the final tile with the actual target.
            path[^1] = coarseTileTo;

            return path;
        }

        public bool IsSolid(Vector2Int coordinate)
        {
            var slamTile = ToSlamMapCoordinate(coordinate);

            // Anything not currently visible is solid
            if (_slamMap.GetVisibleTileStatus(slamTile.x, slamTile.y) == SlamMap.SlamTileStatus.Unseen)
            {
                return true;
            }

            // Check if the coarse tile is actually solid
            var tileStatus = GetTileStatus(coordinate);
            if (tileStatus == SlamMap.SlamTileStatus.Solid)
            {
                return true;
            }

            return false;
        }
        public bool IsUnseenSemiOpen(Vector2Int nextCoordinate, Vector2Int currentCoordinate)
        {
            return true;
        }

        public PathStep[]? GetPathSteps(Vector2Int coarseTileFrom, Vector2Int coarseTileTo, bool acceptPartialPaths = false)
        {
            var path = _aStar.GetOptimisticPath(coarseTileFrom, coarseTileTo, this, acceptPartialPaths);
            return path == null ? null : _aStar.PathToSteps(path);
        }

        public bool IsOptimisticSolid(Vector2Int coordinate)
        {
            var slamTile = ToSlamMapCoordinate(coordinate);

            // Anything not currently visible is not solid
            if (_slamMap.GetVisibleTileStatus(slamTile.x, slamTile.y) == SlamMap.SlamTileStatus.Unseen)
            {
                return false;
            }

            // Check if the coarse tile is actually solid
            var tileStatus = GetTileStatus(coordinate, true);
            if (tileStatus == SlamMap.SlamTileStatus.Solid)
            {
                return true;
            }

            return false;
        }

        public float CellSize => 1.0f;

        public bool IsCoordWithinBounds(Vector2Int coordinate)
        {
            return false;
        }


        public Vector2Int FromSlamMapCoordinate(Vector2Int slamCoord)
        {
            return slamCoord / 2;
        }

        public static Vector2Int ToSlamMapCoordinate(Vector2Int localCoordinate)
        {
            return localCoordinate * 2;
        }

        public bool IsWithinBounds(Vector2Int coordinate)
        {
            return coordinate.x >= 0 && coordinate.x < Width && coordinate.y >= 0 && coordinate.y < Height;
        }

        // Returns the status of the given tile (Solid, Open or Unseen)
        public SlamMap.SlamTileStatus GetTileStatus(Vector2Int localCoordinate, bool optimistic = false)
        {
            var slamCoord = ToSlamMapCoordinate(localCoordinate);

            var status = _slamMap.GetTileStatus(slamCoord);
            if (optimistic)
            {
                status = AggregateStatusOptimistic(status, _slamMap.GetTileStatus(slamCoord + Vector2Int.right));
                status = AggregateStatusOptimistic(status, _slamMap.GetTileStatus(slamCoord + Vector2Int.up));
                status = AggregateStatusOptimistic(status, _slamMap.GetTileStatus(slamCoord + Vector2Int.right + Vector2Int.up));
            }
            else
            {
                status = AggregateStatusPessimistic(status, _slamMap.GetTileStatus(slamCoord + Vector2Int.right));
                status = AggregateStatusPessimistic(status, _slamMap.GetTileStatus(slamCoord + Vector2Int.up));
                status = AggregateStatusPessimistic(status, _slamMap.GetTileStatus(slamCoord + Vector2Int.right + Vector2Int.up));
            }

            return status;
        }

        public Vector2Int? GetNearestTileFloodFill(Vector2Int targetCoordinate, SlamMap.SlamTileStatus lookupStatus, HashSet<Vector2Int>? excludedTiles = null)
        {
            return _aStar.GetNearestTileFloodFill(this, targetCoordinate, lookupStatus, excludedTiles);
        }

        // Combines two SlamTileStatus in a 'optimistic' fashion.
        // If any status is solid both are consider solid. Otherwise, if any status is open both are considered open
        // Unseen is returned only if all statuses are unseen 
        private static SlamMap.SlamTileStatus AggregateStatusOptimistic(SlamMap.SlamTileStatus status1, SlamMap.SlamTileStatus status2)
        {
            if (status1 == SlamMap.SlamTileStatus.Solid || status2 == SlamMap.SlamTileStatus.Solid)
            {
                return SlamMap.SlamTileStatus.Solid;
            }

            if (status1 == SlamMap.SlamTileStatus.Open || status2 == SlamMap.SlamTileStatus.Open)
            {
                return SlamMap.SlamTileStatus.Open;
            }

            return SlamMap.SlamTileStatus.Unseen;
        }

        // Combines two SlamTileStatus in a 'pessimistic' fashion.
        // If any status is solid both are consider solid. If any status is unseen both are considered unseen 
        private static SlamMap.SlamTileStatus AggregateStatusPessimistic(SlamMap.SlamTileStatus status1, SlamMap.SlamTileStatus status2)
        {
            if (status1 == SlamMap.SlamTileStatus.Solid || status2 == SlamMap.SlamTileStatus.Solid)
            {
                return SlamMap.SlamTileStatus.Solid;
            }

            if (status1 == SlamMap.SlamTileStatus.Unseen || status2 == SlamMap.SlamTileStatus.Unseen)
            {
                return SlamMap.SlamTileStatus.Unseen;
            }

            return SlamMap.SlamTileStatus.Open;
        }

        public Vector2Int GetCurrentPosition(bool dependOnBrokenBehaviour = true)
        {
            var approximatePosition = _slamMap.ApproximatePosition - _offset;
            return dependOnBrokenBehaviour ? Vector2Int.FloorToInt(approximatePosition) : Vector2Int.RoundToInt(approximatePosition);
        }

        public Vector3 TileToWorld(Vector2 tile)
        {
            return new Vector3(tile.x, tile.y, -0.01f) + (Vector3)_offset;
        }
    }
}