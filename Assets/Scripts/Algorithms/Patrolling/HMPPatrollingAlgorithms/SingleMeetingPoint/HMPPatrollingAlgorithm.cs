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
using Maes.Algorithms.Patrolling.HMPPatrollingAlgorithms.SingleMeetingPoint.MeetingPoints;
using Maes.Algorithms.Patrolling.HMPPatrollingAlgorithms.SingleMeetingPoint.TrackInfos;
using Maes.Map;
using Maes.Map.Generators.Patrolling.Partitioning;
using Maes.Robot;
using Maes.Utilities;

using UnityEngine;

namespace Maes.Algorithms.Patrolling.HMPPatrollingAlgorithms.SingleMeetingPoint
{
    /// <summary>
    /// Partition-based patrolling algorithm with the use of meeting points in a limited communication range.
    /// Proposed by Henrik, Mads, and Puvikaran. 
    /// </summary>
    public sealed class HMPPatrollingAlgorithm : PatrollingAlgorithm
    {
        public HMPPatrollingAlgorithm(int seed = 0)
        {
            _heuristicConscientiousReactiveLogic = new HeuristicConscientiousReactiveLogic(DistanceMethod, seed);
        }
        public override string AlgorithmName => "HMPAlgorithm";

        private IEnumerable<int> VerticesByIdToPatrol => _partitionComponent.VerticesByIdToPatrol;

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
            _partitionComponent = new PartitionComponent(controller, GenerateAssignments);
            _goToNextVertexComponent = new GoToNextVertexComponent(NextVertex, this, controller, patrollingMap, GetInitialVertexToPatrol);
            _meetingComponent = new MeetingComponent(-200, -200, () => LogicTicks, EstimateTime, patrollingMap, _partitionComponent, ExchangeInformation);

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

        private float DistanceMethod(Vertex source, Vertex target)
        {
            if (PatrollingMap.Paths.TryGetValue((source.Id, target.Id), out var path))
            {
                return path.Sum(p => Vector2Int.Distance(p.Start, p.End));
            }

            throw new Exception($"Path from {source.Id} to {target.Id} not found");
        }

        private PartitionComponent.PartitionGeneratorResult GenerateAssignments(HashSet<int> robotIds)
        {
            var map = Controller.SlamMap.CoarseMap;
            var collisionMap = MapUtilities.MapToBitMap(map);

            var vertexByPosition = PatrollingMap.Vertices.ToDictionary(v => v.Position, v => v);

            var vertexPositions = vertexByPosition.Keys.ToList();
            var distanceMatrix = MapUtilities.CalculateDistanceMatrix(collisionMap, vertexPositions);
            var clusters =
                SpectralBisectionPartitioningGenerator.Generator(distanceMatrix, vertexPositions, robotIds.Count);

            var partitions = clusters.Select(vertexPoints => vertexPoints.Select(point => vertexByPosition[point]).ToHashSet()).ToArray();

            var meetingVertex = GetSingleMeeting(distanceMatrix, partitions);

            foreach (var vertexIds in partitions)
            {
                vertexIds.Add(meetingVertex);
            }

            var diameterPartitions = GetPartitionDiameters(partitions, 1);
            var globalMeetingIntervalTicks = diameterPartitions.Max();
            var startMeetingAfterTicks = GetWhenToStartMeeting(partitions);
            var meetingPoint = new MeetingPoint(meetingVertex.Id, startMeetingAfterTicks + globalMeetingIntervalTicks, globalMeetingIntervalTicks, robotIds);

            var assignmentByRobotId = new Dictionary<int, Assignment>();
            for (var i = 0; i < partitions.Length; i++)
            {
                assignmentByRobotId[i] = new Assignment(partitions[i].Select(v => v.Id).ToHashSet());
            }

            return new PartitionComponent.PartitionGeneratorResult(assignmentByRobotId, meetingPoint);
        }

        private Vertex GetSingleMeeting(IReadOnlyDictionary<(Vector2Int, Vector2Int), int> distanceMatrix, HashSet<Vertex>[] partitions)
        {
            Vertex? shortestVertex = null;
            var shortestDistance = float.MaxValue;

            foreach (var vertex in PatrollingMap.Vertices)
            {
                var distance = 0f;
                foreach (var partition in partitions)
                {
                    distance = Mathf.Max(distance, GetShortestDistanceVertex(vertex, partition));
                }
                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    shortestVertex = vertex;
                }
            }

            return shortestVertex!;

            float GetShortestDistanceVertex(Vertex vertex, IEnumerable<Vertex> vertices)
            {
                var shortestDistanceToVertex = float.MaxValue;

                foreach (var partitionVertex in vertices)
                {
                    if (partitionVertex == vertex)
                    {
                        return 0f;
                    }

                    var distance = distanceMatrix[(vertex.Position, partitionVertex.Position)];
                    if (distance < shortestDistanceToVertex)
                    {
                        shortestDistanceToVertex = distance;
                    }
                }

                return shortestDistanceToVertex;
            }
        }

        private IEnumerable<int> GetPartitionDiameters(
            HashSet<Vertex>[] partitions,
            int numberOfMeetingPoints)
        {
            foreach (var vertexIds in partitions)
            {
                yield return EstimatePartitionMeetingIntervalTicks(vertexIds, numberOfMeetingPoints);
            }
        }

        private int EstimatePartitionMeetingIntervalTicks(HashSet<Vertex> vertexIds, int numberOfMeetingPoints)
        {
            var maxTravelTime = EstimateMaxTravelTimeForPartition(vertexIds);
            return 2 * (int)Math.Ceiling((double)vertexIds.Count / numberOfMeetingPoints) * maxTravelTime;
        }

        private int EstimateMaxTravelTimeForPartition(HashSet<Vertex> vertices)
        {
            var maxTicks = 0;

            var vertexPositions = vertices
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

        private int GetWhenToStartMeeting(HashSet<Vertex>[] partitions)
        {
            var startMeetingAfterTicks = 0;

            foreach (var partition in partitions)
            {
                int? timeToClosestVertex = null;
                foreach (var vertex in partition)
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
    }
}