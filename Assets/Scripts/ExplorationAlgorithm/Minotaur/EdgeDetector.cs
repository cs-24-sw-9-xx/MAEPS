﻿// Copyright 2024 MAES
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
// Contributors: Rasmus Borrisholt Schmidt, Andreas Sebastian Sørensen, Thor Beregaard
// 
// Original repository: https://github.com/Molitany/MAES

using System.Collections.Generic;
using System.Linq;

using Maes.Map;
using Maes.Map.PathFinding;
using Maes.Utilities;

using UnityEngine;

using static Maes.Map.SlamMap;

namespace Maes.ExplorationAlgorithm.Minotaur
{
    internal class EdgeDetector
    {
        public EdgeState State => UpdateState();
        public bool isStuck => GetTilesAroundPoint(_edgeSize, _defaultLimitors).Where(tile => _coarseMap.GetTileStatus(tile) != SlamTileStatus.Open).Any();

        private readonly SlamMap _slamMap;
        private readonly CoarseGrainedMap _coarseMap;
        private readonly int _edgeSize;
        private readonly int _visionRange;
        private readonly List<SlamTileStatus> _defaultLimitors = new List<SlamTileStatus> { SlamTileStatus.Solid };
        private Vector2Int _robotPosition => _coarseMap.GetCurrentPosition();

        public enum EdgeState
        {
            Forward,
            ForwardLeft,
            ForwardRight,
        }

        public EdgeDetector(SlamMap map, float visionRange)
        {
            _slamMap = map;
            _coarseMap = map.GetCoarseMap();
            _edgeSize = (int)visionRange + 1;
            _visionRange = (int)visionRange;
        }

        private EdgeState UpdateState()
        {
            return EdgeState.ForwardRight;
        }

        public Vector2Int? GetNearestUnseenTile()
        {
            var angles = Enumerable.Range(0, 360).ToList();
            var range = 1;
            while (angles.Any())
            {
                var unseenTiles = new List<Vector2Int>();
                var removedAngles = new List<int>();
                foreach (var angle in angles)
                {
                    var tile = GetFurthestTileAroundPoint(_coarseMap.GetApproximateGlobalDegrees() + angle, range, _defaultLimitors);

                    if (_coarseMap.GetTileStatus(tile) == SlamTileStatus.Solid)
                    {
                        removedAngles.Add(angle);
                        continue;
                    }
                    if (_coarseMap.GetTileStatus(tile) == SlamTileStatus.Unseen)
                    {
                        unseenTiles.Add(tile);
                    }
                }
                foreach (var removedangle in removedAngles)
                {
                    angles.Remove(removedangle);
                }
                if (unseenTiles.Any())
                {
                    return unseenTiles.OrderBy(tile => Vector2.Distance(_robotPosition, tile)).First();
                }
                range++;
            }
            return null;
        }

        /// <summary>
        /// Gets the tiles around the robot by casting 360-<paramref name="startAngle"/> rays. These rays expand from the robot and out being stopped by the <paramref name="limiters"/>.
        /// <para></para>
        /// If only one ray is desired, consider <seealso cref="GetFurthestTileAroundPoint(float, int, List{SlamTileStatus}, Vector2Int?, bool, bool)"/>
        /// </summary>
        /// <param name="range">The distance of the ray</param>
        /// <param name="limiters">What tiles should stop the rays</param>
        /// <param name="point"></param>
        /// <param name="slamPrecision">Target slam tiles instead of coarse tiles</param>
        /// <param name="startAngle">If set above 0 then this will create arcs instead of circles around the robot, based on <see cref="Vector2.right"/> counter-clockwise</param>
        /// <returns>The unique tiles that were hit</returns>
        public HashSet<Vector2Int> GetTilesAroundPoint(int range, List<SlamTileStatus> limiters, Vector2Int? point = null, bool slamPrecision = false, int startAngle = 0)
        {
            IPathFindingMap map = slamPrecision ? _slamMap : _coarseMap;
            var tiles = new HashSet<Vector2Int>();
            for (var angle = startAngle; angle < 360; angle++)
            {
                var tile = GetFurthestTileAroundPoint(_coarseMap.GetApproximateGlobalDegrees() + angle, range, limiters, slamPrecision: slamPrecision);
                if (map.IsWithinBounds(tile))
                {
                    tiles.Add(tile);
                }
            }

            return tiles;
        }

        public Vector2Int GetFurthestTileAroundPoint(float angle, int range, List<SlamTileStatus> limiters, Vector2Int? point = null, bool snapToGrid = false, bool slamPrecision = false)
        {
            Vector2Int position;
            if (point.HasValue)
            {
                position = point.Value;
            }
            else
            {
                position = slamPrecision ? _slamMap.GetCurrentPosition() : _robotPosition;
            }
            IPathFindingMap map = slamPrecision ? _slamMap : _coarseMap;
            var tile = position;
            for (var r = 0; r < (slamPrecision ? range * 2 : range); r++)
            {
                tile = snapToGrid ? CardinalDirection.AngleToDirection(angle).Vector * r : Vector2Int.FloorToInt(Geometry.VectorFromDegreesAndMagnitude(angle, r));
                var candidateTile = tile + position;
                if (map.IsWithinBounds(candidateTile))
                {
                    foreach (var limiter in limiters)
                    {
                        if (map.GetTileStatus(candidateTile) == limiter && (limiter != SlamTileStatus.Open || r > _visionRange))
                        {
                            return candidateTile;
                        }
                    }
                    tile = candidateTile;
                }
            }
            return tile;
        }

        public IEnumerable<Vector2Int> GetBoxAroundRobot()
        {
            var boxTileList = new List<Vector2Int>();
            for (var x = -_edgeSize; x <= _edgeSize; x++)
            {
                for (var y = _edgeSize; y <= _edgeSize + 1; y++)
                {
                    boxTileList.Add(_robotPosition + new Vector2Int(x, y));
                    boxTileList.Add(_robotPosition + new Vector2Int(x, -y));
                    boxTileList.Add(_robotPosition + new Vector2Int(y, x));
                    boxTileList.Add(_robotPosition + new Vector2Int(-y, x));
                }
            }
            return boxTileList.Where(tile => _coarseMap.IsWithinBounds(tile));
        }


    }
}