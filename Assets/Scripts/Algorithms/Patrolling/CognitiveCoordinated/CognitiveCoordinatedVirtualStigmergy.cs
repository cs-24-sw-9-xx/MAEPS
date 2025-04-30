using System;
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
        protected override PatrollingMap _globalMap => _patrollingMap;
        private VirtualStigmergyComponent<int, Vertex> _virtualStigmergyComponent = null!;

        protected override IComponent[] CreateComponents(IRobotController controller, PatrollingMap patrollingMap)
        {
            var components = base.CreateComponents(controller, patrollingMap);
            _virtualStigmergyComponent = new VirtualStigmergyComponent<int, Vertex>((_, localKnowledge, _) => localKnowledge, controller);
            components = components.Append(_virtualStigmergyComponent).ToArray();
            return components;
        }

        public override void InitializeCoordinator(PatrollingMap _)
        {
        }

        public override void OccupyVertex(int robotId, Vertex vertex)
        {
#if DEBUG
            if (!_globalMap.Vertices.Contains(vertex))
            {
                throw new ArgumentException($"Vertex ({vertex}) is not a part of GlobalMap.Vertices.", nameof(vertex));
            }
#endif

            // Update the local knowledge of the robot.
            _virtualStigmergyComponent.Put(robotId, vertex);
        }

        public override IEnumerable<Vertex> GetOccupiedVertices(int robotId)
        {
            return GetCurrentState()
                    .Where(p => p.Key != robotId)
                    .Select(p => p.Value);
        }

        public override IEnumerable<Vertex> GetUnoccupiedVertices(int robotId)
        {
            var occupiedVertices = GetOccupiedVertices(robotId);
            return _globalMap.Vertices.Except(occupiedVertices);
        }

        private Dictionary<int, Vertex> GetCurrentState()
        {
            var currentState = new Dictionary<int, Vertex>();
            for (var i = 0; i < _amountOfRobots; i++)
            {
                var info = _virtualStigmergyComponent.Get(i);
                if (info == null)
                {
                    continue;
                }
                currentState.Add(i, info);
            }
            return currentState;
        }
    }
}