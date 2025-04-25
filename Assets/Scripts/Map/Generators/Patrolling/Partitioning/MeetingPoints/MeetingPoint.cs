using System.Collections.Generic;

namespace Maes.Map.Generators.Patrolling.Partitioning.MeetingPoints
{
    public class MeetingPoint
    {
        public MeetingPoint(int vertexId, int atTicks, IReadOnlyCollection<int> robotIds)
        {
            VertexId = vertexId;
            AtTicks = atTicks;
            RobotIds = robotIds;
        }

        public int VertexId { get; }
        public int AtTicks { get; }
        public IReadOnlyCollection<int> RobotIds { get; }
    }
}