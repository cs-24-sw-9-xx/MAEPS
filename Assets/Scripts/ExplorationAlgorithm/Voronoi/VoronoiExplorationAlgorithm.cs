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
using System.Text;

using Maes.Algorithms;
using Maes.Map;
using Maes.Robot;
using Maes.Robot.Task;
using Maes.Utilities;

using UnityEngine;
using UnityEngine.Assertions;

using Debug = UnityEngine.Debug;
using Random = System.Random;

namespace Maes.ExplorationAlgorithm.Voronoi
{
    public class VoronoiExplorationAlgorithm : IExplorationAlgorithm
    {
        // Set by SetController
        private IRobotController _robotController = null!;
        private readonly Random _random;

        private List<VoronoiRegion> _localVoronoiRegions = new();
        private VoronoiRegion _currentRegion;
        private readonly int _voronoiRegionMaxDistance; // Measured in coarse tiles

        // TODO: Find out why this is static. Shared between robots?
        private static readonly Dictionary<Vector2Int, bool> IsExploredMap = new();
        private int _currentTick;

        private VoronoiSearchPhase _currentSearchPhase = VoronoiSearchPhase.ExploreMode;
        private readonly float _markExploredRangeInCoarseTiles;
        private const int ExpandVoronoiRecalcInterval = 40;
        private const int SearchModeRecalcInterval = 40;
        private const int ExploreModeRecalcInterval = 50;
        private readonly List<(Vector2Int, int)> _coarseOcclusionPointsVisitedThisSearchMode = new();
        private List<(Vector2Int, bool)>? _currentTargetPath; // bool represents, if it has been visited or not.
        private Vector2Int? _currentPartialMovementTarget;
        private const float DistanceBetweenSameOccPoint = 1f; // If two occlusion points are closer than this, they are the same
        private const float VoronoiBoundaryEqualDistanceDelta = 2f;
        private VoronoiHeuristic _heuristic;
        private UnexploredTilesComparer _unexploredTilesComparer => GetSortingFunction(_heuristic);

        private delegate int UnexploredTilesComparer(Vector2Int c1, Vector2Int c2);

        // Debugging variables
        private Vector2Int? _closestOcclusionPoint;
        private int _occlusionPointsWithinView;
        private int _regionSizeCoarseTiles;
        private int _unexploredTilesInRegion;


        private enum VoronoiHeuristic
        {
            NorthEast,
            EastSouth,
            SouthWest,
            WestNorth
        }

        private enum VoronoiSearchPhase
        {
            SearchMode, // If not unexplored tiles within view 
            ExploreMode, // Go to next unexplored tile
            ExpandVoronoi
        }

        private readonly struct VoronoiRegion
        {
            public readonly int RobotId;
            public readonly List<Vector2Int> Tiles;

            public bool IsEmpty()
            {
                return Tiles.Count == 0;
            }

            public int Size()
            {
                return Tiles.Count;
            }

            public VoronoiRegion(int robotId, List<Vector2Int> tiles)
            {
                RobotId = robotId;
                Tiles = tiles;
            }
        }

        public VoronoiExplorationAlgorithm(int randomSeed, float markExploredRangeInCoarseTiles)
        {
            _random = new Random(randomSeed);
            _markExploredRangeInCoarseTiles = markExploredRangeInCoarseTiles;
            _voronoiRegionMaxDistance = 50;
        }

        public void SetController(Robot2DController controller)
        {
            _robotController = controller;
            _heuristic = (VoronoiHeuristic)(_robotController.GetRobotID() % 4);
        }

        public void UpdateLogic()
        {
            if (!_currentRegion.IsEmpty())
            {
                _currentRegion.Tiles.ForEach(tile => tile.DrawDebugLineFromRobot(_robotController.GetSlamMap().GetCoarseMap(), Color.black, 0.2f));
            }

            UpdateExploredStatusOfTiles();

            // Divide into voronoi regions with local robots
            if (ShouldRecalculate())
            {
                // Debug.Log("Recalculating voronoi");
                RecalculateVoronoiRegions();

                var unexploredTiles = FindUnexploredTilesWithinRegion(_currentRegion);
                _unexploredTilesInRegion = unexploredTiles.Count;
                _regionSizeCoarseTiles = _currentRegion.Size();

                if (unexploredTiles.Count != 0)
                {
                    _currentSearchPhase = VoronoiSearchPhase.ExploreMode;
                    _coarseOcclusionPointsVisitedThisSearchMode.Clear();
                    // _currentTargetPath = null;
                    // _currentPartialMovementTarget = null;
                    // Sorted by north, west, east, south 
                    unexploredTiles.Sort((c1, c2) => _unexploredTilesComparer(c1, c2));

                    // Movement target is the best tile, where a path can be found
                    var target = unexploredTiles[0];
                    SetCurrentMovementTarget(target);
                }
                else
                {
                    EnterSearchMode();
                }
            }

            // Make the robot follow the current path.
            if ((!IsDoneWithCurrentPath() && _robotController.GetStatus() == RobotStatus.Idle))
            {
                var nextStep = GetNextStep();
                if (nextStep != null)
                {
                    // We move to partly unseen targets. If they turn out to be solid, find a new target
                    if (_robotController.GetSlamMap().GetCoarseMap().GetTileStatus(nextStep.Value) !=
                        SlamMap.SlamTileStatus.Solid)
                    {
                        _currentPartialMovementTarget = nextStep.Value;
                        var relativePosition = _robotController.GetSlamMap().GetCoarseMap()
                            .GetTileCenterRelativePosition(_currentPartialMovementTarget.Value);

                        // Find delta
                        var delta = 1.5f;
                        if (Math.Abs(relativePosition.RelativeAngle) <= delta)
                        {
                            _robotController.Move((relativePosition.Distance));
                        }
                        else
                        {
                            _robotController.Rotate(relativePosition.RelativeAngle);
                        }
                    }
                    else
                    {
                        _currentTargetPath = null;
                        _currentPartialMovementTarget = null;
                    }

                }
                else
                {
                    Debug.Log("Next step == null");
                }

            }

            _currentTick++;
        }

        private UnexploredTilesComparer GetSortingFunction(VoronoiHeuristic heuristic)
        {
            return heuristic switch
            {
                VoronoiHeuristic.SouthWest => (c1, c2) =>
                {
                    if (c2.y.CompareTo(c1.y) == 0)
                    {
                        return -c2.x.CompareTo(c1.x);
                    }

                    return -c2.y.CompareTo(c1.y);
                }
                ,
                VoronoiHeuristic.WestNorth => (c1, c2) =>
                {
                    if (c2.x.CompareTo(c1.x) == 0)
                    {
                        return c2.y.CompareTo(c1.y);
                    }

                    return -c2.x.CompareTo(c1.x);
                }
                ,
                VoronoiHeuristic.NorthEast => (c1, c2) =>
                {
                    if (c2.y.CompareTo(c1.y) == 0)
                    {
                        return c2.x.CompareTo(c1.x);
                    }

                    return c2.y.CompareTo(c1.y);
                }
                ,
                VoronoiHeuristic.EastSouth => (c1, c2) =>
                {
                    if (c2.x.CompareTo(c1.x) == 0)
                    {
                        return -c2.y.CompareTo(c1.y);
                    }

                    return c2.x.CompareTo(c1.x);
                }
                ,
                _ => throw new Exception("Could not find sorting function for voronoi heuristic.")
            };
        }

        private Vector2Int? GetNextStep()
        {
            if (IsDoneWithCurrentPath())
            {
                return null;
            }

            // IsDoneWithCurrentPath checks for _currentPargetPath == null
            return _currentTargetPath!.First(e => e.Item2 == false).Item1;
        }

        private void UpdateExploredStatusOfTiles()
        {
            var currentPosition = _robotController.GetSlamMap().GetCoarseMap().GetApproximatePosition();
            var currentlyVisibleTiles = _robotController.GetSlamMap().GetCurrentlyVisibleTiles();
            var currentlyVisibleCoarseTiles = _robotController.GetSlamMap().GetCoarseMap()
                .FromSlamMapCoordinates(currentlyVisibleTiles.Keys.ToList());

            foreach (var visibleTile in currentlyVisibleCoarseTiles)
            {
                var distance = Geometry.DistanceBetween(currentPosition, visibleTile);
                if (distance <= _markExploredRangeInCoarseTiles)
                {
                    if (!IsExploredMap.ContainsKey(visibleTile))
                    {
                        IsExploredMap[visibleTile] = true;
                    }
                }
            }

            // Check if movement target reached
            if (!IsDoneWithCurrentPath())
            {
                if (_currentTargetPath != null && _currentPartialMovementTarget != null)
                {
                    // Have we reached the next partial goal?
                    var robotCoarseTile = _robotController.GetSlamMap().GetCoarseMap().GetCurrentTile();
                    if (robotCoarseTile.Equals(_currentPartialMovementTarget.Value))
                    {
                        // Mark the current partial movement target as visited
                        _currentTargetPath = _currentTargetPath.Select(e =>
                        {
                            if (e.Item1.Equals(robotCoarseTile))
                            {
                                return (e.Item1, true);
                            }

                            return e;
                        }).ToList();
                        if (_currentSearchPhase == VoronoiSearchPhase.SearchMode)
                        {
                            _coarseOcclusionPointsVisitedThisSearchMode.Add((_currentPartialMovementTarget.Value, _currentTick));
                        }
                    }
                }
            }
        }

        private bool ShouldRecalculate()
        {
            if (_robotController.IsCurrentlyColliding())
            {
                _robotController.StopCurrentTask();
                return true;
            }

            if (IsDoneWithCurrentPath())
            {
                _currentTargetPath = null;
                return true;
            }

            if (_robotController.SenseNearbyRobots().Count > 0)
            {
                switch (_currentSearchPhase)
                {
                    case VoronoiSearchPhase.SearchMode:
                        return _currentTick % SearchModeRecalcInterval == 0;
                    case VoronoiSearchPhase.ExploreMode:
                        return _currentTick % ExploreModeRecalcInterval == 0;
                    case VoronoiSearchPhase.ExpandVoronoi:
                        return _currentTick % ExpandVoronoiRecalcInterval == 0;
                }
            }

            return false;
        }

        private void EnterSearchMode()
        {
            var coarseMap = _robotController.GetSlamMap().GetCoarseMap();
            var coarseOcclusionPoints = FindClosestOcclusionPoints();
            _occlusionPointsWithinView = coarseOcclusionPoints.Count;

            // Occlusion points close to something visited recently will not be visited
            var coarseOcclusionPointsNotVisitedThisSearchMode = new List<Vector2Int>();
            foreach (var op in coarseOcclusionPoints)
            {
                foreach (var visitedOccPo in _coarseOcclusionPointsVisitedThisSearchMode)
                {
                    if (Geometry.DistanceBetween(op, visitedOccPo.Item1) > DistanceBetweenSameOccPoint)
                    {
                        coarseOcclusionPointsNotVisitedThisSearchMode.Add(op);
                    }
                }
            }

            // If we have some unvisited occlusion points, visit them. 
            if (coarseOcclusionPointsNotVisitedThisSearchMode.Count > 0)
            {
                _currentSearchPhase = VoronoiSearchPhase.SearchMode;
                // Find occlusion point closest to robot
                var robotPosition = coarseMap.GetApproximatePosition();
                coarseOcclusionPointsNotVisitedThisSearchMode.Sort((c1, c2) =>
                {
                    var c1Distance = Geometry.DistanceBetween(robotPosition, c1);
                    var c2Distance = Geometry.DistanceBetween(robotPosition, c2);
                    return c1Distance.CompareTo(c2Distance);
                });
                _closestOcclusionPoint = coarseOcclusionPointsNotVisitedThisSearchMode[0];
                SetCurrentMovementTarget(coarseOcclusionPointsNotVisitedThisSearchMode[0]);
            }
            else if (coarseOcclusionPointsNotVisitedThisSearchMode.Count == 0)
            {
                // If we have visited all occlusion points, but we still see several
                // Visit least recently visited occlusion point
                if (_coarseOcclusionPointsVisitedThisSearchMode.Count > 1)
                {
                    _currentSearchPhase = VoronoiSearchPhase.SearchMode;
                    _coarseOcclusionPointsVisitedThisSearchMode.Sort((e1, e2) => e1.Item2.CompareTo(e2.Item2));
                    var leastRecentlyVisitedOcclusionPoint = _coarseOcclusionPointsVisitedThisSearchMode[0];
                    SetCurrentMovementTarget(leastRecentlyVisitedOcclusionPoint.Item1);
                    _closestOcclusionPoint = leastRecentlyVisitedOcclusionPoint.Item1;
                }
                // If we see no occlusion points or the only occlusion point is already visited, expand region
                else if (_coarseOcclusionPointsVisitedThisSearchMode.Count == 0 || (_coarseOcclusionPointsVisitedThisSearchMode.Count == 1 && coarseOcclusionPointsNotVisitedThisSearchMode.Count == 0))
                {
                    _currentSearchPhase = VoronoiSearchPhase.ExpandVoronoi;
                    var closestVoronoiBoundary = FindClosestVoronoiBoundary();
                    SetCurrentMovementTarget(closestVoronoiBoundary);
                    _closestOcclusionPoint = null;
                }
            }
        }

        private bool IsDoneWithCurrentPath()
        {
            return _currentTargetPath == null || _currentTargetPath.TrueForAll(e => e.Item2);
        }

        private void SetCurrentMovementTarget(Vector2Int target)
        {
            // Find a the course coordinate and generate path
            var robotCoarseTile = _robotController.GetSlamMap().GetCoarseMap().GetCurrentTile();
            var path = _robotController.GetSlamMap().GetVisibleTilesCoarseMap().GetPathSteps(robotCoarseTile, target, true);

            if (path == null)
            {
                Debug.Log($"Could not find path between {robotCoarseTile} and {target}");
            }
            else
            {
                _currentTargetPath = path.Select(e => (e.End, false)).ToList(); // All points on path are not visited, i.e. false
                if (_currentTargetPath[0].Item1.Equals(robotCoarseTile) &&
                    !_robotController.HasCollidedSinceLastLogicTick())
                {
                    _currentTargetPath[0] = (_currentTargetPath[0].Item1, true);
                }
            }
        }

        private Vector2Int FindClosestVoronoiBoundary()
        {
            var coarseMap = _robotController.GetSlamMap().GetCoarseMap();
            // Should move to closest voronoi boundary, that is not a wall
            var coarseEdgeTiles = FindEdgeTiles(_currentRegion.Tiles);
            coarseEdgeTiles = coarseEdgeTiles.Distinct().ToList(); // Remove possible duplicate

            // Filter away edge tiles near obstacles
            var openCoarseEdgeTiles = coarseEdgeTiles.Where(e => !coarseMap.IsOptimisticSolid(e)).ToList();
            // If no voronoi boundary is available without an obstacle, just take one with an obstacle
            if (openCoarseEdgeTiles.Count > 0)
            {
                coarseEdgeTiles = openCoarseEdgeTiles;
            }

            var robotPosition = coarseMap.GetApproximatePosition();


            //var chance = _random.NextDouble();
            //if (chance < SELECT_FURTHEST_VORONOI_BOUNDARY_CHANCE)
            //{
            //    // Sort by furthest from robot
            //    coarseEdgeTiles.Sort((c1, c2) =>
            //    {
            //        var c1Distance = Geometry.DistanceBetween(robotPosition, c1);
            //        var c2Distance = Geometry.DistanceBetween(robotPosition, c2);
            //        return c2Distance.CompareTo(c1Distance);
            //    });
            //}
            //else
            //{
            // Sort by closest to robot
            coarseEdgeTiles.Sort((c1, c2) =>
            {
                var c1Distance = Geometry.DistanceBetween(robotPosition, c1);
                var c2Distance = Geometry.DistanceBetween(robotPosition, c2);
                return c1Distance.CompareTo(c2Distance);
            });
            //}

            // Find all tiles with a distance within delta and assume they are equally far away
            var closestOrFurthestDistance = Geometry.DistanceBetween(robotPosition, coarseEdgeTiles[0]);
            var candidates = coarseEdgeTiles.Where(e =>
                Mathf.Abs(Geometry.DistanceBetween(e, robotPosition) - closestOrFurthestDistance) < VoronoiBoundaryEqualDistanceDelta).ToList();

            // Filter away anything close to the occlusion points visited this search mode
            var filteredCandidates = candidates.Where(e =>
            {
                foreach (var occPo in _coarseOcclusionPointsVisitedThisSearchMode)
                {
                    if (Geometry.DistanceBetween(e, occPo.Item1) > DistanceBetweenSameOccPoint)
                    {
                        return false;
                    }
                }

                return true;
            }).ToList();

            if (filteredCandidates.Count > 0)
            {
                candidates = filteredCandidates;
            }

            // Take random candidate
            var candidateIndex = _random.Next(candidates.Count);

            return candidates[candidateIndex];
        }

        private List<Vector2Int> FindClosestOcclusionPoints()
        {
            var coarseMap = _robotController.GetSlamMap().GetCoarseMap();
            var visibleTilesMaps = _robotController.GetSlamMap().GetCurrentlyVisibleTiles();
            var visibleCoarseTiles = coarseMap.FromSlamMapCoordinates(visibleTilesMaps.Keys).ToList();

            var robotPosition = coarseMap.GetApproximatePosition();

            // The surrounding edge of the visibility
            var edgeTiles = FindEdgeTiles(visibleCoarseTiles);

            // Filter out edge tiles, that are walls. They can't show anything new
            var nonSolidEdgeTiles = edgeTiles.Where(e => !coarseMap.IsSolid(e)).ToList();

            // Debug.Log("--------------------------");
            var furthestAwayTileDistance = 0f;
            foreach (var edge in nonSolidEdgeTiles)
            {
                var range = Geometry.DistanceBetween(robotPosition, edge);
                if (range > furthestAwayTileDistance)
                {
                    furthestAwayTileDistance = range;
                }
                // Debug.Log($"Edge {edge.x},{edge.y}. Range: {Geometry.DistanceBetween(robotPosition, edge)}");
            }
            // Debug.Log("--------------------------");

            // Remove edges, that are as far away as our visibility range, since they cannot be occluded by anything.
            var possiblyOccludedEdges = nonSolidEdgeTiles
                .Where(c => Geometry.DistanceBetween(robotPosition, c) < furthestAwayTileDistance - 2f)
                .ToList();

            if (possiblyOccludedEdges.Count == 0)
            {
                return new List<Vector2Int>();
            }

            // Find group of open edges (possibly occluded by walls)
            var edgeTileGroups = FindGroupedTiles(possiblyOccludedEdges);

            // Take closest point from all groups
            var occlusionPoints = new List<Vector2Int>();
            foreach (var edgeGroup in edgeTileGroups)
            {
                edgeGroup.Sort((c1, c2) =>
                {
                    var c1Distance = Geometry.DistanceBetween(robotPosition, c1);
                    var c2Distance = Geometry.DistanceBetween(robotPosition, c2);
                    return c1Distance.CompareTo(c2Distance);
                });
                occlusionPoints.Add(edgeGroup[0]);
            }

            return occlusionPoints;
        }

        private List<List<Vector2Int>> FindGroupedTiles(List<Vector2Int> allTiles)
        {
            var res = new List<List<Vector2Int>>();

            // No tiles are accounted for in the beginning
            var counter = new Dictionary<Vector2Int, bool>();
            foreach (var tile in allTiles)
            {
                counter[tile] = false;
            }

            // Continue while some tiles have not yet been counted
            while (counter.ContainsValue(false))
            {
                var next = counter.First(e => e.Value == false);
                var connectedTiles = GetConnectedTiles(next.Key, allTiles);
                foreach (var tile in connectedTiles)
                {
                    counter[tile] = true;
                }

                res.Add(connectedTiles);
            }

            var totalTiles = allTiles.Count;
            var totalAfter = res.Aggregate(0, (e1, e2) => e1 + e2.Count);
            Assert.IsTrue(totalTiles == totalAfter);

            return res;
        }

        private List<Vector2Int> GetConnectedTiles(Vector2Int startTile, List<Vector2Int> allTiles)
        {
            var res = new List<Vector2Int>();
            var countFlagList = new List<Vector2Int>();
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(startTile);
            countFlagList.Add(startTile);

            while (queue.Count > 0)
            {
                var tile = queue.Dequeue();
                res.Add(tile);

                for (var x = tile.x - 1; x <= tile.x + 1; x++)
                {
                    for (var y = tile.y - 1; y <= tile.y + 1; y++)
                    {
                        if (x == tile.x || y == tile.y)
                        { // Diagonal does not count.
                            var neighTile = new Vector2Int(x, y);
                            if (!countFlagList.Contains(neighTile) && allTiles.Contains(neighTile))
                            {
                                queue.Enqueue(neighTile);
                                countFlagList.Add(neighTile);
                            }
                        }
                    }
                }
            }

            return res;
        }

        private List<Vector2Int> FindEdgeTiles(List<Vector2Int> tiles)
        {
            // An edge is any tile, where a neighbor is missing in the set of tiles.
            var edgeTiles = new List<Vector2Int>();

            foreach (var tile in tiles)
            {
                var isEdge = false;
                for (var x = tile.x - 1; x <= tile.x + 1; x++)
                {
                    for (var y = tile.y - 1; y <= tile.y + 1; y++)
                    {
                        if (x == tile.x || y == tile.y)
                        {
                            var neighbour = new Vector2Int(x, y);
                            if (!tiles.Contains(neighbour))
                            {
                                isEdge = true;
                            }
                        }
                    }
                }

                if (isEdge)
                {
                    edgeTiles.Add(tile);
                }
            }

            return edgeTiles;
        }

        private List<Vector2Int> FindUnexploredTilesWithinRegion(VoronoiRegion region)
        {
            if (region.IsEmpty())
            {
                return new List<Vector2Int>();
            }

            var coarseMap = _robotController.GetSlamMap().GetCoarseMap();

            var unExploredInRegion = region.Tiles.Where(e => !IsExploredMap.ContainsKey(e) || IsExploredMap[e] == false).ToList();

            unExploredInRegion =
                unExploredInRegion
                    .Where(e => coarseMap.GetTileStatus(e) != SlamMap.SlamTileStatus.Solid)
                    .ToList();

            // Filter out any slam tiles right next to a wall.
            return unExploredInRegion;
        }

        private void RecalculateVoronoiRegions()
        {
            var coarseMap = _robotController.GetSlamMap().GetCoarseMap();
            _localVoronoiRegions = new List<VoronoiRegion>();

            var nearbyRobots = _robotController.SenseNearbyRobots();
            var myPosition = _robotController.GetSlamMap().GetCoarseMap().GetCurrentTile();

            // If no near robots, all visible tiles are assigned to my own region
            //if (nearbyRobots.Count == 0)
            //{
            //    var visibleSlamTiles = _robotController.GetSlamMap().GetCurrentlyVisibleTiles();
            //    var visibleCoarseTiles = coarseMap.FromSlamMapCoordinates(visibleSlamTiles.Keys).ToList();
            //    var region = new VoronoiRegion(this._robotController.GetRobotID(), visibleCoarseTiles);
            //    _localVoronoiRegions.Add(region);
            //    _currentRegion = region;
            //    return;
            //}

            // Find furthest away robot. Voronoi partition should include all robots within broadcast range
            nearbyRobots.Sort((r1, r2) => r1.Distance.CompareTo(r2.Distance));

            var robotIdToClosestTilesMap = new Dictionary<int, List<Vector2Int>>();

            // Assign tiles to robots to create regions
            var currentlyVisibleTiles = _robotController.GetSlamMap().GetCurrentlyVisibleTiles();
            var currentlyVisibleCoarseTiles = coarseMap.FromSlamMapCoordinates(currentlyVisibleTiles.Keys.ToList()).ToHashSet();
            for (var x = myPosition.x - _voronoiRegionMaxDistance; x < myPosition.x + _voronoiRegionMaxDistance; x++)
            {
                for (var y = myPosition.y - _voronoiRegionMaxDistance; y < myPosition.y + _voronoiRegionMaxDistance; y++)
                {
                    // Only go through tiles within line of sight of the robot
                    if (currentlyVisibleCoarseTiles.Contains(new Vector2Int(x, y)))
                    {
                        var tilePosition = new Vector2Int(x, y);
                        var bestDistance = Geometry.DistanceBetween(myPosition, tilePosition);
                        var bestRobotId = _robotController.GetRobotID();
                        foreach (var robot in nearbyRobots)
                        {
                            var otherPosition = robot.GetRelativePosition(myPosition, _robotController.GetSlamMap().GetRobotAngleDeg());
                            var distance = Geometry.DistanceBetween(otherPosition, tilePosition);
                            if (distance < bestDistance)
                            {
                                bestDistance = distance;
                                bestRobotId = robot.item;
                            }
                        }

                        if (!robotIdToClosestTilesMap.Keys.Contains(bestRobotId))
                        {
                            robotIdToClosestTilesMap[bestRobotId] = new List<Vector2Int>();
                        }

                        robotIdToClosestTilesMap[bestRobotId].Add(tilePosition);
                    }
                }
            }

            foreach (var kv in robotIdToClosestTilesMap)
            {
                _localVoronoiRegions.Add(new VoronoiRegion(kv.Key, kv.Value));
            }

            _currentRegion = _localVoronoiRegions
                .DefaultIfEmpty(new VoronoiRegion())
                .First(k => k.RobotId == _robotController.GetRobotID());
        }

        public string GetDebugInfo()
        {
            var info = new StringBuilder();

            info.AppendLine($"Heuristic: {_heuristic}");
            if (_currentTargetPath != null && _currentTargetPath.Count > 0)
            {
                var finalTarget = _currentTargetPath[_currentTargetPath.Count - 1];
                info.Append($"Current final target: x:{finalTarget.Item1.x}, y:{finalTarget.Item1.y}\n");
            }
            else
            {
                info.Append("Voronoi has no final target\n");
            }

            if (_currentPartialMovementTarget.HasValue)
            {
                info.Append($"Current partial target: x:{_currentPartialMovementTarget.Value.x}, y:{_currentPartialMovementTarget.Value.y}\n");
            }
            else
            {
                info.Append("Voronoi has no partial target\n");
            }

            if (_currentSearchPhase == VoronoiSearchPhase.SearchMode || _currentSearchPhase == VoronoiSearchPhase.ExpandVoronoi)
            {
                info.AppendLine($"Occlusion Points within range: {_occlusionPointsWithinView}");
            }
            else
            {
                info.AppendLine($"Occlusion Points within range: Not in search mode");
            }

            if (_closestOcclusionPoint.HasValue)
            {
                info.Append($"Closest occlusion point slamtile: x:{_closestOcclusionPoint.Value.x}, y:{_closestOcclusionPoint.Value.y}\n");
            }
            else
            {
                info.Append("No closest occlusion point\n");
            }

            info.Append($"Search phase: {_currentSearchPhase}\n");
            info.Append($"Current Region size: {_regionSizeCoarseTiles}\n");
            info.Append($"Unexplored tiles in region: {_unexploredTilesInRegion}\n");
            info.Append($"Explored tiles: {IsExploredMap.Count}\n");

            if (_currentTargetPath == null)
            {
                info.Append($"No path to target found\n");
            }
            else
            {
                info.Append($"Path tiles: {string.Join(",", _currentTargetPath)}\n");
            }

            info.Append($"Explored tiles: {string.Join(",", IsExploredMap)}\n");

            return info.ToString();
        }
    }
}