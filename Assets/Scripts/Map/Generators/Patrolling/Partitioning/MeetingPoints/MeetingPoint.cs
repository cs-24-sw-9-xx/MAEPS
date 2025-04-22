using System.Collections.Generic;

namespace Maes.Map.Generators.Patrolling.Partitioning.MeetingPoints
{
    public class MeetingPoint
    {
        public MeetingPoint(int vertexId, int atTicks)
        {
            VertexId = vertexId;
            AtTicks = atTicks;
        }

        public int VertexId { get; }
        public int AtTicks { get; set; }
        public HashSet<int> RobotIds { get; } = new();
    }
}