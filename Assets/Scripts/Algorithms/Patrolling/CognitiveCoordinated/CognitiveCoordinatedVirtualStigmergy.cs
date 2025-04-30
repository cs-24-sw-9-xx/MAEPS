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
        public CognitiveCoordinatedVirtualStigmergy(int amountOfRobots)
        {
            _amountOfRobots = amountOfRobots;
        }

        private readonly int _amountOfRobots;
        public override string AlgorithmName => "Cognitive Coordinated (virtual-stigmergy knowledge) Algorithm";
        private VirtualStigmergyComponent<int, int> _virtualStigmergyComponent = null!;

        protected override IComponent[] CreateComponents(IRobotController controller, PatrollingMap patrollingMap)
        {
            _controller = controller;
            _goToNextVertexComponent = new GoToNextVertexComponent(NextVertex, this, controller, patrollingMap);
            _collisionRecoveryComponent = new CollisionRecoveryComponent(controller, _goToNextVertexComponent);
            _virtualStigmergyComponent = new VirtualStigmergyComponent<int, int>((_, localKnowledge, _) => localKnowledge, controller);
            return new IComponent[] { _goToNextVertexComponent, _collisionRecoveryComponent, _virtualStigmergyComponent };
        }

        public override void OccupyVertex(int robotId, Vertex vertex)
        {
            // Update the local knowledge of the robot.
            _virtualStigmergyComponent.Put(robotId, vertex.Id);
        }

        public override IEnumerable<Vertex> GetOccupiedVertices(int robotId)
        {
            return GetCurrentState()
                    .Where(p => p.Key != robotId)
                    .Select(p => _patrollingMap.Vertices.Single(v => v.Id == p.Value))
                    .ToArray();
        }

        public override IEnumerable<Vertex> GetUnoccupiedVertices(int robotId)
        {
            var occupiedVertices = GetOccupiedVertices(robotId);
            return _patrollingMap.Vertices.Except(occupiedVertices);
        }

        private Dictionary<int, int> GetCurrentState()
        {
            var currentState = new Dictionary<int, int>();
            for (var i = 0; i < _amountOfRobots; i++)
            {
                if (_virtualStigmergyComponent.TryGet(i, out var value))
                {
                    currentState.Add(i, value);
                }
            }
            return currentState;
        }
    }
}