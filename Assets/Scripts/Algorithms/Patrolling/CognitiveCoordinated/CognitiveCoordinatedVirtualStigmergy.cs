using System;
using System.Collections.Generic;
using System.Linq;

using Maes.Map;

namespace Maes.Algorithms.Patrolling
{
    /// <summary>
    /// This is a distributed version of the Cognitive Coordinated Algorithm.
    /// It uses virtual stigmergy to coordinate the robots.
    /// Each robot has its own local map and uses the occupied vertices of other robots to coordinate.
    /// </summary>
    public sealed class CognitiveCoordinatedVirtualStigmergy : CognitiveCoordinatedBase
    {
        public override string AlgorithmName => "Cognitive Coordinated (virtual-stigmergy knowledge) Algorithm";
        protected override PatrollingMap _globalMap => _patrollingMap;

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
            if (!_virtualStigmergyComponent.Has(VirtualStigmergyMessageId))
            {
                _virtualStigmergyComponent.Put(VirtualStigmergyMessageId, new Dictionary<int, Vertex>());
            }

            // Update the local knowledge of the robot.
            var _localKnowledge = _virtualStigmergyComponent.GetNonSending(VirtualStigmergyMessageId)!;
            _localKnowledge[robotId] = vertex;
            _virtualStigmergyComponent.Put(VirtualStigmergyMessageId, _localKnowledge);
        }

        public override IEnumerable<Vertex> GetOccupiedVertices(int robotId)
        {
            // Hacky solution to initialize the local knowledge of the robot.
            if (!_virtualStigmergyComponent.Has(VirtualStigmergyMessageId))
            {
                return Enumerable.Empty<Vertex>();
            }
            return _virtualStigmergyComponent.Get(VirtualStigmergyMessageId)
                    .Where(p => p.Key != robotId)
                    .Select(p => p.Value);
        }

        public override IEnumerable<Vertex> GetUnoccupiedVertices(int robotId)
        {
            var occupiedVertices = GetOccupiedVertices(robotId);
            return _globalMap.Vertices.Except(occupiedVertices);
        }
    }
}