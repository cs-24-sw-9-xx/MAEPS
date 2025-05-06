using System.Collections.Generic;

namespace Maes.Algorithms.Patrolling.Components
{
    public class MeetingObserverComponent : IComponent
    {
        public MeetingObserverComponent(int preUpdateOrder, int postUpdateOrder,
            ICollisionRecoveryComponent collisionRecoveryComponent, IMovementComponent component,
            MeetingComponent meetingComponent)
        {
            PreUpdateOrder = preUpdateOrder;
            PostUpdateOrder = postUpdateOrder;
            _movementComponent = component;
            _meetingComponent = meetingComponent;
            _collisionRecoveryComponent = collisionRecoveryComponent;
        }

        private readonly ICollisionRecoveryComponent _collisionRecoveryComponent;
        private readonly IMovementComponent _movementComponent;
        private readonly MeetingComponent _meetingComponent;

        public int PreUpdateOrder { get; }
        public int PostUpdateOrder { get; }

        public IEnumerable<ComponentWaitForCondition> PreUpdateLogic()
        {
            while (true)
            {
                if (_collisionRecoveryComponent.DoingCollisionRecovery)
                {
                    var shouldGoToMeeting = _meetingComponent.ShouldGoToNextMeeting(_movementComponent.ApproachingVertex);
                    if (shouldGoToMeeting != null && shouldGoToMeeting.Position != _movementComponent.TargetPosition)
                    {
                        _movementComponent.AbortCurrentTask(new AbortingTask(shouldGoToMeeting));
                    }
                }

                yield return ComponentWaitForCondition.WaitForLogicTicks(1, true);
            }
        }
    }
}