using System;
using System.Collections.Generic;
using System.Linq;

using Maes.Map;

namespace Maes.Algorithms.Patrolling
{
    public sealed class CognitiveCoordinatedGlobal : CognitiveCoordinatedBase
    {
        public override string AlgorithmName => "Cognitive Coordinated (global knowledge) Algorithm";

        protected override PatrollingMap GlobalMap => Coordinator.GlobalMap;

        public override void InitializeCoordinator(PatrollingMap globalMap)
        {
            Coordinator.GlobalMap = globalMap;

            Coordinator.ClearOccupiedVertices();
        }

        public override void OccupyVertex(int robotId, Vertex vertex)
        {
            Coordinator.OccupyVertex(robotId, vertex);
        }

        public override IEnumerable<Vertex> GetOccupiedVertices(int robotId)
        {
            return Coordinator.GetOccupiedVertices(robotId);
        }

        public override IEnumerable<Vertex> GetUnoccupiedVertices(int robotId)
        {
            return Coordinator.GetUnoccupiedVertices(robotId);
        }

        // TODO: Find a better way to have a coordinator, so that it is not static.
        private static class Coordinator
        {
            public static PatrollingMap GlobalMap { get; set; } = null!;

            private static readonly Dictionary<int, Vertex> VerticesOccupiedByRobot = new();

            public static IEnumerable<Vertex> GetOccupiedVertices(int robotId)
            {
                return VerticesOccupiedByRobot
                    .Where(p => p.Key != robotId)
                    .Select(p => p.Value);
            }

            public static IEnumerable<Vertex> GetUnoccupiedVertices(int robotId)
            {
                var occupiedVertices = GetOccupiedVertices(robotId);
                return GlobalMap.Vertices.Except(occupiedVertices);
            }

            public static void OccupyVertex(int robotId, Vertex vertex)
            {
#if DEBUG
                if (!GlobalMap.Vertices.Contains(vertex))
                {
                    throw new ArgumentException($"Vertex ({vertex}) is not a part of GlobalMap.Vertices.", nameof(vertex));
                }
#endif

                VerticesOccupiedByRobot[robotId] = vertex;
            }

            public static void ClearOccupiedVertices()
            {
                VerticesOccupiedByRobot.Clear();
            }
        }
    }
}