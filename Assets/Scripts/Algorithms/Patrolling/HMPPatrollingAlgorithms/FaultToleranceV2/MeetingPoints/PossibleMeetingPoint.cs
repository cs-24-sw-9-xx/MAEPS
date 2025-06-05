using System.Collections.Generic;

using Maes.Map;

namespace Maes.Algorithms.Patrolling.HMPPatrollingAlgorithms.FaultToleranceV2.MeetingPoints
{
    public struct PossibleMeetingPoint
    {
        public readonly int Robot1Id;
        public readonly int Robot2Id;
        public readonly List<(Vertex, Vertex)> Connections;

        public PossibleMeetingPoint(int robot1Id, int robot2Id)
        {
            Robot1Id = robot1Id;
            Robot2Id = robot2Id;
            Connections = new List<(Vertex, Vertex)>();
        }
    }
}