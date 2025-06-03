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

using Maes.Utilities;

namespace Maes.Algorithms.Patrolling.HMPPatrollingAlgorithms.SingleMeetingPoint
{
    public sealed class Assignment : ICloneable<Assignment>
    {
        public Assignment(IReadOnlyCollection<int> vertexIds)
        {
            VertexIds = vertexIds;
        }

        public IReadOnlyCollection<int> VertexIds { get; }

        public Assignment Clone()
        {
            // This class is immutable, therefore we can just return this.
            return this;
        }
    }
}