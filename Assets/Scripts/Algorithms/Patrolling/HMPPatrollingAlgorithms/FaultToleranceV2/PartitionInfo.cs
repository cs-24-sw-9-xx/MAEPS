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

using System.Collections.Generic;

using Maes.Algorithms.Patrolling.HMPPatrollingAlgorithms.FaultToleranceV2.MeetingPoints;
using Maes.Utilities;

namespace Maes.Algorithms.Patrolling.HMPPatrollingAlgorithms.FaultToleranceV2
{
    public readonly struct UnfinishedPartitionInfo
    {
        public int RobotId { get; }

        public HashSet<int> VertexIds { get; }

        public UnfinishedPartitionInfo(int robotId, HashSet<int> vertexIds)
        {
            RobotId = robotId;
            VertexIds = vertexIds;
        }
    }

    public readonly struct UnfinishedPartitionInfoWithDiameter
    {
        public int RobotId { get; }

        public HashSet<int> VertexIds { get; }

        public int Diameter { get; }

        public UnfinishedPartitionInfoWithDiameter(int robotId, HashSet<int> vertexIds, int diameter)
        {
            RobotId = robotId;
            VertexIds = vertexIds;
            Diameter = diameter;
        }
    }

    public sealed class PartitionInfo : ICloneable<PartitionInfo>
    {
        public PartitionInfo(int robotId, IReadOnlyCollection<int> vertexIds, IReadOnlyList<MeetingPoint> meetingPoints, int diameter)
        {
            RobotId = robotId;
            VertexIds = vertexIds;
            MeetingPoints = meetingPoints;
            Diameter = diameter;
        }

        public int RobotId { get; }
        public IReadOnlyCollection<int> VertexIds { get; }
        public IReadOnlyList<MeetingPoint> MeetingPoints { get; }

        public int Diameter { get; }

        public PartitionInfo Clone()
        {
            // This class is immutable, therefore we can just return this.
            return this;
        }
    }
}