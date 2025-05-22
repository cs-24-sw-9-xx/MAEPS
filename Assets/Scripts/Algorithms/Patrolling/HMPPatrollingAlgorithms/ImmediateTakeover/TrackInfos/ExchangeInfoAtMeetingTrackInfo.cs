using Maes.Algorithms.Patrolling;

namespace Maes.Assets.Scripts.Algorithms.Patrolling.HMPPatrollingAlgorithms.ImmediateTakeover.TrackInfos
{
    public class ExchangeInfoAtMeetingTrackInfo : ITrackInfo
    {
        public ExchangeInfoAtMeetingTrackInfo(MeetingComponent.Meeting meeting, int exchangeAtTick, int robotId)
        {
            Meeting = meeting;
            ExchangeAtTick = exchangeAtTick;
            RobotId = robotId;
        }

        public MeetingComponent.Meeting Meeting { get; }
        public int ExchangeAtTick { get; }
        public int RobotId { get; }
    }
}