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
//                      Casper Nyvang Sørensen, Christian Ziegler Sejersen, Henrik Van Peet, Jakob Meyer Olsen, Mads Beyer Mogensen and Puvikaran Santhirasegaram 
// 
// Original repository: https://github.com/Molitany/MAES

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

using Maes.Map.PathFinding;
using Maes.Robot;
using Maes.Utilities;

using UnityEngine;

namespace Maes.Map
{

    // This represents a low-resolution map where the robot can comfortably fit inside a single cell
    public sealed class CoarseGrainedMap : IPathFindingMap
    {
        private readonly SlamMap _slamMap;
        private Bitmap _tilesCoveredStatus;
        private SlamMap.SlamTileStatus[,] _optimisticTileStatuses;
        private HashSet<Vector2Int>? _excludedTiles;
        private readonly Vector2 _offset;
        private readonly MyAStar _aStar = new();

        /// <summary>
        /// A lower-resolution map (half the resolution of a <see cref="SlamMap"/>).
        /// </summary>
        /// <param name="slamMap">The map to create the CoarseGrainedMap from.</param>
        /// <param name="width">Width in coarse-grained tiles.</param>
        /// <param name="height">Height in coarse-grained tiles.</param>
        /// <param name="offset">Coordinate offset.</param>
        /// <param name="mapKnown">Whether the map is initially known.</param>
        [ForbiddenKnowledge]
        public CoarseGrainedMap(SlamMap slamMap, int width, int height, Vector2 offset, bool mapKnown = false)
        {
            _slamMap = slamMap;
            Width = width;
            Height = height;
            _offset = offset;
            _tilesCoveredStatus = new Bitmap(0, 0, width, height);
            _optimisticTileStatuses = SetTileStatuses(slamMap, width, height, mapKnown);
        }

        private static SlamMap.SlamTileStatus[,] SetTileStatuses(SlamMap slamMap, int width, int height, bool mapKnown)
        {
            var tileStatuses = new SlamMap.SlamTileStatus[width, height];
            if (mapKnown)
            {
                var slamMapTiles = slamMap.GetTileStatuses();
                for (var x = 0; x < width; x++)
                {
                    for (var y = 0; y < height; y++)
                    {
                        var tiles = new[] { slamMapTiles[x * 2, y * 2], slamMapTiles[x * 2 + 1, y * 2], slamMapTiles[x * 2, y * 2 + 1], slamMapTiles[x * 2 + 1, y * 2 + 1] };
                        tileStatuses[x, y] = tiles.Contains(SlamMap.SlamTileStatus.Solid) ? SlamMap.SlamTileStatus.Solid : SlamMap.SlamTileStatus.Open;
                    }
                }
            }
            return tileStatuses;
        }

        /// <summary>
        /// Returns the approximate position on this map (local tile scale coordinates)
        /// </summary> 
        public Vector2 GetApproximatePosition()
        {
            return _slamMap.ApproximatePosition - _offset;
        }

        public Vector2Int GetCurrentPosition(bool dependOnBrokenBehavior = true)
        {
            var approximatePosition = GetApproximatePosition();
            return dependOnBrokenBehavior ? Vector2Int.FloorToInt(approximatePosition) : Vector2Int.RoundToInt(approximatePosition);
        }

        public float GetApproximateGlobalDegrees()
        {
            return _slamMap.GetRobotAngleDeg();
        }

        /// <param name="tileCoord">The coarse-grained tile to get a relative position to</param>
        /// <param name="dependOnBrokenBehaviour"></param>
        /// <returns>A <see cref="RelativePosition"/>, relative from the calling Robot to the target tileCoord.</returns>
        public RelativePosition GetTileCenterRelativePosition(Vector2Int tileCoord, bool dependOnBrokenBehaviour = true)
        {
            // Convert to local coordinate
            var robotPosition = GetApproximatePosition();
            var target = tileCoord + (dependOnBrokenBehaviour ? Vector2.one * 0.5f : Vector2.zero);
            var distance = Vector2.Distance(robotPosition, target);
            var angle = Vector2.SignedAngle(Geometry.DirectionAsVector(_slamMap.GetRobotAngleDeg()), target - robotPosition);
            return new RelativePosition(distance, angle);
        }

        /// <returns>all tiles that are either Seen or Solid as a Dictionary.</returns>
        public Dictionary<Vector2Int, SlamMap.SlamTileStatus> GetExploredTiles()
        {
            var res = new Dictionary<Vector2Int, SlamMap.SlamTileStatus>();

            for (var x = 0; x < Width; x++)
            {
                for (var y = 0; y < Height; y++)
                {
                    var pos = new Vector2Int(x, y);
                    if (GetTileStatus(pos) != SlamMap.SlamTileStatus.Unseen)
                    {
                        res[pos] = GetTileStatus(pos);
                    }
                }
            }

            return res;
        }


        /// <returns>all tiles that are unseen as a list.</returns>
        public List<Vector2Int> GetUnexploredTiles()
        {
            var res = new List<Vector2Int>();

            for (var x = 0; x < Width; x++)
            {
                for (var y = 0; y < Height; y++)
                {
                    var pos = new Vector2Int(x, y);
                    if (GetTileStatus(pos) == SlamMap.SlamTileStatus.Unseen)
                    {
                        res.Add(pos);
                    }
                }
            }

            return res;
        }

        /// <param name="localCoordinate">the tile to get the explored-status from.</param>
        /// <returns>the explored-status of the given tile.</returns>
        public bool IsTileExplored(Vector2Int localCoordinate)
        {
            AssertWithinBounds(localCoordinate);
            return _tilesCoveredStatus.Contains(localCoordinate.x, localCoordinate.y);
        }

        // Sets the data at the given tile, overwriting any existing data object if present
        /// <summary>
        /// Sets the data at a given tile, overwriting any existing data.
        /// </summary>
        /// <param name="localCoordinate">the given tile to set det data at.</param>
        /// <param name="data">the data-value to set at the given tile.</param>
        [ForbiddenKnowledge]
        public void SetTileExplored(Vector2Int localCoordinate, bool data)
        {
            AssertWithinBounds(localCoordinate);
            if (data)
            {
                _tilesCoveredStatus.Set(localCoordinate.x, localCoordinate.y);
            }
            else
            {
                _tilesCoveredStatus.Unset(localCoordinate.x, localCoordinate.y);
            }
        }

        /// <summary>
        /// Asserts that a given coordinate is within the <see cref="CoarseGrainedMap"/> bounds.
        /// </summary>
        /// <param name="coordinate">the coordinate to test.</param>
        /// <exception cref="ArgumentException">raised when coordinate is out of bounds.</exception>
        [Conditional("DEBUG")]
        private void AssertWithinBounds(Vector2Int coordinate)
        {
            if (!IsWithinBounds(coordinate))
            {
                throw new ArgumentException($"Given coordinate is out of bounds {coordinate} ({Width}, {Height})");
            }
        }

        public bool IsCoordWithinBounds(Vector2Int coordinate)
        {
            return (coordinate.x >= 0 && coordinate.x < Width && coordinate.y >= 0 && coordinate.y < Height) && !CheckIfAnyIsStatus(coordinate, SlamMap.SlamTileStatus.Solid);
        }

        // Returns the status of the given tile (Solid, Open or Unseen)
        /// <summary>
        /// Returns SLAM status of a given tile. Aggregates with neighbours up, right, and up+right (to compensate for half resolution).
        /// </summary>
        /// <param name="localCoordinate">To coarse-grained coordinate to get the status of.</param>
        /// <param name="optimistic"><br/>
        /// <li>if <b>true</b>, uses <see cref="AggregateStatusOptimistic"/> to get status on neighbours.</li><br/>
        /// <li>if <b>false</b>, uses <see cref="AggregateStatusPessimistic"/> to get status on neighbours.</li>
        /// </param>
        /// <returns>the aggregates status of the tile as a <see cref="SlamMap.SlamTileStatus"/>.</returns>
        public SlamMap.SlamTileStatus GetTileStatus(Vector2Int localCoordinate, bool optimistic = false)
        {
            var slamCoord = ToSlamMapCoordinate(localCoordinate);

            var x = slamCoord.x;
            var y = slamCoord.y;

            var status = _slamMap.GetTileStatus(x, y);
            if (optimistic)
            {
                status = AggregateStatusOptimistic(status, _slamMap.GetTileStatus(x + 1, y)); // Right
                status = AggregateStatusOptimistic(status, _slamMap.GetTileStatus(x, y + 1)); // Up
                status = AggregateStatusOptimistic(status, _slamMap.GetTileStatus(x + 1, y + 1)); // Right + Up
            }
            else
            {
                status = AggregateStatusPessimistic(status, _slamMap.GetTileStatus(x + 1, y)); // Right
                status = AggregateStatusPessimistic(status, _slamMap.GetTileStatus(x, y + 1)); // Up
                status = AggregateStatusPessimistic(status, _slamMap.GetTileStatus(x + 1, y + 1)); // Right + Up
            }

            return status;
        }

        /// <summary>
        /// Gets the nearest target status tile using flood fill, which is faster than doing aStar.
        /// </summary>
        /// <param name="targetCoordinate">Location to start the flood fill</param>
        /// <param name="lookupStatus">The status that is being looked for being {unseen, open, solid}</param>
        /// <param name="excludedTiles"></param>
        /// <returns>The given tile in coarse coordinates</returns>
        public Vector2Int? GetNearestTileFloodFill(Vector2Int targetCoordinate, SlamMap.SlamTileStatus lookupStatus, HashSet<Vector2Int>? excludedTiles = null)
        {
            return _aStar.GetNearestTileFloodFill(this, targetCoordinate, lookupStatus, excludedTiles);
        }

        /// <summary>
        /// Combines two <see cref="SlamMap.SlamTileStatus"/>' in an "optimistic" fashion.
        /// </summary>
        /// <returns>
        /// <li>If any status is 'solid', both are considered 'solid'.</li>
        /// <li>Otherwise, if any status is 'open', both are considered 'open'.</li><br/>
        /// 'Unseen' is only returned if both tiles are 'unseen'.
        /// </returns>
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

        /// <summary>
        /// Combines two <see cref="SlamMap.SlamTileStatus"/>' in a "pessimistic" fashion.
        /// </summary>
        /// <returns>
        /// <li>If any status is 'solid', both are considered 'solid'.</li>
        /// <li>if any status is 'unseen', both are considered 'unseen'.</li><br/>
        /// 'Open' is only returned if both tiles are 'open'.
        /// </returns>
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


        /// <summary>
        /// Converts the given <see cref="SlamMap"/> coordinate to a local coordinate.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2Int FromSlamMapCoordinate(Vector2Int slamCoord)
        {
            return new Vector2Int(slamCoord.x >> 1, slamCoord.y >> 1);
        }

        private static readonly Func<Vector2Int, Vector2Int> FromSlamMapCoordinateDelegate = FromSlamMapCoordinate;

        /// <summary>
        /// Converts a list of <see cref="SlamMap"/> coordinates to a list of local coordinates.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HashSet<Vector2Int> FromSlamMapCoordinates(IEnumerable<Vector2Int> slamCoords)
        {
            return new HashSet<Vector2Int>(slamCoords.Select(FromSlamMapCoordinateDelegate));
        }

        /// <summary>
        /// Converts the given local coordinate to a <see cref="SlamMap"/> coordinate.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2Int ToSlamMapCoordinate(Vector2 localCoordinate)
        {
            return Vector2Int.FloorToInt(localCoordinate * 2);
        }

        /// <summary>
        /// Returns the position of the neighbour in the given direction, relative to the current direction and position of the robot.
        /// </summary>
        public Vector2Int GetRelativeNeighbour(CardinalDirection.RelativeDirection relativeDirection)
        {
            var targetDirection = GetRelativeNeighbourDirection(relativeDirection);
            var currentPosition = GetApproximatePosition();
            var relativePosition = currentPosition + targetDirection.Vector;
            return new Vector2Int((int)relativePosition.x, (int)relativePosition.y);
        }

        public CardinalDirection GetRelativeNeighbourDirection(CardinalDirection.RelativeDirection relativeDirection)
        {
            var currentCardinalDirection = CardinalDirection.DirectionFromDegrees(_slamMap.GetRobotAngleDeg());
            return currentCardinalDirection.DirectionFromRelativeDirection(relativeDirection);
        }

        /// <summary>
        /// Returns the position of the neighbour in the given global direction, relative to the robot's position.
        /// </summary>
        /// <param name="direction"></param>
        /// <returns></returns>
        public Vector2Int GetGlobalNeighbour(CardinalDirection direction)
        {
            var currentPosition = GetApproximatePosition();
            var relativePosition = currentPosition + direction.Vector;
            return new Vector2Int((int)relativePosition.x, (int)relativePosition.y);
        }

        /// <summary>
        /// Calculates, and returns, the path from the robot's current position to the target.
        /// </summary>
        /// <param name="target">the target that the path should end at.</param>
        /// <param name="beOptimistic">if <b>true</b>, returns path getting the closest to the target, if no full path can be found.</param>
        /// <param name="beOptimistic">if <b>true</b>, treats unseen tiles as open in the path finding algorithm. Treats unseen tiles as solid otherwise.</param>
        /// <param name="acceptPartialPaths"></param>
        /// <param name="dependOnBrokenBehavior"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2Int[]? GetPath(Vector2Int target, bool beOptimistic = false, bool acceptPartialPaths = false, bool dependOnBrokenBehavior = true)
        {
            var approxPosition = GetApproximatePosition();
            var position = dependOnBrokenBehavior ? Vector2Int.FloorToInt(approxPosition) : Vector2Int.RoundToInt(approxPosition);
            return _aStar.GetPath(position, target, this, beOptimistic: beOptimistic, acceptPartialPaths: acceptPartialPaths);
        }

        /// <summary>
        /// Calculates, and returns, the path from the robot's current position to the target.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="target">the target that the path should end at.</param>
        /// <param name="beOptimistic">if <b>true</b>, returns path getting the closest to the target, if no full path can be found.</param>
        /// <param name="beOptimistic">if <b>true</b>, treats unseen tiles as open in the path finding algorithm. Treats unseen tiles as solid otherwise.</param>
        /// <param name="acceptPartialPaths"></param>
        /// <param name="dependOnBrokenBehavior"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2Int[]? GetPath(Vector2Int start, Vector2Int target, bool beOptimistic = false, bool acceptPartialPaths = false)
        {
            return _aStar.GetPath(start, target, this, beOptimistic: beOptimistic, acceptPartialPaths: acceptPartialPaths);
        }

        /// <summary>
        /// Calculates, and returns, the path from the robot's current position to the target. Will avoid planning a path though the excluded tiles.
        /// </summary>
        /// <param name="target">the target that the path should end at.</param>
        /// <param name="excludedTiles">the tiles that should be avoided during the path.</param>
        /// <param name="maxPathCost">the maximum cost of the path.</param>
        public Vector2Int[]? GetPath(Vector2Int target, HashSet<Vector2Int>? excludedTiles = null, float maxPathCost = float.MaxValue)
        {
            if (excludedTiles != null && excludedTiles.Contains(target))
            {
                return null;
            }

            var approxPosition = GetApproximatePosition();
            if (excludedTiles != null)
            {
                _excludedTiles = excludedTiles;
            }

            var path = _aStar.GetOptimisticPath(new Vector2Int((int)approxPosition.x, (int)approxPosition.y), target, this);
            _excludedTiles = null;
            return path;
        }

        /// <summary>
        /// Calculates, and returns, the path from the robot's current position to the target. The path will be "reduced" to a set of steps, rather than containing every single tile
        /// that will be traversed during travel.
        /// </summary>
        /// <param name="target">the target that the path should end at.</param>
        /// <param name="excludedTiles">the tiles that should be avoided during traversal.</param>
        /// <returns></returns>
        public PathStep[]? GetPathSteps(Vector2Int target, HashSet<Vector2Int>? excludedTiles = null)
        {
            if (excludedTiles != null && excludedTiles.Contains(target))
            {
                return null;
            }

            var approxPosition = GetApproximatePosition();
            if (excludedTiles != null)
            {
                _excludedTiles = excludedTiles;
            }

            var path = _aStar.GetOptimisticPath(new Vector2Int((int)approxPosition.x, (int)approxPosition.y), target, this);
            _excludedTiles = null;
            return path == null ? null : _aStar.PathToSteps(path);
        }

        /// <summary>
        /// Calculates, and returns, a path from the robots current position to the target. Will reduce the path to a list of <see cref="PathStep"/>s.
        /// </summary>
        public PathStep[]? GetTnfPathAsPathSteps(Vector2Int target)
        {
            var path = GetPath(target, beOptimistic: false);
            return path == null
                ? null
                : _aStar.PathToSteps(path);
        }

        public bool BrokenCollisionMap => _slamMap.BrokenCollisionMap;
        public int LastUpdateTick => _slamMap.LastUpdateTick;
        public int Width { get; }

        public int Height { get; }

        /// <returns>whether or not a tile at a given position is solid.</returns>
        public bool IsSolid(Vector2Int coordinate)
        {
            if (_excludedTiles?.Contains(coordinate) ?? false)
            {
                return true;
            }

            var tileStatus = GetTileStatus(coordinate, optimistic: false);
            return tileStatus != SlamMap.SlamTileStatus.Open;
        }

        /// <returns>whether or not a tile at a given position is solid. Is more optimistic than <see cref="IsSolid"/>.</returns>
        public bool IsOptimisticSolid(Vector2Int coordinate)
        {
            if (_excludedTiles?.Contains(coordinate) ?? false)
            {
                return true;
            }

            return _optimisticTileStatuses[coordinate.x, coordinate.y] != SlamMap.SlamTileStatus.Open;
        }

        public float CellSize => 1.0f;

        /// <summary>
        /// Converts the <see cref="Vector2"/> given by <see cref="GetApproximatePosition"/> to the tile in
        /// which the robot is currently positioned.
        /// </summary>
        public Vector2Int GetCurrentTile()
        {
            var robotPosition = GetApproximatePosition();
            return new Vector2Int((int)robotPosition.x, (int)robotPosition.y);
        }

        /// <summary>
        /// Synchronizes a list of maps, to make them all contain the same information. Used to simulate Distributed SLAM.
        /// </summary>
        public static void Synchronize(List<CoarseGrainedMap> maps, SlamMap.SlamTileStatus[,] newSlamStatuses)
        {
            // Synchronize exploration bool statuses
            using var globalExplorationStatuses = new Bitmap(0, 0, maps[0].Width, maps[0].Height);
            foreach (var map in maps)
            {
                globalExplorationStatuses.Union(map._tilesCoveredStatus);
            }

            foreach (var map in maps)
            {
                map._tilesCoveredStatus.Dispose();
                map._tilesCoveredStatus = globalExplorationStatuses.Clone();
            }

            // Synchronize tile statuses
            var globalMap = new SlamMap.SlamTileStatus[maps[0].Width, maps[0].Height];
            for (var x = 0; x < maps[0].Width; x++)
            {
                for (var y = 0; y < maps[0].Height; y++)
                {
                    var slamX = x * 2;
                    var slamY = y * 2;
                    var status = newSlamStatuses[slamX, slamY];
                    status = AggregateStatusOptimistic(status, newSlamStatuses[slamX + 1, slamY]);
                    status = AggregateStatusOptimistic(status, newSlamStatuses[slamX + 1, slamY + 1]);
                    status = AggregateStatusOptimistic(status, newSlamStatuses[slamX, slamY + 1]);
                    globalMap[x, y] = status;
                }
            }
            foreach (var map in maps)
            {
                map._optimisticTileStatuses = (SlamMap.SlamTileStatus[,])globalMap.Clone();
            }
        }

        /// <summary>
        /// Adds the information in the list of other maps to the information found in one map.
        /// </summary>
        /// <param name="map"></param>
        /// <param name="others"></param>
        /// <param name="newSlamStatuses"></param>
        public static void Combine(CoarseGrainedMap map, List<CoarseGrainedMap> others, SlamMap.SlamTileStatus[,] newSlamStatuses)
        {
            var globalExplorationStatuses = new Bitmap(0, 0, map.Width, map.Height);
            foreach (var other in others)
            {
                globalExplorationStatuses.Union(other._tilesCoveredStatus);
            }

            map._tilesCoveredStatus.Dispose();
            map._tilesCoveredStatus = globalExplorationStatuses;

            // Synchronize tile statuses
            var globalMap = new SlamMap.SlamTileStatus[others[0].Width, others[0].Height];
            for (var x = 0; x < others[0].Width; x++)
            {
                for (var y = 0; y < others[0].Height; y++)
                {
                    var slamX = x * 2;
                    var slamY = y * 2;
                    var status = newSlamStatuses[slamX, slamY];
                    status = AggregateStatusOptimistic(status, newSlamStatuses[slamX + 1, slamY]);
                    status = AggregateStatusOptimistic(status, newSlamStatuses[slamX + 1, slamY + 1]);
                    status = AggregateStatusOptimistic(status, newSlamStatuses[slamX, slamY + 1]);
                    globalMap[x, y] = status;
                }
            }

            map._optimisticTileStatuses = (SlamMap.SlamTileStatus[,])globalMap.Clone();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsWithinBounds(Vector2Int coordinate)
        {
            return coordinate.x >= 0 && coordinate.x < Width && coordinate.y >= 0 && coordinate.y < Height;
        }

        /// <returns><b>false</b>, only if the tile at the coordinate is known to be solid.</returns>
        public bool IsPotentiallyExplorable(Vector2Int coordinate)
        {
            // To avoid giving away information which the robot cannot know, tiles outside the map bounds are
            // considered explorable
            if (!IsWithinBounds(coordinate))
            {
                return true;
            }

            return (!_tilesCoveredStatus.Contains(coordinate.x, coordinate.y)) && GetSlamTileStatuses(coordinate).All(status => status != SlamMap.SlamTileStatus.Solid);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private SlamMap.SlamTileStatus[] GetSlamTileStatuses(Vector2Int coordinate)
        {
            var slamCoord = coordinate * 2;
            return new[] {
                _slamMap.GetTileStatus(slamCoord),
                _slamMap.GetTileStatus(slamCoord + Vector2Int.right),
                _slamMap.GetTileStatus(slamCoord + Vector2Int.up),
                _slamMap.GetTileStatus(slamCoord + Vector2Int.up + Vector2Int.right),
            };
        }

        public bool IsUnseenSemiOpen(Vector2Int nextCoordinate, Vector2Int currentCoordinate)
        {
            // This function is a hacky fix to a pathfinding deadlocking issue.
            // get SLAM coordinates for the coarse grained tiles that needs to be checked
            const SlamMap.SlamTileStatus solid = SlamMap.SlamTileStatus.Solid;
            const SlamMap.SlamTileStatus open = SlamMap.SlamTileStatus.Open;

            // If all SLAM tiles are solid, just return solid
            if (CheckIfAnyIsStatus(nextCoordinate, solid) || CheckIfAnyIsStatus(currentCoordinate, solid) || CheckIfAllSlamStatusesSolid(nextCoordinate) || CheckIfAllSlamStatusesSolid(currentCoordinate))
            {
                return true;
            }
            if (CheckIfAnyIsStatus(currentCoordinate, open) && CheckIfAnyIsStatus(nextCoordinate, open))
            {
                return false;
            }
            return true;
        }

        private bool CheckIfAllSlamStatusesSolid(Vector2Int coordinate)
        {
            var slamCoord = coordinate * 2;
            var solids =
                (_slamMap.GetTileStatus(slamCoord) != SlamMap.SlamTileStatus.Open ? 1 : 0) +
                    (_slamMap.GetTileStatus(slamCoord + Vector2Int.right) != SlamMap.SlamTileStatus.Open ? 1 : 0) +
                    (_slamMap.GetTileStatus(slamCoord + Vector2Int.up) != SlamMap.SlamTileStatus.Open ? 1 : 0) +
                    (_slamMap.GetTileStatus(slamCoord + Vector2Int.up + Vector2Int.right) != SlamMap.SlamTileStatus.Open ? 1 : 0);

            return solids == 4;
        }

        private bool CheckIfAnyIsStatus(Vector2Int coordinate, SlamMap.SlamTileStatus status)
        {
            var slamCoord = coordinate * 2;
            return
                _slamMap.GetTileStatus(slamCoord) == status ||
                _slamMap.GetTileStatus(slamCoord + Vector2Int.right) == status ||
                _slamMap.GetTileStatus(slamCoord + Vector2Int.up) == status ||
                _slamMap.GetTileStatus(slamCoord + Vector2Int.up + Vector2Int.right) == status;

        }

        /// <summary>
        /// Updates the information in a tile with the new observed status. Does not change anything, if any of the SLAM tiles in the coarse-grained tile are 'solid'.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateTile(Vector2Int courseCoord, SlamMap.SlamTileStatus observedStatus)
        {
            var x = courseCoord.x;
            var y = courseCoord.y;

            UpdateTile(x, y, observedStatus);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateTile(int coarseX, int coarseY, SlamMap.SlamTileStatus observedStatus)
        {
            ref var tileStatus = ref _optimisticTileStatuses[coarseX, coarseY];
            // If some sub-tile of the coarse tile is known to be solid, then new status does not matter
            // Otherwise assign the new status (either open or solid)
            if (tileStatus != SlamMap.SlamTileStatus.Solid)
            {
                tileStatus = observedStatus;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 TileToWorld(Vector2 tile)
        {
            return new Vector3(tile.x, tile.y, -0.01f) + (Vector3)_offset;
        }
    }
}