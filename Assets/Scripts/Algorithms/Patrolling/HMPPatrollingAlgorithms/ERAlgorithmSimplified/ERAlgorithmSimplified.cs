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
using System.Text;

using Maes.Algorithms.Patrolling.Components;
using Maes.Map;
using Maes.Robot;
using Maes.Utilities;

using UnityEngine;

namespace Maes.Algorithms.Patrolling.HMPPatrollingAlgorithms.ERAlgorithmSimplified
{
    /// <summary>
    /// A (modified) simplified version of Multi-robot patrol: A distributed algorithm based on expected idleness.
    /// By: Chuanbo Yan and Tao Zhang
    /// Changes: Remove the intention system and just set the last visited directly instead.
    /// This was done because it is unclear how each robot filters out old information from the intention set.
    /// </summary>
    public sealed class ERAlgorithmSimplified : PatrollingAlgorithm
    {

        public static Dictionary<VisitMessage, (int count, int lasttick)> RecievedMeessageOfVisitMessage = new();

        public override string AlgorithmName => "ERAlgorithm";

        private GoToNextVertexComponent _goToNextVertexComponent = null!;
        private MessageComponent _messageComponent = null!;

        private readonly Dictionary<int, int> _vertexIdToLastVisited = new Dictionary<int, int>();

        protected override IComponent[] CreateComponents(IRobotController controller, PatrollingMap patrollingMap)
        {
            foreach (var vertex in patrollingMap.Vertices)
            {
                _vertexIdToLastVisited.Add(vertex.Id, 0);
            }

            _goToNextVertexComponent = new GoToNextVertexComponent(NextVertex, this, controller, patrollingMap, GetInitialVertexToPatrol);
            _messageComponent = new MessageComponent(controller, this);

            return new IComponent[] { _goToNextVertexComponent, _messageComponent };
        }

        private Vertex GetInitialVertexToPatrol()
        {
            return PatrollingMap.Vertices.ToArray().GetClosestVertex(target => Vector2.Distance(target, Controller.SlamMap.CoarseMap.GetApproximatePosition()));
        }

        private Vertex NextVertex(Vertex currentVertex)
        {
            // We arrived!
            var currentPosition = currentVertex.Position;
            _vertexIdToLastVisited[currentVertex.Id] = Math.Max(LogicTicks, _vertexIdToLastVisited[currentVertex.Id]);
            var mm = new VisitMessage(currentVertex.Id, _vertexIdToLastVisited[currentVertex.Id]);
            Controller.Broadcast(mm);
            RecievedMeessageOfVisitMessage[mm] = (0, LogicTicks);
            var bestVertex = currentVertex.Neighbors.First();
            var maxUtility = float.NegativeInfinity;
            foreach (var vertex in currentVertex.Neighbors)
            {
                var deltaT = Controller.TravelEstimator.EstimateTime(currentPosition, vertex.Position, dependOnBrokenBehaviour: false)!.Value;
                var tNext = LogicTicks + deltaT;
                var expected = tNext - _vertexIdToLastVisited[vertex.Id];

                var utility = Mathf.Abs((float)expected) / (float)deltaT;

                if (utility > maxUtility)
                {
                    bestVertex = vertex;
                    maxUtility = utility;
                }
            }

            var m = new VisitMessage(bestVertex.Id,
                LogicTicks + Controller.TravelEstimator.EstimateTime(currentPosition, bestVertex.Position,
                    dependOnBrokenBehaviour: false)!.Value);
            // We are visiting bestVertex
            Controller.Broadcast(m);
            RecievedMeessageOfVisitMessage[m] = (0, LogicTicks);

            return bestVertex;
        }

        protected override void GetDebugInfo(StringBuilder stringBuilder)
        {
            stringBuilder.Append("Idleness:\n");
            foreach (var vertex in PatrollingMap.Vertices)
            {
                stringBuilder.AppendFormat("{0}: {1}\n", vertex.Id, _vertexIdToLastVisited[vertex.Id]);
            }
        }

        private sealed class MessageComponent : IComponent
        {
            private readonly IRobotController _robotController;
            private readonly ERAlgorithmSimplified _algorithm;
            public int PreUpdateOrder { get; } = -1000;
            public int PostUpdateOrder { get; } = -1000;

            public MessageComponent(IRobotController robotController, ERAlgorithmSimplified algorithm)
            {
                _robotController = robotController;
                _algorithm = algorithm;
            }

            public IEnumerable<ComponentWaitForCondition> PreUpdateLogic()
            {
                while (true)
                {
                    foreach (var message in _robotController.ReceiveBroadcast().OfType<VisitMessage>())
                    {
                        var (count, _) = RecievedMeessageOfVisitMessage[message];
                        count++;
                        RecievedMeessageOfVisitMessage[message] = (count, _algorithm.LogicTicks);

                        _algorithm._vertexIdToLastVisited[message.VertexId] =
                            Math.Max(_algorithm._vertexIdToLastVisited[message.VertexId], message.LastVisited);
                    }
                    yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: true);
                }
            }
        }

        public sealed class VisitMessage
        {
            public readonly int VertexId;
            public readonly int LastVisited;

            public VisitMessage(int vertexId, int lastVisited)
            {
                VertexId = vertexId;
                LastVisited = lastVisited;
            }
        }
    }
}