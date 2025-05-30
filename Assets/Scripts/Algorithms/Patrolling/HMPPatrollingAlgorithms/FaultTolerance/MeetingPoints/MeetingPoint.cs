using System;
using System.Collections.Generic;

using Maes.Utilities;

namespace Maes.Algorithms.Patrolling.HMPPatrollingAlgorithms.FaultTolerance.MeetingPoints
{
    public readonly struct MeetingPoint : IEquatable<MeetingPoint>
    {
        public MeetingPoint(int vertexId, int initialCurrentNextMeetingAtTick, int initialNextNextMeetingAtTick,
            IReadOnlyCollection<int> partitionIds)
        {
            VertexId = vertexId;
            InitialCurrentNextMeetingAtTick = initialCurrentNextMeetingAtTick;
            InitialNextNextMeetingAtTick = initialNextNextMeetingAtTick;
            PartitionIds = partitionIds;
        }

        public int VertexId { get; }
        public int InitialCurrentNextMeetingAtTick { get; }
        public int InitialNextNextMeetingAtTick { get; }
        public IReadOnlyCollection<int> PartitionIds { get; }

        /// <summary>
        /// Gives the tick at which the meeting will be held.
        /// </summary>
        /// <param name="heldMeetings">The number of meetings that have been held so far on this meeting point.</param>
        /// <returns></returns>
        public int GetMeetingAtTick(int heldMeetings)
        {
            return InitialCurrentNextMeetingAtTick + (heldMeetings * InitialNextNextMeetingAtTick);
        }

        public bool Equals(MeetingPoint other)
        {
            return VertexId == other.VertexId && InitialNextNextMeetingAtTick == other.InitialNextNextMeetingAtTick && InitialCurrentNextMeetingAtTick == other.InitialCurrentNextMeetingAtTick && PartitionIds.SetEquals(other.PartitionIds);
        }

        public override bool Equals(object? obj)
        {
            return obj is MeetingPoint other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(VertexId, InitialNextNextMeetingAtTick, InitialCurrentNextMeetingAtTick, PartitionIds);
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