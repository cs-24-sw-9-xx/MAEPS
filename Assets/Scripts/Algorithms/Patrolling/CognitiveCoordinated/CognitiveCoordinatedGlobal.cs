using System.Collections.Generic;
using System.Linq;

using Maes.Algorithms.Patrolling.Components;
using Maes.Map;
using Maes.Robot;

namespace Maes.Algorithms.Patrolling
{
    public sealed class CognitiveCoordinatedGlobal : CognitiveCoordinatedBase
    {
        public override string AlgorithmName => "Cognitive Coordinated (global knowledge) Algorithm";

        protected override IComponent[] CreateComponents(IRobotController controller, PatrollingMap patrollingMap)
        {
            _controller = controller;
            _goToNextVertexComponent = new GoToNextVertexComponent(NextVertex, this, controller, Coordinator.GlobalMap);
            _collisionRecoveryComponent = new CollisionRecoveryComponent(controller, _goToNextVertexComponent);
            return new IComponent[] { _goToNextVertexComponent, _collisionRecoveryComponent };
        }

        public override void SetGlobalPatrollingMap(PatrollingMap globalMap)
        {
            base.SetGlobalPatrollingMap(globalMap);

            Coordinator.GlobalMap = globalMap;

            Coordinator.ClearOccupiedVertices();
        }

        public override void OccupyVertex(int robotId, Vertex vertex)
        {
            Coordinator.OccupyVertex(robotId, vertex);
        }

        public override IEnumerable<Vertex> GetUnoccupiedVertices(int robotId)
        {
            return Coordinator.GetUnoccupiedVertices(robotId);
        }

        public override IEnumerable<(int vertexId, int lastTimeVisitedTick)> GetLastTimeVisitedTick(IEnumerable<int> vertexIds)
        {
            return vertexIds
                .Select(vertexId => (vertexId, Coordinator.GetLastTimeVisitedTick(vertexId)))
                .ToList();
        }

        // TODO: Find a better way to have a coordinator, so that it is not static.
        private static class Coordinator
        {
            public static PatrollingMap GlobalMap { get; set; } = null!;

            private static readonly Dictionary<int, int> VerticesOccupiedByRobot = new();

            public static IEnumerable<int> GetOccupiedVertices(int robotId)
            {
                return VerticesOccupiedByRobot
                    .Where(p => p.Key != robotId)
                    .Select(p => p.Value);
            }

            public static IEnumerable<Vertex> GetUnoccupiedVertices(int robotId)
            {
                var occupiedVertices = GetOccupiedVertices(robotId);
                return GlobalMap.Vertices.Where(v => !occupiedVertices.Contains(v.Id));
            }

            public static void OccupyVertex(int robotId, Vertex vertex)
            {
                VerticesOccupiedByRobot[robotId] = vertex.Id;
            }

            public static void ClearOccupiedVertices()
            {
                VerticesOccupiedByRobot.Clear();
            }

            public static int GetLastTimeVisitedTick(int vertexId)
            {
                return GlobalMap.Vertices
                    .Single(v => v.Id == vertexId)
                    .LastTimeVisitedTick;
            }
        }
    }
}