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

using Maes.Robot.Tasks;

namespace Maes.Algorithms
{
    public readonly struct WaitForCondition
    {
        public readonly ConditionType Type;

        // LogicTicks
        public readonly int LogicTicks;

        // ControllerState
        public readonly RobotStatus RobotStatus;

        private WaitForCondition(ConditionType type, int logicTicks, RobotStatus robotStatus)
        {
            Type = type;
            LogicTicks = logicTicks;
            RobotStatus = robotStatus;
        }

        public static WaitForCondition WaitForLogicTicks(int logicTicks)
        {
            return new WaitForCondition(ConditionType.LogicTicks, logicTicks, RobotStatus.Moving);
        }

        public static WaitForCondition WaitForRobotStatus(RobotStatus status)
        {
            return new WaitForCondition(ConditionType.RobotStatus, 0, status);
        }

        public static WaitForCondition ContinueUpdateLogic()
        {
            return new WaitForCondition(ConditionType.ContinueUpdateLogic, 0, RobotStatus.Moving);
        }


        public enum ConditionType
        {
            LogicTicks,
            RobotStatus,
            ContinueUpdateLogic,
        }
    }
}