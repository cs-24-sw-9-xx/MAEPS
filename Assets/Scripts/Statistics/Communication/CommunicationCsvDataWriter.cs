using System.Collections.Generic;

using Maes.Robot;
using Maes.Statistics.Writer;
using Maes.Utilities;

namespace Maes.Statistics.Communication
{
    public abstract class CommunicationCsvDataWriter<TSnapShot> : CsvDataWriter<TSnapShot>
    where TSnapShot : CommunicationSnapShot
    {
        private readonly Dictionary<int, bool> _allAgentsConnectedSnapShots;
        private readonly Dictionary<int, float> _biggestClusterPercentageSnapShots;

        protected CommunicationCsvDataWriter(CommunicationManager communicationManager, List<TSnapShot> snapShots, string filename) : base(snapShots, filename)
        {
            _allAgentsConnectedSnapShots = communicationManager.CommunicationTracker.InterconnectionSnapShot;
            _biggestClusterPercentageSnapShots = communicationManager.CommunicationTracker.BiggestClusterPercentageSnapshots;
        }

        protected override void PrepareSnapShot(TSnapShot snapShot)
        {
            snapShot.AgentsInterconnected = _allAgentsConnectedSnapShots.GetValueOrNull(snapShot.Tick);
            snapShot.BiggestClusterPercentage = _biggestClusterPercentageSnapShots.GetValueOrNull(snapShot.Tick);
        }
    }
}