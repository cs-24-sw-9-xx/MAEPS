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

using Maes.Map.Generators.Patrolling.Partitioning.MeetingPoints;

namespace Maes.Map.Generators.Patrolling.Partitioning
{
    public class PartitionInfo : IEquatable<PartitionInfo>
    {
        public PartitionInfo(int robotId, HashSet<int> vertexIds)
        {
            RobotId = robotId;
            VertexIds = vertexIds;
        }

        public int RobotId { get; }
        public HashSet<int> VertexIds { get; }

        public bool Equals(PartitionInfo other)
        {
            return RobotId == other.RobotId &&
                   VertexIds.SetEquals(other.VertexIds);
        }
    }

    public class HMPPartitionInfo : PartitionInfo, IEquatable<HMPPartitionInfo>
    {
        public HMPPartitionInfo(PartitionInfo partitionInfo, List<MeetingPoint> meetingPoints) : base(partitionInfo.RobotId, partitionInfo.VertexIds)
        {
            MeetingPoints = meetingPoints;
        }

        public List<MeetingPoint> MeetingPoints { get; }

        public bool Equals(HMPPartitionInfo other)
        {
            return RobotId == other.RobotId &&
                   VertexIds.SetEquals(other.VertexIds);
        }
    }
}