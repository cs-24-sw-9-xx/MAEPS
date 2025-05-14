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
        private readonly Dictionary<int, int> _receivedMessageCountSnapshots;
        private readonly Dictionary<int, int> _sentMessageCountSnapshots;

        protected CommunicationCsvDataWriter(CommunicationManager communicationManager, List<TSnapShot> snapShots, string filename)
            : base(snapShots, filename)
        {
            _allAgentsConnectedSnapShots = communicationManager.CommunicationTracker.InterconnectionSnapShot;
            _biggestClusterPercentageSnapShots = communicationManager.CommunicationTracker.BiggestClusterPercentageSnapshots;
            _receivedMessageCountSnapshots = communicationManager.CommunicationTracker.ReceivedMessageCountSnapshots;
            _sentMessageCountSnapshots = communicationManager.CommunicationTracker.SentMessageCountSnapshots;
        }

        protected override void PrepareSnapShot(TSnapShot snapShot)
        {
            snapShot.AgentsInterconnected = _allAgentsConnectedSnapShots.GetValueOrNull(snapShot.Tick);
            snapShot.BiggestClusterPercentage = _biggestClusterPercentageSnapShots.GetValueOrNull(snapShot.Tick);
            snapShot.ReceivedMessageCount = _receivedMessageCountSnapshots.GetValueOrNull(snapShot.Tick);
            snapShot.SentMessageCount = _sentMessageCountSnapshots.GetValueOrNull(snapShot.Tick);
        }
    }
}