using System;
using System.Collections.Generic;

using Accord.Math;

namespace Maes.Map.Generators.Patrolling.Partitioning.MeetingPoints
{
    public readonly struct MeetingPoint : IEquatable<MeetingPoint>
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

        public bool Equals(MeetingPoint other)
        {
            return VertexId == other.VertexId && AtTicks == other.AtTicks && RobotIds.SetEquals(other.RobotIds);
        }

        public override bool Equals(object? obj)
        {
            return obj is MeetingPoint other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(VertexId, AtTicks, RobotIds);
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