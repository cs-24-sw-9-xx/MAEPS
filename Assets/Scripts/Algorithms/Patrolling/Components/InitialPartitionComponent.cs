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
// Henrik van Peet,
// Mads Beyer Mogensen,
// Puvikaran Santhirasegaram

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Maes.Map.Generators.Patrolling.Partitioning;

namespace Maes.Algorithms.Patrolling.Components
{
    public sealed class InitialPartitionComponent : IComponent
    {
        public InitialPartitionComponent(StartupComponent<Dictionary<int, PartitionInfo>> startupComponent, VirtualStigmergyComponent<PartitionInfo> virtualStigmergyComponent)
        {
            _startupComponent = startupComponent;
            _virtualStigmergyComponent = virtualStigmergyComponent;
        }

        public int PreUpdateOrder => -9000;
        public int PostUpdateOrder => -9000;

        private readonly StartupComponent<Dictionary<int, PartitionInfo>> _startupComponent;
        private readonly VirtualStigmergyComponent<PartitionInfo> _virtualStigmergyComponent;

        [SuppressMessage("ReSharper", "IteratorNeverReturns")]
        public IEnumerable<ComponentWaitForCondition> PreUpdateLogic()
        {
            foreach (var (robotId, partitionInfo) in _startupComponent.Message)
            {
                _virtualStigmergyComponent.Put(robotId.ToString(), partitionInfo);
            }

            while (true)
            {
                yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: true);
            }
        }
    }
}