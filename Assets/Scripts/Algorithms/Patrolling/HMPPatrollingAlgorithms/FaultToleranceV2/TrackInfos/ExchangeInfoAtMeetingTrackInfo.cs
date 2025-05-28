namespace Maes.Algorithms.Patrolling.HMPPatrollingAlgorithms.FaultToleranceV2.TrackInfos
{
    public class ExchangeInfoAtMeetingTrackInfo : IMeetingTrackInfo
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