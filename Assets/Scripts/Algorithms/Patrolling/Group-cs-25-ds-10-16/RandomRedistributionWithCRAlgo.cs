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

using System.Linq;

using Maes.Algorithms.Patrolling.Components;
using Maes.Algorithms.Patrolling.Components.Redistribution;
using Maes.Map;
using Maes.Robot;

namespace Maes.Algorithms.Patrolling
{
    /// <summary>
    /// Original implementation of the Conscientious Reactive Algorithm of https://doi.org/10.1007/3-540-36483-8_11.
    /// Pseudocode can be found in another paper: https://doi.org/10.1080/01691864.2013.763722
    /// </summary>
    public sealed class RandomRedistributionWithCRAlgo : PatrollingAlgorithm
    {
        public RandomRedistributionWithCRAlgo(int seed, int probabilityFactor)
        {
            _seed = seed;
            _probabilityFactor = probabilityFactor;
        }
        public override string AlgorithmName => "Random Redistribution Conscientious Reactive Algorithm";

        // Set by CreateComponents
        private GoToNextVertexComponent _goToNextVertexComponent = null!;
        private CollisionRecoveryComponent _collisionRecoveryComponent = null!;
        private RandomRedistributionComponent _redistributionComponent = null!;
        private readonly int _seed;
        private readonly int _probabilityFactor;

        protected override IComponent[] CreateComponents(IRobotController controller, PatrollingMap patrollingMap)
        {
            _goToNextVertexComponent = new GoToNextVertexComponent(NextVertex, this, controller, patrollingMap);
            _collisionRecoveryComponent = new CollisionRecoveryComponent(controller, _goToNextVertexComponent);
            _redistributionComponent = new RandomRedistributionComponent(controller, patrollingMap.Vertices, this, seed: _seed, probabilityFactor: _probabilityFactor);

            return new IComponent[] { _goToNextVertexComponent, _redistributionComponent, _collisionRecoveryComponent };
        }

        private static Vertex NextVertex(Vertex currentVertex)
        {
            return ConscientiousReactiveLogic.NextVertex(currentVertex);
        }
    }
}