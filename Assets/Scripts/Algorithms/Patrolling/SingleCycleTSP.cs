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



using System.Linq;

using Maes.Algorithms.Patrolling.Components;
using Maes.Map;
using Maes.Robot;

using Random = System.Random;

namespace Maes.Algorithms.Patrolling
{
    /// <summary>
    /// Implementation of the Single Cycle algorithms using an approximation of TSP: https://doi.org/10.1007/978-3-540-28645-5_48.
    /// An implementation can be found here: https://github.com/matteoprata/DRONET-for-Patrolling/blob/main_july_2023/src/patrolling/tsp_cycle.py
    /// </summary>
    public sealed class SingleCycleTSP : PatrollingAlgorithm
    {
        private readonly Random _random;

        public SingleCycleTSP(int seed)
        {
            _random = new Random(seed);
        }

        public override string AlgorithmName => "SingleCycle Algorithm";

        // Set by CreateComponents
        private GoToNextVertexComponent _goToNextVertexComponent = null!;
        private CollisionRecoveryComponent _collisionRecoveryComponent = null!;

        protected override IComponent[] CreateComponents(IRobotController controller, PatrollingMap patrollingMap)
        {
            _goToNextVertexComponent = new GoToNextVertexComponent(NextVertex, this, controller, patrollingMap);
            _collisionRecoveryComponent = new CollisionRecoveryComponent(controller, _goToNextVertexComponent);

            return new IComponent[] { _goToNextVertexComponent, _collisionRecoveryComponent };
        }

        private Vertex NextVertex(Vertex currentVertex)
        {
            // Todo: make it use the TSP algorithm to find the next vertex
            // Use christofides algorithm to approximate the TSP
            var index = _random.Next(currentVertex.Neighbors.Count);
            return currentVertex.Neighbors.ElementAt(index);
        }
    }
}