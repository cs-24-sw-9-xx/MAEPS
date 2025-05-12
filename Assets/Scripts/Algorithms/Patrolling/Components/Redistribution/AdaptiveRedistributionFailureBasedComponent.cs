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
using Maes.Map;
using Maes.Robot;

namespace Maes.Algorithms.Patrolling.Components.Redistribution
{
    /// <summary>
    /// Component responsible for redistributing robots to different partitions based on failure. If the robot receive communication from a partition, it will decrease the tracker.
    /// </summary>
    public sealed class AdaptiveRedistributionFailureBasedComponent : BaseRedistributionComponent
    {
        public AdaptiveRedistributionFailureBasedComponent(IRobotController controller, PatrollingMap map, IPatrollingAlgorithm algorithm, int seed = 123) : base(controller, map, algorithm, seed)
        {
        }

        protected override void UpdateTrackerOnFailure(int partitionId)
        {
            _redistributionTracker[partitionId] += _currentPartition.CommunicationRatio[partitionId];
        }

        protected override void UpdateTrackerOnSuccess(int partitionId)
        {
            _redistributionTracker[partitionId] -= _currentPartition.CommunicationRatio[partitionId];
        }
    }
}