using System.Collections.Generic;
using System.Linq;

using Maes.Algorithms.Patrolling.Components;
using Maes.Map;
using Maes.Robot;

namespace Maes.Algorithms.Patrolling
{
    /// <summary>
    /// This is a distributed version of the Cognitive Coordinated Algorithm.
    /// It uses virtual stigmergy to coordinate the robots.
    /// Each robot has its own local map and uses the occupied vertices of other robots to coordinate.
    /// </summary>
    public sealed class CognitiveCoordinatedVirtualStigmergy : CognitiveCoordinatedBase
    {
        private sealed class VertexVirtualStigmergy { }
        private sealed class OccupiedtilesVirtualStigmergy { }
        public CognitiveCoordinatedVirtualStigmergy(int amountOfRobots)
        {
            _amountOfRobots = amountOfRobots;
            SubscribeOnReachVertex(UpdateLastTimeVisitedTick);
        }

        private readonly int _amountOfRobots;
        public override string AlgorithmName => "Cognitive Coordinated (virtual-stigmergy knowledge) Algorithm";
        private VirtualStigmergyComponent<int, int, OccupiedtilesVirtualStigmergy> _occupiedTilesVirtualStigmergyComponent = null!;
        private VirtualStigmergyComponent<int, int, VertexVirtualStigmergy> _vertexLastTimeVisitedVirtualStigmergyComponent = null!;

        protected override IComponent[] CreateComponents(IRobotController controller, PatrollingMap patrollingMap)
        {
            _controller = controller;
            _goToNextVertexComponent = new GoToNextVertexComponent(NextVertex, this, controller, patrollingMap);
            _collisionRecoveryComponent = new CollisionRecoveryComponent(controller, _goToNextVertexComponent);
            _occupiedTilesVirtualStigmergyComponent = new VirtualStigmergyComponent<int, int, OccupiedtilesVirtualStigmergy>((_, localKnowledge, _) => localKnowledge, controller);
            _vertexLastTimeVisitedVirtualStigmergyComponent = new VirtualStigmergyComponent<int, int, VertexVirtualStigmergy>((_, localKnowledge, incoming) => localKnowledge.Value > incoming.Value ? localKnowledge : incoming, controller);
            return new IComponent[] { _goToNextVertexComponent, _collisionRecoveryComponent, _occupiedTilesVirtualStigmergyComponent };
        }

        public override IEnumerable<(int vertexId, int lastTimeVisitedTick)> GetLastTimeVisitedTick(IEnumerable<int> vertexIds)
        {
            var result = new List<(int vertexId, int lastTimeVisitedTick)>();
            foreach (var vertexId in vertexIds)
            {
                if (_vertexLastTimeVisitedVirtualStigmergyComponent.TryGet(vertexId, out var value))
                {
                    result.Add((vertexId, value));
                }
                else
                {
                    // This is the case when the vertex was never visited by any robot (to this robots knowledge).
                    result.Add((vertexId, _patrollingMap.Vertices.Single(v => v.Id == vertexId).LastTimeVisitedTick));
                }
            }
            return result;
        }

        private void UpdateLastTimeVisitedTick(int vertexId)
        {
            _vertexLastTimeVisitedVirtualStigmergyComponent.Put(vertexId, LogicTicks);
        }

        public override void OccupyVertex(int robotId, Vertex vertex)
        {
            // Update the local knowledge of the robot.
            _occupiedTilesVirtualStigmergyComponent.Put(robotId, vertex.Id);
        }

        private IEnumerable<Vertex> GetOccupiedVertices(int robotId)
        {
            return GetCurrentTileOccupancy()
                    .Where(p => p.Key != robotId)
                    .Select(p => _patrollingMap.Vertices.Single(v => v.Id == p.Value))
                    .ToArray();
        }

        public override IEnumerable<Vertex> GetUnoccupiedVertices(int robotId)
        {
            var occupiedVertices = GetOccupiedVertices(robotId);
            return _patrollingMap.Vertices.Except(occupiedVertices);
        }

        private Dictionary<int, int> GetCurrentTileOccupancy()
        {
            var currentState = new Dictionary<int, int>();
            for (var i = 0; i < _amountOfRobots; i++)
            {
                if (_occupiedTilesVirtualStigmergyComponent.TryGet(i, out var value))
                {
                    currentState.Add(i, value);
                }
            }
            return currentState;
        }
    }
}