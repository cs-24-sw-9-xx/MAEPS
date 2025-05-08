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
using System;
using System.Collections.Generic;
using System.Linq;

using Maes.Map;
using Maes.Robot;

namespace Maes.Algorithms.Patrolling.Components.Redistribution
{
    public sealed class RandomRedistributionComponent : IComponent
    {
        private readonly IRobotController _controller;
        private readonly IReadOnlyList<int> _partitionIds;
        private readonly int _delay;
        private readonly Random _random;
        private readonly IReadOnlyDictionary<(int, int), float> _probabilityForPartitionSwitch;

        public int PreUpdateOrder => -450;

        public int PostUpdateOrder => -450;

        public RandomRedistributionComponent(IRobotController controller, IReadOnlyList<Vertex> vertices, int delay = 1)
        {
            _controller = controller;
            _partitionIds = vertices.Select(v => v.Partition).Distinct().ToList();
            _delay = delay;
            _random = new Random();
        }

        public IEnumerable<ComponentWaitForCondition> PreUpdateLogic()
        {
            while (true)
            {
                SwitchPartition();
                yield return ComponentWaitForCondition.WaitForLogicTicks(_delay, shouldContinue: true);
            }
        }

        private void SwitchPartition()
        {
            var amountOfPartitions = _partitionIds.Count;
            if (amountOfPartitions < 2)
            {
                return;
            }

            var randomPartitionId = _partitionIds[_random.Next(0, amountOfPartitions)];
            _controller.AssignedPartition = randomPartitionId;
        }
    }
}