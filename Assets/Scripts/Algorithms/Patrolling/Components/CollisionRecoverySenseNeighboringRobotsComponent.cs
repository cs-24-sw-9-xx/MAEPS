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
using System.Text;

using Maes.Robot;
using Maes.Robot.Tasks;

using UnityEngine;

namespace Maes.Algorithms.Patrolling.Components
{
    public sealed class CollisionRecoverySenseNeighboringRobotsComponent : IComponent
    {
        private const float RecoveryAngle = 45f; // All robots rotate by this angle
        private readonly IRobotController _controller;
        private readonly IMovementComponent _movementComponent;
        private bool _doingCollisionRecovery;

        public int PreUpdateOrder => -100;
        public int PostUpdateOrder => -100;

        public CollisionRecoverySenseNeighboringRobotsComponent(IRobotController controller, IMovementComponent movementComponent)
        {
            _controller = controller;
            _movementComponent = movementComponent;
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
                    var lowestId = GetLowestIdOfNearbyRobots();

                    if (lowestId != _controller.Id)
                    {
                        foreach (var condition in PerformRecoveryManeuver())
                        {
                            yield return condition;
                        }

                        while (lowestId != _controller.Id)
                        {
                            yield return ComponentWaitForCondition.WaitForLogicTicks(1, false);
                            lowestId = GetLowestIdOfNearbyRobots();
                        }

                        while (GetRelativePositionTo(_movementComponent.TargetPosition).Distance > 0.25f)
                        {
                            _controller.PathAndMoveTo(_movementComponent.TargetPosition, dependOnBrokenBehaviour: false);
                            yield return ComponentWaitForCondition.WaitForLogicTicks(1, false);
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
        /// <returns></returns>
        private int GetLowestIdOfNearbyRobots()
        {
            var nearbyRobots = _controller.SenseNearbyRobots();
            var lowestId = _controller.Id;
            foreach (var robot in nearbyRobots)
            {
                if (robot.RobotId < lowestId)
                {
                    lowestId = robot.RobotId;
                }
            }
            return lowestId;
        }

        private RelativePosition GetRelativePositionTo(Vector2Int position)
        {
            return _controller.SlamMap.CoarseMap.GetTileCenterRelativePosition(position, dependOnBrokenBehaviour: false);
        }
    }
}