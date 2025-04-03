// Copyright 2025 MAEPS
// 
// This file is part of MAEPS
// 
// MAEPS is free software: you can redistribute it and/or modify it under
// the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your option)
// any later version.
// 
// MAEPS is distributed in the hope that it will be useful, but WITHOUT
// ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
// or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General
// Public License for more details.
// 
// You should have received a copy of the GNU General Public License along
// with MAEPS. If not, see http://www.gnu.org/licenses/.
//
// Contributors 2025: 
// Casper Nyvang Sørensen,
// Christian Ziegler Sejersen,
// Jakob Meyer Olsen
// Mads Beyer Mogensen
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Maes.Robot;
using Maes.Robot.Tasks;

namespace Maes.Algorithms.Patrolling.Components
{
    public sealed class CollisionRecoverySenseNeighboringRobotsComponent : IComponent
    {
        private const float RecoveryAngle = 45f; // All robots rotate by this angle
        private readonly Robot2DController _controller;
        private bool _doingCollisionRecovery;

        public int PreUpdateOrder => -100;
        public int PostUpdateOrder => -100;

        public CollisionRecoverySenseNeighboringRobotsComponent(Robot2DController controller)
        {
            _controller = controller;
        }

        public IEnumerable<ComponentWaitForCondition> PreUpdateLogic()
        {
            while (true)
            {
                if (_controller.IsCurrentlyColliding)
                {
                    _doingCollisionRecovery = true;
                    _controller.StopCurrentTask();
                    yield return ComponentWaitForCondition.WaitForRobotStatus(RobotStatus.Idle, false);
                    var myId = _controller.GetRobot().id;
                    var lowestId = GetLowestIdOfNearbyRobots(myId);

                    if (lowestId == myId)
                    {
                        foreach (var condition in PerformRecoveryManeuver())
                        {
                            yield return condition;
                        }

                        while (lowestId == myId)
                        {
                            yield return ComponentWaitForCondition.WaitForLogicTicks(1, false);
                            lowestId = GetLowestIdOfNearbyRobots(myId);
                        }
                    }
                }

                yield return ComponentWaitForCondition.WaitForLogicTicks(1, true);
                _doingCollisionRecovery = false;
            }
        }

        public void DebugInfo(StringBuilder stringBuilder)
        {
            if (_doingCollisionRecovery)
            {
                stringBuilder.Append("Doing collision avoidance\n");
            }
        }

        /// <summary>
        /// Performs the recovery maneuver. The robot rotates by a certain angle and then moves backwards for a short distance.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<ComponentWaitForCondition> PerformRecoveryManeuver()
        {
            _controller.Rotate(RecoveryAngle);
            yield return ComponentWaitForCondition.WaitForRobotStatus(RobotStatus.Idle, false);
            _controller.Move(2.5f, reverse: true);
            yield return ComponentWaitForCondition.WaitForRobotStatus(RobotStatus.Idle, false);
        }

        /// <summary>
        /// Gets the lowest id of the robots in the robots' neighborhood. If no robots are in the neighborhood, -1 is returned.
        /// </summary>
        /// <param name="myId"></param>
        /// <returns></returns>
        private int GetLowestIdOfNearbyRobots(int myId)
        {
            var nearbyRobots = _controller.SenseNearbyRobots();
            return nearbyRobots.Length == 0 ? -1 : nearbyRobots.Select(r => r.RobotId).Min();
        }
    }
}