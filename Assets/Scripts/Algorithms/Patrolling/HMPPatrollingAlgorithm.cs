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

using Maes.Algorithms.Patrolling.Components;
using Maes.Map;
using Maes.Map.Generators.Patrolling.Partitioning;
using Maes.Robot;

namespace Maes.Algorithms.Patrolling
{
    /// <summary>
    /// Partition-based patrolling algorithm with the use of meeting points in a limited communication range.
    /// Proposed by Henrik, Mads, and Puvikaran. 
    /// </summary>
    public class HMPPatrollingAlgorithm : PatrollingAlgorithm
    {
        public HMPPatrollingAlgorithm(BasePartitionGenerator partitionGenerator)
        {
            _partitionGenerator = partitionGenerator;
        }
        public override string AlgorithmName => "HMPAlgorithm";

        private readonly BasePartitionGenerator _partitionGenerator;

        private readonly VirtualStigmergyComponent<PartitionInfo>.OnConflictDelegate _onConflict = (_, local, _) => local;
        private VirtualStigmergyComponent<PartitionInfo> _virtualStigmergyComponent = null!;
        private StartupComponent<Dictionary<int, PartitionInfo>> _startupComponent = null!;
        private InitialPartitionComponent _initialPartitionComponent = null!;

        protected override IComponent[] CreateComponents(Robot2DController controller, PatrollingMap patrollingMap)
        {
            _partitionGenerator.SetMaps(patrollingMap, controller.SlamMap.CollisionMap);
            _startupComponent = new StartupComponent<Dictionary<int, PartitionInfo>>(controller, _partitionGenerator.GeneratePartitions);
            _virtualStigmergyComponent = new VirtualStigmergyComponent<PartitionInfo>(_onConflict, controller, controller.GetRobot());
            _initialPartitionComponent = new InitialPartitionComponent(_startupComponent, _virtualStigmergyComponent);

            return new IComponent[] { _startupComponent, _virtualStigmergyComponent, _initialPartitionComponent };
        }
    }
}