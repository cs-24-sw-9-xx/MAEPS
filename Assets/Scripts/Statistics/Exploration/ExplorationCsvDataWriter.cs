using System.Collections.Generic;

using Maes.Simulation;
using Maes.Statistics.Writer;
using Maes.Utilities;

namespace Maes.Statistics.Exploration
{
    public class ExplorationCsvDataWriter : CsvDataWriter<ExplorationSnapShot>
    {
        private readonly Dictionary<int, bool> _allAgentsConnectedSnapShots;
        private readonly Dictionary<int, float> _biggestClusterPercentageSnapShots;

        public ExplorationCsvDataWriter(ExplorationSimulation simulation, string filename) : base(simulation.ExplorationTracker.snapShots, filename)
        {
            _allAgentsConnectedSnapShots = simulation.CommunicationManager.CommunicationTracker.InterconnectionSnapShot;
            _biggestClusterPercentageSnapShots = simulation.CommunicationManager.CommunicationTracker.BiggestClusterPercentageSnapshots;
        }

        protected override void PrepareSnapShot(ExplorationSnapShot snapShot)
        {
            snapShot.AgentsInterconnected = _allAgentsConnectedSnapShots.GetValueOrNull(snapShot.Tick);
            snapShot.BiggestClusterPercentage = _biggestClusterPercentageSnapShots.GetValueOrNull(snapShot.Tick);
        }
    }
}