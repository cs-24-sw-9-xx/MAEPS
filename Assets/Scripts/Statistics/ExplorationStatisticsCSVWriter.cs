// Copyright 2024 MAES
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
// Contributors: Rasmus Borrisholt Schmidt, Andreas Sebastian Sørensen, Thor Beregaard, Malte Z. Andreasen, Philip I. Holler and Magnus K. Jensen,
// 
// Original repository: https://github.com/Molitany/MAES

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

using Maes.Simulation;

using UnityEngine;

using static Maes.Statistics.ExplorationTracker;

namespace Maes.Statistics
{
    internal class ExplorationStatisticsCSVWriter
    {
        private readonly List<SnapShot<float>> _coverSnapShots;
        private readonly List<SnapShot<float>> _exploreSnapShots;
        private readonly List<SnapShot<float>> _distanceSnapShots;
        private readonly Dictionary<int, SnapShot<bool>> _allAgentsConnectedSnapShots;
        private readonly Dictionary<int, SnapShot<float>> _biggestClusterPercentageSnapShots;
        private readonly string _path;

        public ExplorationStatisticsCSVWriter(ExplorationSimulation explorationSimulation, string fileNameWithoutExtension)
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

            _coverSnapShots = explorationSimulation.ExplorationTracker._coverSnapshots;
            _exploreSnapShots = explorationSimulation.ExplorationTracker._exploreSnapshots;
            _distanceSnapShots = explorationSimulation.ExplorationTracker._distanceSnapshots;
            _allAgentsConnectedSnapShots = explorationSimulation.CommunicationManager.CommunicationTracker.InterconnectionSnapShot;
            _biggestClusterPercentageSnapShots = explorationSimulation.CommunicationManager.CommunicationTracker.BiggestClusterPercentageSnapshots;

            var resultForFileName = "e??-c??";
            if (_exploreSnapShots.Any())
            {
                resultForFileName = $"e{(int)_exploreSnapShots[^1].Value}-c{(int)_coverSnapShots[^1].Value}";
            }

            _path = GlobalSettings.StatisticsOutPutPath + fileNameWithoutExtension + "_" + resultForFileName + ".csv";
        }
        public void CreateCsvFile(string separator)
        {
            using var csv = new StreamWriter(Path.GetFullPath(_path));
            csv.WriteLine("Tick,Covered,Explored,Average Agent Distance,Agents Interconnected, Biggest Cluster %");
            for (var i = 0; i < _coverSnapShots.Count; i++)
            {
                var tick = _coverSnapShots[i].Tick;
                var coverage = "" + _coverSnapShots[i].Value;
                var explore = "" + _exploreSnapShots[i].Value;
                var distance = "" + _distanceSnapShots[i].Value;
                var line = new StringBuilder();
                line.Append(
                    $"{"" + tick}{separator}{coverage}{separator}{explore}{separator}{distance}{separator}");
                if (_allAgentsConnectedSnapShots.TryGetValue(tick, out var agentsConnectedSnapShot))
                {
                    var allAgentsInterconnectedString = agentsConnectedSnapShot.Value ? "" + 1 : "" + 0;
                    line.Append($"{allAgentsInterconnectedString}");
                }
                line.Append($"{separator}");
                if (_biggestClusterPercentageSnapShots.TryGetValue(tick, out var biggestClusterPercentageSnapShot))
                {
                    line.Append($"{biggestClusterPercentageSnapShot.Value}");
                }

                csv.WriteLine(line.ToString());
            }
            Debug.Log($"Writing statistics to path: {_path}");
        }
    }
}