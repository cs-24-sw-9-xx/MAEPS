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
// Contributors: Mads Beyer Mogensen

using System.Collections.Generic;

using Maes.Robot.Task;

namespace Maes.Algorithms.Patrolling.Components
{
    /// <summary>
    /// A component that can be used in <see cref="PatrollingAlgorithm"/> to add functionality.
    /// </summary>
    public interface IComponent
    {
        IEnumerable<ComponentWaitForCondition> PreUpdateLogic();

        IEnumerable<ComponentWaitForCondition> PostUpdateLogic();

        /// <summary>
        /// The order the component's <see cref="PreUpdateLogic"/> method is executed in comparison to the other components.
        /// </summary>
        int PreUpdateOrder { get; }

        /// <summary>
        /// The order the component's <see cref="PostUpdateLogic"/> method is executed in comparison to the other components.
        /// </summary>
        int PostUpdateOrder { get; }
    }

    public readonly struct ComponentWaitForCondition
    {
        public WaitForCondition Condition { get; }

        public bool ShouldContinue { get; }

        // Do not use this!
        public ComponentWaitForCondition(WaitForCondition waitForCondition, bool shouldContinue)
        {
            Condition = waitForCondition;
            ShouldContinue = shouldContinue;
        }

        public static ComponentWaitForCondition WaitForLogicTicks(int logicTicks, bool shouldContinue)
        {
            return new ComponentWaitForCondition(WaitForCondition.WaitForLogicTicks(logicTicks), shouldContinue);
        }

        public static ComponentWaitForCondition WaitForRobotStatus(RobotStatus status, bool shouldContinue)
        {
            return new ComponentWaitForCondition(WaitForCondition.WaitForRobotStatus(status), shouldContinue);
        }
    }
}