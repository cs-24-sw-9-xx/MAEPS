// Copyright 2025 MAEPS
// 
// This file is part of MAEPS
// 
// MAEPS is free software: you can redistribute it and/or modify it under
// the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your option)
// any later version.
// 
// MAEPS is distributed in the hope that it will be useful, but WITHOUT
// ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
// or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General
// Public License for more details.
// 
// You should have received a copy of the GNU General Public License along
// with MAEPS. If not, see http://www.gnu.org/licenses/.
//
// Contributors 2025: 
// Henrik van Peet,
// Mads Beyer Mogensen,
// Puvikaran Santhirasegaram

using System;
using System.Collections.Generic;
using System.Linq;

using Accord.Math;

using Maes.Map.Generators.Patrolling.Partitioning.MeetingPoints;
using Maes.Utilities;

namespace Maes.Map.Generators.Patrolling.Partitioning
{
    public class PartitionInfo : IEquatable<PartitionInfo>, ICloneable<PartitionInfo>
    {
        public PartitionInfo(int robotId, IReadOnlyCollection<int> vertexIds)
        {
            RobotId = robotId;
            VertexIds = vertexIds;
        }

        public int RobotId { get; }
        public IReadOnlyCollection<int> VertexIds { get; }

        public bool Equals(PartitionInfo other)
        {
            return RobotId == other.RobotId &&
                   VertexIds.SetEquals(other.VertexIds);
        }

        public override bool Equals(object? obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((PartitionInfo)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(RobotId, VertexIds);
        }

        public static bool operator ==(PartitionInfo? left, PartitionInfo? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(PartitionInfo? left, PartitionInfo? right)
        {
            return !Equals(left, right);
        }

        PartitionInfo ICloneable<PartitionInfo>.Clone()
        {
            // This class is immutable, therefore we can just return this.
            return this;
        }
    }

    public sealed class HMPPartitionInfo : PartitionInfo, IEquatable<HMPPartitionInfo>, ICloneable<HMPPartitionInfo>
    {
        public HMPPartitionInfo(PartitionInfo partitionInfo, IReadOnlyList<MeetingPoint> meetingPoints)
            : base(partitionInfo.RobotId, partitionInfo.VertexIds)
        {
            MeetingPoints = meetingPoints;
        }

        public IReadOnlyList<MeetingPoint> MeetingPoints { get; }

        public bool Equals(HMPPartitionInfo other)
        {
            return base.Equals(other) && MeetingPoints.SequenceEqual(other.MeetingPoints);
        }

        public override bool Equals(object? obj)
        {
            return ReferenceEquals(this, obj) || obj is HMPPartitionInfo other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(base.GetHashCode(), MeetingPoints);
        }

        public object Clone()
        {
            // This class is immutable, therefore we can just return this.
            return this;
        }

        HMPPartitionInfo ICloneable<HMPPartitionInfo>.Clone()
        {
            // This class is immutable, therefore we can just return this.
            return this;
        }

        public static bool operator ==(HMPPartitionInfo? left, HMPPartitionInfo? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(HMPPartitionInfo? left, HMPPartitionInfo? right)
        {
            return !Equals(left, right);
        }
    }
}