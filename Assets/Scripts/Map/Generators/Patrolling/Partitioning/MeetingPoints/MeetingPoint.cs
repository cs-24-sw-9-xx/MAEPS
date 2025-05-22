using System;
using System.Collections.Generic;

using Maes.Utilities;

namespace Maes.Map.Generators.Patrolling.Partitioning.MeetingPoints
{
    public class MeetingPoint : IEquatable<MeetingPoint>, ICloneable<MeetingPoint>
    {
        public MeetingPoint(int vertexId, IReadOnlyCollection<int> robotIds, int globalMeetingCycle, int globalMeetingInterval)
        {
            VertexId = vertexId;
            GlobalMeetingCycle = globalMeetingCycle;
            GlobalMeetingInterval = globalMeetingInterval;

            _meetingAtTicks = new Queue<int>();
            _robotIds = new HashSet<int>(robotIds);
        }

        private MeetingPoint(int vertexId, IReadOnlyCollection<int> robotIds, int globalMeetingCycle, Queue<int> meetingAtTicks, int globalMeetingInterval)
        {
            VertexId = vertexId;
            GlobalMeetingCycle = globalMeetingCycle;
            GlobalMeetingInterval = globalMeetingInterval;
            _meetingAtTicks = new Queue<int>(meetingAtTicks);
            _robotIds = new HashSet<int>(robotIds);
        }

        public int VertexId { get; }
        private int GlobalMeetingCycle { get; }
        private int GlobalMeetingInterval { get; }
        private readonly Queue<int> _meetingAtTicks;
        private readonly HashSet<int> _robotIds;
        public IReadOnlyCollection<int> RobotIds => _robotIds;
        public IReadOnlyCollection<int> MeetingAtTicks => _meetingAtTicks;

        public void AttendMeeting(int tick)
        {
            while (_meetingAtTicks.TryPeek(out var result) && result <= tick)
            {
                _meetingAtTicks.Dequeue();
            }
        }

        public int? NextMeetingTime()
        {
            return _meetingAtTicks.TryPeek(out var nextMeeting) ? nextMeeting : null;
        }

        public void AddMeetingTime(int tick)
        {
            _meetingAtTicks.Enqueue(tick);
        }

        public bool Equals(MeetingPoint other)
        {
            return VertexId == other.VertexId && _robotIds.SetEquals(other._robotIds) && _meetingAtTicks.SetEquals(other._meetingAtTicks);
        }

        public override bool Equals(object? obj)
        {
            return obj is MeetingPoint other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(VertexId, RobotIds, MeetingAtTicks);
        }

        public static bool operator ==(MeetingPoint left, MeetingPoint right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MeetingPoint left, MeetingPoint right)
        {
            return !left.Equals(right);
        }

        public MeetingPoint Clone()
        {
            var meetingPoint = new MeetingPoint(VertexId, _robotIds, GlobalMeetingCycle, _meetingAtTicks, GlobalMeetingInterval);
            return meetingPoint;
        }

        public bool IsRobotParticipating(IReadOnlyCollection<int> meetingPointRobotIds)
        {
            return _robotIds.Overlaps(meetingPointRobotIds);
        }
    }
}