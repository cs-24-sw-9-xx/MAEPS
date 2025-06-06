using System.Collections.Generic;

namespace Maes.Algorithms.Patrolling.HMPPatrollingAlgorithms.ImmediateTakeover.TrackInfos
{
    public class MissingRobotsAtMeetingTrackInfo : ITrackInfo
    {
        public MissingRobotsAtMeetingTrackInfo(MeetingComponent.Meeting meeting, HashSet<int> missingRobotIds, int reportedByRobotId)
        {
            Meeting = meeting;
            MissingRobotIds = missingRobotIds;
            ReportedByRobotId = reportedByRobotId;
        }

        public MeetingComponent.Meeting Meeting { get; }
        public HashSet<int> MissingRobotIds { get; }
        public int ReportedByRobotId { get; }
    }
}