namespace Maes.Algorithms.Patrolling.HMPPatrollingAlgorithms.FaultToleranceV2.TrackInfos
{
    public interface IMeetingTrackInfo : ITrackInfo
    {
        public MeetingComponent.Meeting Meeting { get; }
        public int ExchangeAtTick { get; }
        public int RobotId { get; }
    }
}