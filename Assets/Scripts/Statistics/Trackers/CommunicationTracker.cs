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

namespace Maes.Statistics.Trackers
{
    public sealed class CommunicationTracker
    {
        public readonly Dictionary<int, bool> InterconnectionSnapShot = new();
        public readonly Dictionary<int, float> BiggestClusterPercentageSnapshots = new();
        public readonly Dictionary<int, int> SentMessageCountSnapshots = new();
        public readonly Dictionary<int, int> ReceivedMessageCountSnapshots = new();

        public void CreateSnapshot(int tick, int receivedMessageCount, int sentMessageCount, HashSet<HashSet<int>> communicationGroups)
        {
            if (tick == 0)
            {
                return;
            }

            CreateInterconnectedSnapShot(tick, communicationGroups);
            CreateClusterSizeSnapShot(tick, communicationGroups);
            CreateMessageCountSnapshot(tick, receivedMessageCount, sentMessageCount);
        }

        private void CreateClusterSizeSnapShot(int tick, HashSet<HashSet<int>> communicationGroups)
        {
            if (communicationGroups.Count == 0)
            {
                return;
            }

            // if we have exactly one group, then every agent must be in it!
            if (communicationGroups.Count == 1)
            {
                BiggestClusterPercentageSnapshots[tick] = 100.0f;
            }
            else
            {
                var totalRobots = communicationGroups.Sum(g => g.Count);
                var biggestCluster = communicationGroups.Select(g => g.Count).Max();
                var percentage = (float)biggestCluster / (float)totalRobots * 100f;
                BiggestClusterPercentageSnapshots[tick] = percentage;
            }
        }

        private void CreateInterconnectedSnapShot(int tick, HashSet<HashSet<int>> communicationGroups)
        {
            if (communicationGroups.Count == 1)
            {
                InterconnectionSnapShot[tick] = true;
            }
            else
            {
                InterconnectionSnapShot[tick] = false;
            }
        }

        private void CreateMessageCountSnapshot(int tick, int receivedMessageCount, int sentMessageCount)
        {
            ReceivedMessageCountSnapshots[tick] = receivedMessageCount;
            SentMessageCountSnapshots[tick] = sentMessageCount;
        }
    }
}