// Copyright 2025 MAES
// 
// This file is part of MAES
// 
// MAES is free software: you can redistribute it and/or modify it under
// the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your option)
// any later version.
// 
// MAES is distributed in the hope that it will be useful, but WITHOUT
// ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
// or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General
// Public License for more details.
// 
// You should have received a copy of the GNU General Public License along
// with MAES. If not, see http://www.gnu.org/licenses/.
// 
// Contributors: 
// Casper Nyvang Sørensen,
// Christian Ziegler Sejersen,
// Jakob Meyer Olsen,
// 
// Original repository: https://github.com/Molitany/MAES

using System.Linq;

using Maes.Algorithms.Patrolling.Components;
using Maes.Algorithms.Patrolling.Components.Redistribution;
using Maes.Map;
using Maes.Robot;

namespace Maes.Algorithms.Patrolling.PartitionedAlgorithms
{
    public sealed class PartitionedConscientiousReactive : PatrollingAlgorithm
    {
        public override string AlgorithmName => "Test Partitioning Algorithm";

        // Set by CreateComponents
        private GoToNextVertexComponent _goToNextVertexComponent = null!;
        private CollisionRecoveryComponent _collisionRecoveryComponent = null!;
        private PartitionRedistributionComponent _redistributionComponent = null!;

        protected override IComponent[] CreateComponents(IRobotController controller, PatrollingMap patrollingMap)
        {
            _goToNextVertexComponent = new GoToNextVertexComponent(NextVertex, this, controller, patrollingMap);
            _collisionRecoveryComponent = new CollisionRecoveryComponent(controller, _goToNextVertexComponent);
            _redistributionComponent = new PartitionRedistributionComponent(controller, patrollingMap);

            return new IComponent[] { _goToNextVertexComponent, _collisionRecoveryComponent, _redistributionComponent };
        }

        private static Vertex NextVertex(Vertex currentVertex)
        {
            return currentVertex.Neighbors.OrderBy(x => x.LastTimeVisitedTick).First();
        }
    }

}