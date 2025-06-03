using System;
using System.Collections.Generic;

using Maes.Utilities;

namespace Maes.Algorithms.Patrolling.HMPPatrollingAlgorithms.SingleMeetingPoint.MeetingPoints
{
    public readonly struct MeetingPoint : IEquatable<MeetingPoint>
    {
        public MeetingPoint(int vertexId, int firstMeetingAtTick, int cycleIntervalTicks,
            IReadOnlyCollection<int> partitionIds)
        {
            VertexId = vertexId;
            FirstMeetingAtTick = firstMeetingAtTick;
            CycleIntervalTicks = cycleIntervalTicks;
            PartitionIds = partitionIds;
        }

        public int VertexId { get; }
        public int FirstMeetingAtTick { get; }
        public int CycleIntervalTicks { get; }
        public IReadOnlyCollection<int> PartitionIds { get; }

        public bool Equals(MeetingPoint other)
        {
            return VertexId == other.VertexId && FirstMeetingAtTick == other.FirstMeetingAtTick && CycleIntervalTicks == other.CycleIntervalTicks && PartitionIds.SetEquals(other.PartitionIds);
        }

        public override bool Equals(object? obj)
        {
            return obj is MeetingPoint other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(VertexId, FirstMeetingAtTick, CycleIntervalTicks, PartitionIds);
        }

        public static bool operator ==(MeetingPoint left, MeetingPoint right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MeetingPoint left, MeetingPoint right)
        {
            return !left.Equals(right);
        }
    }
}