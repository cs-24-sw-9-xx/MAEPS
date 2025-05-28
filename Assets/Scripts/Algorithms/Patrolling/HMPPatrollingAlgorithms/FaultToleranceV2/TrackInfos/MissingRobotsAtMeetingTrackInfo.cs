using System.Collections.Generic;

namespace Maes.Algorithms.Patrolling.HMPPatrollingAlgorithms.FaultToleranceV2.TrackInfos
{
    public class MissingRobotsAtMeetingTrackInfo : IMeetingTrackInfo
    {
        public MissingRobotsAtMeetingTrackInfo(MeetingComponent.Meeting meeting, int exchangeAtTick, int robotId,
            HashSet<int> missingRobotIds, int reportedByRobotId)
        {
            Meeting = meeting;
            MissingRobotIds = missingRobotIds;
            ReportedByRobotId = reportedByRobotId;
            ExchangeAtTick = exchangeAtTick;
            RobotId = robotId;
        }

        public MeetingComponent.Meeting Meeting { get; }
        public int ExchangeAtTick { get; }
        public int RobotId { get; }
        public HashSet<int> MissingRobotIds { get; }
        public int ReportedByRobotId { get; }
    }
}