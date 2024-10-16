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
using UnityEngine;
using static Maes.Statistics.ExplorationTracker;

namespace Maes.Statistics
{
    internal class StatisticsCSVWriter
    {
        private Simulation _simulation { get; }
        private List<SnapShot<float>> _coverSnapShots { get; }
        private List<SnapShot<float>> _exploreSnapShots { get; }
        private List<SnapShot<float>> _distanceSnapShots { get; }
        private Dictionary<int, SnapShot<bool>> _allAgentsConnectedSnapShots { get; }
        private Dictionary<int, SnapShot<float>> _biggestClusterPercentageSnapShots { get; }
        private string _path { get; }


        public StatisticsCSVWriter(Simulation simulation, string fileNameWithoutExtension)
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

            _coverSnapShots = simulation.ExplorationTracker._coverSnapshots;
            _exploreSnapShots = simulation.ExplorationTracker._exploreSnapshots;
            _distanceSnapShots = simulation.ExplorationTracker._distanceSnapshots;
            _allAgentsConnectedSnapShots = simulation._communicationManager.CommunicationTracker.InterconnectionSnapShot;
            _biggestClusterPercentageSnapShots = simulation._communicationManager.CommunicationTracker.BiggestClusterPercentageSnapshots;

            _simulation = simulation;
            var resultForFileName = "e??-c??";
            if (_exploreSnapShots.Any())
                resultForFileName = $"e{(int)_exploreSnapShots[^1].Value}-c{(int)_coverSnapShots[^1].Value}";
            _path = GlobalSettings.StatisticsOutPutPath + fileNameWithoutExtension + "_" + resultForFileName + ".csv";
        }
        public void CreateCSVFile(string separator)
        {
            using var csv = new StreamWriter(Path.GetFullPath(_path));
            csv.WriteLine("Tick,Covered,Explored,Average Agent Distance,Agents Interconnected, Biggest Cluster %");
            for (var i = 0; i < _coverSnapShots.Count; i++)
            {
                var tick = _coverSnapShots[i].Tick;
                var coverage = "" + _coverSnapShots[i].Value;
                var explore = "" + _exploreSnapShots[i].Value;
                var distance = "" + _distanceSnapShots[i].Value;
                StringBuilder line = new StringBuilder();
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
