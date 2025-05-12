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
using Maes.Algorithms.Patrolling.TrackInfos;
using Maes.Map;
using Maes.Map.Generators.Patrolling.Partitioning;
using Maes.Robot;
using Maes.Utilities;

using UnityEngine;

namespace Maes.Algorithms.Patrolling
{
    /// <summary>
    /// Partition-based patrolling algorithm with the use of meeting points in a limited communication range.
    /// Proposed by Henrik, Mads, and Puvikaran. 
    /// </summary>
    public sealed class HMPPatrollingAlgorithm : PatrollingAlgorithm
    {
        public HMPPatrollingAlgorithm(IHMPPartitionGenerator partitionGenerator, int seed = 0)
        {
            _partitionGenerator = partitionGenerator;
            _heuristicConscientiousReactiveLogic = new HeuristicConscientiousReactiveLogic(DistanceMethod, seed);
        }
        public override string AlgorithmName => "HMPAlgorithm";
        public HMPPartitionInfo PartitionInfo => _partitionComponent.PartitionInfo!;
        public override Dictionary<int, Color32[]> ColorsByVertexId => _partitionComponent.PartitionInfo?
                                                                           .VertexIds
                                                                           .ToDictionary(vertexId => vertexId, _ => new[] { Controller.Color }) ?? new Dictionary<int, Color32[]>();

        private readonly IHMPPartitionGenerator _partitionGenerator;
        private readonly HeuristicConscientiousReactiveLogic _heuristicConscientiousReactiveLogic;

        private PartitionComponent _partitionComponent = null!;
        private MeetingComponent _meetingComponent = null!;
        private MeetingObserverComponent _meetingObserverComponent = null!;
        private CollisionRecoveryComponent _collisionRecoveryComponent = null!;
        private GoToNextVertexComponent _goToNextVertexComponent = null!;

        protected override IComponent[] CreateComponents(IRobotController controller, PatrollingMap patrollingMap)
        {
            _partitionGenerator.SetMaps(patrollingMap, controller.SlamMap.CoarseMap);
            _partitionGenerator.SetEstimates(EstimateTime, target => controller.EstimateTimeToTarget(target));

            _partitionComponent = new PartitionComponent(controller, _partitionGenerator);
            _goToNextVertexComponent = new GoToNextVertexComponent(NextVertex, this, controller, patrollingMap, GetInitialVertexToPatrol);
            _meetingComponent = new MeetingComponent(-200, -200, () => LogicTicks, EstimateTime, patrollingMap, Controller, _partitionComponent, ExchangeInformation, OnMissingRobotAtMeeting, _goToNextVertexComponent);
            _collisionRecoveryComponent = new CollisionRecoveryComponent(controller, _goToNextVertexComponent);
            _meetingObserverComponent = new MeetingObserverComponent(-101, -101, _collisionRecoveryComponent, _goToNextVertexComponent, _meetingComponent);

            return new IComponent[] { _partitionComponent, _meetingComponent, _meetingObserverComponent, _collisionRecoveryComponent, _goToNextVertexComponent };
        }

        private int? EstimateTime(Vector2Int start, Vector2Int target)
        {
            return Controller.TravelEstimator.OverEstimateTime(start, target);
        }

        private Vertex GetInitialVertexToPatrol()
        {
            var vertices = PatrollingMap.Vertices.Where(vertex => PartitionInfo.VertexIds.Contains(vertex.Id)).ToArray();

            return vertices.GetClosestVertex(target => Controller.EstimateTimeToTarget(target) ?? int.MaxValue);
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
    }
}