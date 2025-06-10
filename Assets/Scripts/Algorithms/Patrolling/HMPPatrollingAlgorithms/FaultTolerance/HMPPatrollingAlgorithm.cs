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

using Maes.Algorithms.Patrolling.Components;
using Maes.Algorithms.Patrolling.HeuristicConscientiousReactive;
using Maes.Algorithms.Patrolling.HMPPatrollingAlgorithms.FaultTolerance.MeetingPoints;
using Maes.Algorithms.Patrolling.HMPPatrollingAlgorithms.FaultTolerance.TrackInfos;
using Maes.Map;
using Maes.Map.Generators.Patrolling.Partitioning;
using Maes.Map.Generators.Patrolling.Partitioning.MeetingPoints;
using Maes.Map.Generators.Patrolling.Waypoints.Connectors;
using Maes.Robot;
using Maes.Utilities;

using UnityEngine;

namespace Maes.Algorithms.Patrolling.HMPPatrollingAlgorithms.FaultTolerance
{
    /// <summary>
    /// Partition-based patrolling algorithm with the use of meeting points in a limited communication range.
    /// Proposed by Henrik, Mads, and Puvikaran. 
    /// </summary>
    public sealed class HMPPatrollingAlgorithm : PatrollingAlgorithm
    {
        public HMPPatrollingAlgorithm(int seed)
        {
            _heuristicConscientiousReactiveLogic = new HeuristicConscientiousReactiveLogic(DistanceMethod, seed);
        }
        public override string AlgorithmName => "HMPAlgorithm";

        public IEnumerable<int> VerticesByIdToPatrol => _partitionComponent.VerticesByIdToPatrol;

        private readonly Dictionary<int, Color32[]> _colorsByVertexId = new();
        public override Dictionary<int, Color32[]> ColorsByVertexId
        {
            get
            {
                if (_colorsByVertexId.Keys.SetEquals(VerticesByIdToPatrol))
                {
                    return _colorsByVertexId;
                }

                _colorsByVertexId.Clear();
                foreach (var vertex in VerticesByIdToPatrol)
                {
                    _colorsByVertexId[vertex] = new[] { Controller.Color };
                }

                return _colorsByVertexId;
            }
        }

        private readonly HeuristicConscientiousReactiveLogic _heuristicConscientiousReactiveLogic;

        private PartitionComponent _partitionComponent = null!;
        private MeetingComponent _meetingComponent = null!;
        private GoToNextVertexComponent _goToNextVertexComponent = null!;

        protected override IComponent[] CreateComponents(IRobotController controller, PatrollingMap patrollingMap)
        {
            _partitionComponent = new PartitionComponent(controller, GeneratePartitions);
            _goToNextVertexComponent = new GoToNextVertexComponent(NextVertex, this, controller, patrollingMap, GetInitialVertexToPatrol);
            _meetingComponent = new MeetingComponent(-200, -200, () => LogicTicks, EstimateTime, patrollingMap, Controller, _partitionComponent, ExchangeInformation);

            return new IComponent[] { _partitionComponent, _meetingComponent, _goToNextVertexComponent };
        }

        private int? EstimateTime(Vector2Int start, Vector2Int target)
        {
            return Controller.TravelEstimator.OverEstimateTime(start, target, dependOnBrokenBehaviour: false);
        }

        private Vertex GetInitialVertexToPatrol()
        {
            var vertices = PatrollingMap.Vertices.Where(vertex => VerticesByIdToPatrol.Contains(vertex.Id)).ToArray();

            return vertices.GetClosestVertex(target => Controller.EstimateTimeToTarget(target, dependOnBrokenBehaviour: false) ?? int.MaxValue);
        }

        private Vertex NextVertex(Vertex currentVertex)
        {
            var suggestedVertex = _heuristicConscientiousReactiveLogic.NextVertex(currentVertex,
                currentVertex.Neighbors.Where(vertex => VerticesByIdToPatrol.Contains(vertex.Id)).ToArray());

            return _meetingComponent.NextVertex(currentVertex, suggestedVertex);
        }

        private IEnumerable<ComponentWaitForCondition> ExchangeInformation(MeetingComponent.Meeting meeting)
        {
            TrackInfo(new ExchangeInfoAtMeetingTrackInfo(meeting, LogicTicks, Controller.Id));
            foreach (var condition in _partitionComponent.ExchangeInformation(meeting.Vertex.Id))
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

            var partitionsWithDiametersById =
                GetPartitionDiameters(partitionsWithMeetingPointsById, meetingRobotIdsByVertexId);

            var meetingPointsByPartitionId = GetMeetingPointsByPartitionId(meetingRobotIdsByVertexId, partitionsWithDiametersById);

            var hmpPartitionsById = new Dictionary<int, PartitionInfo>();
            foreach (var (partitionId, partitionInfo) in partitionsWithMeetingPointsById)
            {
                hmpPartitionsById[partitionId] = new PartitionInfo(partitionInfo.RobotId, partitionInfo.VertexIds, meetingPointsByPartitionId[partitionId], partitionsWithDiametersById[partitionId].Diameter);
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
            Dictionary<int, HashSet<int>> meetingRobotIdsByVertexId, Dictionary<int, UnfinishedPartitionInfoWithDiameter> partitionsById)
        {
            // We have only a single partition
            if (partitionsById.Count == 1)
            {
                return new Dictionary<int, List<MeetingPoint>>()
                {
                    {partitionsById.Keys.Single(), new List<MeetingPoint>()}
                };
            }

            var globalMeetingIntervalTicks = GetGlobalMeetingIntervalTicks(partitionsById);
            var tickColorAssignment = new WelshPowellMeetingPointColorer(meetingRobotIdsByVertexId).Run();
            var maximumTickColorAssignment = tickColorAssignment.Values.Max();

            var startMeetingAfterTicks = GetWhenToStartMeeting(partitionsById.Values);

            var meetingPointsByPartitionId = new Dictionary<int, List<MeetingPoint>>();
            foreach (var (vertexId, meetingPartitionIds) in meetingRobotIdsByVertexId)
            {
                var initialCurrentNextMeetingAtTick =
                    startMeetingAfterTicks + globalMeetingIntervalTicks * tickColorAssignment[vertexId];
                var initialNextNextMeetingAtTick = initialCurrentNextMeetingAtTick +
                                                   globalMeetingIntervalTicks * maximumTickColorAssignment;
                var meetingPoint = new MeetingPoint(vertexId, initialCurrentNextMeetingAtTick, initialNextNextMeetingAtTick, meetingPartitionIds);
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

        private Dictionary<int, UnfinishedPartitionInfoWithDiameter> GetPartitionDiameters(
            Dictionary<int, UnfinishedPartitionInfo> partitionsById,
            Dictionary<int, HashSet<int>> meetingRobotIdsByVertexId)
        {
            return partitionsById.ToDictionary(kv => kv.Key, kv =>
            {
                var robotId = kv.Key;
                var partitionInfo = kv.Value;
                var numberOfMeetingPoints = meetingRobotIdsByVertexId
                    .Count(m => m.Value.Contains(robotId));
                var estimated = EstimatePartitionMeetingIntervalTicks(partitionInfo, numberOfMeetingPoints);
                return new UnfinishedPartitionInfoWithDiameter(partitionInfo.RobotId, partitionInfo.VertexIds,
                    estimated);
            });
        }

        private int GetGlobalMeetingIntervalTicks(Dictionary<int, UnfinishedPartitionInfoWithDiameter> partitionsById)
        {
            return partitionsById.Values.Select(p => p.Diameter).Max();
        }

        private int EstimatePartitionMeetingIntervalTicks(UnfinishedPartitionInfo partition, int numberOdMeetingPoints)
        {
            var maxTravelTime = EstimateMaxTravelTimeForPartition(partition);
            return 2 * (int)Math.Ceiling((double)partition.VertexIds.Count / numberOdMeetingPoints) * maxTravelTime;
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

        private int GetWhenToStartMeeting(IEnumerable<UnfinishedPartitionInfoWithDiameter> partitionInfos)
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