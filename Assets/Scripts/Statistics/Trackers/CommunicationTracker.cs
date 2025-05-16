// Copyright 2022 MAES
// 
// This file is part of MAES
// 
// MAES is free software: you can redistribute it and/or modify it under
// the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your option)
// any later version.
// 
// MAES is distributed in the hope that it will be useful, but WITHOUT
// ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
// or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General
// Public License for more details.
// 
// You should have received a copy of the GNU General Public License along
// with MAES. If not, see http://www.gnu.org/licenses/.
// 
// Contributors: Malte Z. Andreasen, Philip I. Holler and Magnus K. Jensen
// 
// Original repository: https://github.com/MalteZA/MAES

using System.Collections.Generic;
using System.Linq;

using Maes.Statistics.Snapshots;

namespace Maes.Statistics.Trackers
{
    public sealed class CommunicationTracker
    {
        public CommunicationSnapshot LatestSnapshot { get; private set; }

        public void CreateSnapshot(int tick, int receivedMessageCount, int sentMessageCount, HashSet<HashSet<int>> communicationGroups)
        {
            if (tick == 0)
            {
                return;
            }

            var interconnected = CalculateInterconnected(communicationGroups);
            var biggestClusterSizePercentage = CalculateBiggestClusterSizePercentage(communicationGroups);

            LatestSnapshot = new CommunicationSnapshot(tick, interconnected, biggestClusterSizePercentage, receivedMessageCount, sentMessageCount);
        }

        private float CalculateBiggestClusterSizePercentage(HashSet<HashSet<int>> communicationGroups)
        {
            if (communicationGroups.Count == 0)
            {
                return 0f;
            }

            // if we have exactly one group, then every agent must be in it!
            if (communicationGroups.Count == 1)
            {
                return 100.0f;
            }
            else
            {
                var totalRobots = communicationGroups.Sum(g => g.Count);
                var biggestCluster = communicationGroups.Select(g => g.Count).Max();
                return (float)biggestCluster / (float)totalRobots * 100f;
            }
        }

        private bool CalculateInterconnected(HashSet<HashSet<int>> communicationGroups)
        {
            return communicationGroups.Count == 1;
        }
    }
}