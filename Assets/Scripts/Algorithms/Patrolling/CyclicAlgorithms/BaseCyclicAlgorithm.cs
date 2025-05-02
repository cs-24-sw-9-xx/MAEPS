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
using Maes.Robot;


namespace Maes.Algorithms.Patrolling
{
    public abstract class BaseCyclicAlgorithm : PatrollingAlgorithm
    {
        // Set by CreateComponents
        private GoToNextVertexComponent _goToNextVertexComponent = null!;
        private CollisionRecoveryComponent _collisionRecoveryComponent = null!;
        private List<Vertex> _patrollingCycle = new();

        protected override IComponent[] CreateComponents(IRobotController controller, PatrollingMap patrollingMap)
        {
            _goToNextVertexComponent = new GoToNextVertexComponent(NextVertex, this, controller, patrollingMap);
            _collisionRecoveryComponent = new CollisionRecoveryComponent(controller, _goToNextVertexComponent);

            return new IComponent[] { _goToNextVertexComponent, _collisionRecoveryComponent };
        }

        private Vertex NextVertex(Vertex currentVertex)
        {
            if (_patrollingCycle.Count == 0)
            {
                _patrollingCycle = CreatePatrollingCycle(currentVertex);
            }
            return NextVertexInCycle(currentVertex);
        }

        private Vertex NextVertexInCycle(Vertex currentVertex)
        {
            var currentIndex = _patrollingCycle.IndexOf(currentVertex);
            var nextIndex = (currentIndex + 1) % _patrollingCycle.Count;
            return _patrollingCycle[nextIndex];
        }

        protected abstract List<Vertex> CreatePatrollingCycle(Vertex startVertex);
    }
}