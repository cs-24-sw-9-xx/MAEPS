using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using Maes.Map;
using Maes.Map.Generators.Patrolling.Partitioning;
using Maes.Map.Generators.Patrolling.Partitioning.MeetingPoints;
using Maes.Robot;

using UnityEngine;

namespace Maes.Algorithms.Patrolling.Components
{
    public class HMPPartitionComponent : PartitionComponent<HMPPartitionInfo>
    {
        public HMPPartitionComponent(IRobotController controller, IPartitionGenerator<HMPPartitionInfo> partitionGenerator) : base(controller, partitionGenerator)
        {
        }

        private VirtualStigmergyComponent<int, MeetingPoint, HMPPartitionComponent> _virtualStigmergyMeetingPointByVertexId = null!;
        public IReadOnlyList<MeetingPoint> MeetingPoints { get; private set; } = null!;

        private static VirtualStigmergyComponent<int, MeetingPoint, HMPPartitionComponent>.ValueInfo OnConflict(int key, VirtualStigmergyComponent<int, MeetingPoint, HMPPartitionComponent>.ValueInfo localvalueinfo, VirtualStigmergyComponent<int, MeetingPoint, HMPPartitionComponent>.ValueInfo incomingvalueinfo)
        {
            if (localvalueinfo.RobotId < incomingvalueinfo.RobotId)
            {
                return localvalueinfo;
            }

            return incomingvalueinfo;
        }

        public override IComponent[] CreateComponents(IRobotController controller, PatrollingMap patrollingMap)
        {
            var components = base.CreateComponents(controller, patrollingMap).ToList();

            _virtualStigmergyMeetingPointByVertexId =
                new VirtualStigmergyComponent<int, MeetingPoint, HMPPartitionComponent>(OnConflict, controller);
            components.Add(_virtualStigmergyMeetingPointByVertexId);

            return components.ToArray();
        }

        [SuppressMessage("ReSharper", "IteratorNeverReturns")]
        public override IEnumerable<ComponentWaitForCondition> PreUpdateLogic()
        {
            var meetingPointVertexIds = new HashSet<int>();
            MeetingPoints = new List<MeetingPoint>(_startupComponent.Message[_robotId].MeetingPoints.Select(x => x.Clone()));
            foreach (var meetingPoint in MeetingPoints)
            {
                meetingPointVertexIds.Add(meetingPoint.VertexId);
                _virtualStigmergyMeetingPointByVertexId.Put(meetingPoint.VertexId, meetingPoint);
            }

            foreach (var robotId in _startupComponent.DiscoveredRobots)
            {
                if (robotId == _robotId)
                {
                    continue;
                }

                foreach (var meetingPoint in _startupComponent.Message[robotId].MeetingPoints)
                {
                    if (meetingPointVertexIds.Contains(meetingPoint.VertexId))
                    {
                        continue;
                    }

                    _virtualStigmergyMeetingPointByVertexId.Put(meetingPoint.VertexId, meetingPoint.Clone());
                    meetingPointVertexIds.Add(meetingPoint.VertexId);
                }
            }

            using var baseEnumerator = base.PreUpdateLogic().GetEnumerator();
            while (baseEnumerator.MoveNext())
            {
                if (_virtualStigmergyMeetingPointByVertexId.NewUpdate)
                {
                    var meetingPoints = _virtualStigmergyMeetingPointByVertexId.GetAllWithoutSeeding();
                    var list = new List<MeetingPoint>();
                    foreach (var meetingPoint in meetingPoints)
                    {
                        if (meetingPoint.RobotIds.Contains(_robotId))
                        {
                            list.Add(meetingPoint);
                        }
                    }
                    MeetingPoints = list;
                }
                yield return baseEnumerator.Current;
            }
        }

        public void AttendMeeting(int meetingPointId, int atTick)
        {
            if (_virtualStigmergyMeetingPointByVertexId.TryGet(meetingPointId, out var meetingPoint))
            {
                meetingPoint.AttendMeeting(atTick);
                _virtualStigmergyMeetingPointByVertexId.Put(meetingPointId, meetingPoint);
            }
        }

        public IEnumerable<ComponentWaitForCondition> ExchangeInformation(IReadOnlyCollection<int> meetingPointRobotIds)
        {
            _virtualStigmergyComponent.SendAll();
            _virtualStigmergyMeetingPointByVertexId.SendAll();
            yield return ComponentWaitForCondition.WaitForLogicTicks(2, shouldContinue: false);


            var meetingPoints = _virtualStigmergyMeetingPointByVertexId.GetAllWithoutSeeding();
            var meetingPoints1 = meetingPoints.Where(m => m.IsRobotParticipating(meetingPointRobotIds)).ToList();


        }

        public IEnumerable<ComponentWaitForCondition> OnMissingRobotAtMeeting(MeetingComponent.Meeting meeting, HashSet<int> missingRobots)
        {
            // TODO: Implement the logic for when some other robots are not at the meeting point
            Debug.Log("Some robots are not at the meeting point");

            yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: false);
        }
    }
}