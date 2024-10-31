using System.Collections.Generic;
using Maes.Robot;

namespace Maes.Trackers
{
    public class PatrollingTracker : ITracker
    {
        private RobotConstraints _constraints;
        
        public PatrollingTracker(RobotConstraints constraints) {
            _constraints = constraints;
        }
        
        public void LogicUpdate(IReadOnlyList<MonaRobot> robots)
        {
            if (_constraints.AutomaticallyUpdateSlam) {
                // Always update estimated robot position and rotation
                // regardless of whether the slam map was updated this tick
                foreach (var robot in robots) {
                    var slamMap = robot.Controller.SlamMap;
                    slamMap.UpdateApproxPosition(robot.transform.position);
                    slamMap.SetApproxRobotAngle(robot.Controller.GetForwardAngleRelativeToXAxis());
                }
            }
        }

        public void SetVisualizedRobot(MonaRobot robot)
        {
            // TODO: Implement
        }
    }
}