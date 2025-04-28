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
// Contributors: Puvikaran Santhirasegaram

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

using Maes.Robot;
using Maes.Robot.Tasks;

using UnityEngine;

namespace Maes.Algorithms.Patrolling.Components
{
    public interface ICollisionRecoveryComponent : IComponent
    {
        public bool DoingCollisionRecovery { get; }
    }

    public class CollisionRecoveryComponent : ICollisionRecoveryComponent
    {
        // How close the robot has to get to a point before it has arrived.
        private const float MinDistance = 0.25f;

        private readonly IRobotController _controller;
        private readonly IMovementComponent _movementComponent;

        public bool DoingCollisionRecovery { get; private set; }

        public int PreUpdateOrder => -100;
        public int PostUpdateOrder => -100;

        public CollisionRecoveryComponent(IRobotController controller, IMovementComponent movementComponent)
        {
            _controller = controller;
            _movementComponent = movementComponent;
        }

        [SuppressMessage("ReSharper", "IteratorNeverReturns")]
        public IEnumerable<ComponentWaitForCondition> PreUpdateLogic()
        {
            while (true)
            {
                if (_controller.IsCurrentlyColliding)
                {
                    DoingCollisionRecovery = true;

                    _controller.StopCurrentTask();
                    yield return ComponentWaitForCondition.WaitForRobotStatus(RobotStatus.Idle, false);

                    _controller.Move(1.0f, reverse: true);
                    yield return ComponentWaitForCondition.WaitForRobotStatus(RobotStatus.Idle, false);

                    if (_controller.IsCurrentlyColliding)
                    {
                        _controller.Move(1.0f, reverse: false);
                        yield return ComponentWaitForCondition.WaitForRobotStatus(RobotStatus.Idle, false);
                    }

                    while (GetRelativePositionTo(_movementComponent.TargetPosition).Distance > MinDistance)
                    {
                        _controller.PathAndMoveTo(_movementComponent.TargetPosition, dependOnBrokenBehaviour: false);
                        yield return ComponentWaitForCondition.WaitForLogicTicks(1, false);
                    }
                }

                yield return ComponentWaitForCondition.WaitForLogicTicks(1, true);
                DoingCollisionRecovery = false;
            }
        }

        public void DebugInfo(StringBuilder stringBuilder)
        {
            if (DoingCollisionRecovery)
            {
                stringBuilder.Append("Doing collision avoidance\n");
            }
        }

        private RelativePosition GetRelativePositionTo(Vector2Int position)
        {
            return _controller.SlamMap.CoarseMap.GetTileCenterRelativePosition(position, dependOnBrokenBehaviour: false);
        }
    }
}