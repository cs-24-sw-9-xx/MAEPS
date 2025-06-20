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
// Contributors 2025: 
// Henrik van Peet,
// Mads Beyer Mogensen,
// Puvikaran Santhirasegaram

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

using Maes.Algorithms.Patrolling.Components;
using Maes.Algorithms.Patrolling.HeuristicConscientiousReactive;
using Maes.Algorithms.Patrolling.HMPPatrollingAlgorithms.ImmediateTakeover.MeetingPoints;
using Maes.Algorithms.Patrolling.HMPPatrollingAlgorithms.ImmediateTakeover.TrackInfos;
using Maes.Map;
using Maes.Map.Generators.Patrolling.Partitioning;
using Maes.Map.Generators.Patrolling.Partitioning.MeetingPoints;
using Maes.Map.Generators.Patrolling.Waypoints.Connectors;
using Maes.Robot;
using Maes.Utilities;

using UnityEngine;

namespace Maes.Algorithms.Patrolling.HMPPatrollingAlgorithms.ImmediateTakeover
{
    /// <summary>
    /// Partition-based patrolling algorithm with the use of meeting points in a limited communication range.
    /// Proposed by Henrik, Mads, and Puvikaran. 
    /// </summary>
    public sealed class HMPPatrollingAlgorithm : PatrollingAlgorithm
    {
        /// <summary>
        /// Takes a partition, the number of meeting points (M), and the max travel time (D), and return the estimated interval between meetings.
        /// </summary>
        public delegate int PartitionMeetingIntervalEstimator(UnfinishedPartitionInfo partition, int numberOfMeetingPoints, int maxTravelTime);

        private readonly PartitionMeetingIntervalEstimator _partitionMeetingIntervalEstimator;

        public HMPPatrollingAlgorithm(PartitionComponent.TakeoverStrategy takeoverStrategy, int seed,
            PartitionMeetingIntervalEstimator? partitionMeetingIntervalEstimator = null)
        {
            _heuristicConscientiousReactiveLogic = new HeuristicConscientiousReactiveLogic(DistanceMethod, seed);
            _takeoverStrategy = takeoverStrategy;
            _random = new System.Random(seed);
            _partitionMeetingIntervalEstimator = partitionMeetingIntervalEstimator ?? DefaultPartitionMeetingIntervalEstimator;
        }

        private readonly System.Random _random;
        public StrongBox<int> RobotId = null!;
        public override string AlgorithmName => "HMPAlgorithm" + Enum.GetName(typeof(PartitionComponent.TakeoverStrategy), _takeoverStrategy);
        public PartitionInfo PartitionInfo => _partitionComponent.PartitionInfo!;
        public override Dictionary<int, Color32[]> ColorsByVertexId => _partitionComponent.PartitionInfo?
                                                                           .VertexIds
                                                                           .ToDictionary(vertexId => vertexId, _ => new[] { Controller.Color }) ?? new Dictionary<int, Color32[]>();

        private readonly HeuristicConscientiousReactiveLogic _heuristicConscientiousReactiveLogic;
        private readonly PartitionComponent.TakeoverStrategy _takeoverStrategy;
        private PartitionComponent _partitionComponent = null!;
        private MeetingComponent _meetingComponent = null!;
        private GoToNextVertexComponent _goToNextVertexComponent = null!;

        protected override IComponent[] CreateComponents(IRobotController controller, PatrollingMap patrollingMap)
        {
            RobotId = new StrongBox<int>(controller.Id);
            _partitionComponent = new PartitionComponent(RobotId, GeneratePartitions, _takeoverStrategy, _random);
            _goToNextVertexComponent = new GoToNextVertexComponent(NextVertex, this, controller, patrollingMap, GetInitialVertexToPatrol);
            _meetingComponent = new MeetingComponent(-200, -200, () => LogicTicks, EstimateTime, patrollingMap, Controller, _partitionComponent, ExchangeInformation, OnMissingRobotAtMeeting, _goToNextVertexComponent, RobotId);

            return new IComponent[] { _partitionComponent, _meetingComponent, _goToNextVertexComponent };
        }

        private int? EstimateTime(Vector2Int start, Vector2Int target)
        {
            return Controller.TravelEstimator.OverEstimateTime(start, target, dependOnBrokenBehaviour: false);
        }

        private Vertex GetInitialVertexToPatrol()
        {
            var vertices = PatrollingMap.Vertices.Where(vertex => PartitionInfo.VertexIds.Contains(vertex.Id)).ToArray();

            return vertices.GetClosestVertex(target => Controller.EstimateTimeToTarget(target, dependOnBrokenBehaviour: false) ?? int.MaxValue);
        }

        private Vertex NextVertex(Vertex currentVertex)
        {
            var suggestedVertex = _heuristicConscientiousReactiveLogic.NextVertex(currentVertex,
                currentVertex.Neighbors.Where(vertex => PartitionInfo.VertexIds.Contains(vertex.Id)).ToArray());

            return _meetingComponent.NextVertex(currentVertex, suggestedVertex);
        }

        private IEnumerable<ComponentWaitForCondition> ExchangeInformation(MeetingComponent.Meeting meeting)
        {
            TrackInfo(new ExchangeInfoAtMeetingTrackInfo(meeting, LogicTicks, Controller.Id));
            foreach (var condition in _partitionComponent.ExchangeInformation())
            {
                yield return condition;
            }
        }

        private IEnumerable<ComponentWaitForCondition> OnMissingRobotAtMeeting(MeetingComponent.Meeting meeting, HashSet<int> missingRobotIds)
        {
            TrackInfo(new MissingRobotsAtMeetingTrackInfo(meeting, missingRobotIds, Controller.Id));

            foreach (var condition in _partitionComponent.OnMissingRobotAtMeeting(meeting, missingRobotIds))
            {
                yield return condition;
            }
        }

        private float DistanceMethod(Vertex source, Vertex target)
        {
            if (PatrollingMap.Paths.TryGetValue((source.Id, target.Id), out var path))
            {
                return path.Sum(p => Vector2Int.Distance(p.Start, p.End));
            }

            throw new Exception($"Path from {source.Id} to {target.Id} not found");
        }

        private Dictionary<int, PartitionInfo> GeneratePartitions(HashSet<int> robotIds)
        {
            var map = Controller.SlamMap.CoarseMap;
            var collisionMap = MapUtilities.MapToBitMap(map);

            var vertexIdByPosition = PatrollingMap.Vertices.ToDictionary(v => v.Position, v => v.Id);

            var vertexPositions = vertexIdByPosition.Keys.ToList();
            var distanceMatrix = MapUtilities.CalculateDistanceMatrix(collisionMap, vertexPositions);
            var clusters =
                SpectralBisectionPartitioningGenerator.Generator(distanceMatrix, vertexPositions, robotIds.Count);

            var vertexIdsPartitions = clusters.Select(vertexPoints => vertexPoints.Select(point => vertexIdByPosition[point]).ToHashSet()).ToArray();

            var partitionsWithNoMeetingPointsById = vertexIdsPartitions.Select((vertexIds, id) => (vertexIds, id)).ToDictionary(partition => partition.id, partition => new UnfinishedPartitionInfo(partition.id, partition.vertexIds));

            var partitionsWithMeetingPointsById = AddMissingMeetingPointsForNeighborPartitions(partitionsWithNoMeetingPointsById, distanceMatrix);

            var meetingRobotIdsByVertexId = FindMeetingRobotsAtMeetingPoints(partitionsWithMeetingPointsById.Values.ToArray());

            var meetingPointsByPartitionId = GetMeetingPointsByPartitionId(meetingRobotIdsByVertexId, partitionsWithMeetingPointsById);

            var hmpPartitionsById = new Dictionary<int, PartitionInfo>();
            foreach (var (partitionId, partitionInfo) in partitionsWithMeetingPointsById)
            {
                hmpPartitionsById[partitionId] = new PartitionInfo(partitionInfo.RobotId, partitionInfo.VertexIds, meetingPointsByPartitionId[partitionId]);
            }

            return hmpPartitionsById;
        }

        private Dictionary<int, UnfinishedPartitionInfo> AddMissingMeetingPointsForNeighborPartitions(Dictionary<int, UnfinishedPartitionInfo> partitions, Dictionary<(Vector2Int, Vector2Int), int> distanceMatrix)
        {
            var verticesReverseNearestNeighbors = PatrollingMap.Vertices.Select(v => new Vertex(v.Id, v.Position, v.Partition, v.Color)).ToArray();
            ReverseNearestNeighborWaypointConnector.ConnectVertices(verticesReverseNearestNeighbors, distanceMatrix);

            var neighborsPartitionsWithNoCommonVertices = GetNeighborsPartitionsWithNoCommonVertices(partitions, verticesReverseNearestNeighbors);
            foreach (var meetingPoint in neighborsPartitionsWithNoCommonVertices)
            {
                var shortestConnection = meetingPoint.Connections[0];
                var shortestDistance = SquaredDistanceOfConnection(PatrollingMap, shortestConnection);

                foreach (var connection in meetingPoint.Connections.Skip(1))
                {
                    var distance = SquaredDistanceOfConnection(PatrollingMap, connection);

                    if (!(distance < shortestDistance))
                    {
                        continue;
                    }

                    shortestConnection = connection;
                    shortestDistance = distance;
                }

                // Add the vertex id of the best connection to the partition with the smallest amount of vertices, such that vertex would be the shared meeting point
                // This is done to only extend the partition with the smallest amount of vertices
                var robotIdWithSmallestPartition = partitions[meetingPoint.Robot1Id].VertexIds.Count < partitions[meetingPoint.Robot2Id].VertexIds.Count
                    ? meetingPoint.Robot1Id
                    : meetingPoint.Robot2Id;

                // One of the partitions would already contain the vertex id of the best connection, so that will be a redundant since duplicates can not happen because of HashSet
                var partitionToReplace = partitions[robotIdWithSmallestPartition];
                var newVertexIds = new HashSet<int>(partitionToReplace.VertexIds)
                {
                    shortestConnection.Item1.Id, shortestConnection.Item2.Id
                };
                partitions[robotIdWithSmallestPartition] = new UnfinishedPartitionInfo(partitionToReplace.RobotId, newVertexIds);
            }

            return partitions;
        }

        private static double SquaredDistanceOfConnection(PatrollingMap patrollingMap, (Vertex, Vertex) connection)
        {
            return patrollingMap.SquaredDistanceBetweenVertices(connection.Item1.Id, connection.Item2.Id);
        }

        private static Dictionary<int, HashSet<int>> FindMeetingRobotsAtMeetingPoints(UnfinishedPartitionInfo[] partitions)
        {
            var meetingPointVertexByVertexId = new Dictionary<int, HashSet<int>>();

            foreach (var (partition1, partition2) in partitions.Combinations())
            {
                var intersectionVertexIds =
                    partition1.VertexIds
                        .Intersect(partition2.VertexIds)
                        .ToArray();

                if (intersectionVertexIds.Length == 0)
                {
                    continue;
                }

                foreach (var vertexId in intersectionVertexIds)
                {
                    if (!meetingPointVertexByVertexId.TryGetValue(vertexId, out var meetingPoint))
                    {
                        meetingPoint = new HashSet<int>();
                        meetingPointVertexByVertexId[vertexId] = meetingPoint;
                    }

                    meetingPoint.Add(partition1.RobotId);
                    meetingPoint.Add(partition2.RobotId);
                }
            }

            return meetingPointVertexByVertexId;
        }

        private Dictionary<int, List<MeetingPoint>> GetMeetingPointsByPartitionId(
            Dictionary<int, HashSet<int>> meetingRobotIdsByVertexId, Dictionary<int, UnfinishedPartitionInfo> partitionsById)
        {
            // We have only a single partition
            if (partitionsById.Count == 1)
            {
                return new Dictionary<int, List<MeetingPoint>>()
                {
                    {partitionsById.Keys.Single(), new List<MeetingPoint>()}
                };
            }

            var globalMeetingIntervalTicks = GetGlobalMeetingIntervalTicks(partitionsById, meetingRobotIdsByVertexId);
            var tickColorAssignment = new WelshPowellMeetingPointColorer(meetingRobotIdsByVertexId).Run();
            var globalMeetingCycleTicks = globalMeetingIntervalTicks * tickColorAssignment.Values.Max();

            var startMeetingAfterTicks = GetWhenToStartMeeting(partitionsById.Values);

            var meetingPointsByPartitionId = new Dictionary<int, List<MeetingPoint>>();
            foreach (var (vertexId, meetingPartitionIds) in meetingRobotIdsByVertexId)
            {
                var meetingPoint = new MeetingPoint(vertexId, globalMeetingCycleTicks, startMeetingAfterTicks + globalMeetingIntervalTicks * tickColorAssignment[vertexId], meetingPartitionIds);
                foreach (var partitionId in meetingPartitionIds)
                {
                    if (!meetingPointsByPartitionId.TryGetValue(partitionId, out var meetingPoints))
                    {
                        meetingPoints = new List<MeetingPoint>();
                        meetingPointsByPartitionId[partitionId] = meetingPoints;
                    }

                    meetingPoints.Add(meetingPoint);
                }
            }

            return meetingPointsByPartitionId;
        }

        private int GetGlobalMeetingIntervalTicks(Dictionary<int, UnfinishedPartitionInfo> partitionsById,
            Dictionary<int, HashSet<int>> meetingRobotIdsByVertexId)
        {
            var estimatedPartitionMeetingIntervalTicks = new List<int>();
            foreach (var (robotId, partitionInfo) in partitionsById)
            {
                var numberOfMeetingPoints = meetingRobotIdsByVertexId
                    .Count(m => m.Value.Contains(robotId));
                var estimated = EstimatePartitionMeetingIntervalTicks(partitionInfo, numberOfMeetingPoints);
                estimatedPartitionMeetingIntervalTicks.Add(estimated);
            }

            return estimatedPartitionMeetingIntervalTicks.Max();
        }

        private int EstimatePartitionMeetingIntervalTicks(UnfinishedPartitionInfo partition, int numberOfMeetingPoints)
        {
            var maxTravelTime = EstimateMaxTravelTimeForPartition(partition);
            return _partitionMeetingIntervalEstimator(partition, numberOfMeetingPoints, maxTravelTime);
        }

        private static int DefaultPartitionMeetingIntervalEstimator(UnfinishedPartitionInfo partition, int numberOfMeetingPoints, int maxTravelTime)
        {
            // Default formula: (partition size / number of meeting points) * max travel time
            return (int)Math.Ceiling((double)partition.VertexIds.Count / numberOfMeetingPoints) * maxTravelTime;
        }

        private int EstimateMaxTravelTimeForPartition(UnfinishedPartitionInfo partition)
        {
            var maxTicks = 0;

            var vertexPositions = PatrollingMap.Vertices
                .Where(v => partition.VertexIds.Contains(v.Id))
                .Select(v => v.Position)
                .ToArray();

            foreach (var (vertexPosition1, vertexPosition2) in vertexPositions.Combinations())
            {
                var ticks = EstimateTime(vertexPosition1, vertexPosition2);
                if (ticks == null)
                {
                    continue;
                }

                maxTicks = Mathf.Max(maxTicks, ticks.Value);
            }

            return maxTicks;
        }

        private int GetWhenToStartMeeting(IEnumerable<UnfinishedPartitionInfo> partitionInfos)
        {
            var startMeetingAfterTicks = 0;

            foreach (var partitionInfo in partitionInfos)
            {
                int? timeToClosestVertex = null;
                foreach (var vertex in PatrollingMap.Vertices.Where(v => partitionInfo.VertexIds.Contains(v.Id)))
                {
                    var timeToTarget = EstimateTime(Controller.SlamMap.CoarseMap.GetCurrentPosition(), vertex.Position) ?? int.MaxValue;
                    if (timeToClosestVertex == null || timeToTarget < timeToClosestVertex)
                    {
                        timeToClosestVertex = timeToTarget;
                    }
                }

                if (timeToClosestVertex > startMeetingAfterTicks)
                {
                    startMeetingAfterTicks = timeToClosestVertex.Value;
                }
            }

            return startMeetingAfterTicks;
        }

        public static List<PossibleMeetingPoint> GetNeighborsPartitionsWithNoCommonVertices(Dictionary<int, UnfinishedPartitionInfo> partitionInfoByRobotId, IReadOnlyCollection<Vertex> vertices)
        {
            var possibleMeetingPoints = new List<PossibleMeetingPoint>();

            var combinationOfPartitions = partitionInfoByRobotId.Values.Combinations();
            foreach (var (partitionInfo1, partitionInfo2) in combinationOfPartitions)
            {
                if (partitionInfo1.VertexIds.Any(v => partitionInfo2.VertexIds.Contains(v)))
                {
                    continue;
                }

                var possibleMeetingPoint = GetPossibleMeetingPointsForPartitions(partitionInfo1, partitionInfo2, vertices);
                if (possibleMeetingPoint.Connections.Count > 0)
                {
                    possibleMeetingPoints.Add(possibleMeetingPoint);
                }
            }

            return possibleMeetingPoints;
        }

        private static PossibleMeetingPoint GetPossibleMeetingPointsForPartitions(UnfinishedPartitionInfo partitionInfo1, UnfinishedPartitionInfo partitionInfo2, IReadOnlyCollection<Vertex> vertices)
        {
            var possibleMeetingPoint = new PossibleMeetingPoint(partitionInfo1.RobotId, partitionInfo2.RobotId);
            var partition1Vertices = vertices.Where(vertex => partitionInfo1.VertexIds.Contains(vertex.Id));
            foreach (var vertex in partition1Vertices)
            {
                foreach (var neighborVertex in vertex.Neighbors)
                {
                    if (partitionInfo2.VertexIds.Contains(neighborVertex.Id)) // Check if the neighbor is in the second partition
                    {
                        possibleMeetingPoint.Connections.Add((vertex, neighborVertex));
                    }
                }
            }
            return possibleMeetingPoint;
        }
    }
}