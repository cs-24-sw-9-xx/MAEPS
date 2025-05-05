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
using System.Linq;

using Maes.Algorithms.Patrolling.Components;
using Maes.Map;
using Maes.Robot;

using UnityEngine;


namespace Maes.Algorithms.Patrolling
{
    /// <summary>
    /// Currently only works when all vertices are connected to all other vertices.
    /// </summary>
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
                var startTime = Time.realtimeSinceStartup;
                _patrollingCycle = CreatePatrollingCycle(currentVertex);
                var elapsed = Time.realtimeSinceStartup - startTime;
                Debug.Log($"Patrolling cycle created in {elapsed} s. Cycle length: {_patrollingCycle.Count}");
                Debug.Log($"Patrolling cycle: {string.Join(", ", _patrollingCycle.Select(v => v.Id))}");
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

        protected float[,] EstimatedDistanceMatrix(IReadOnlyList<Vertex> vertices)
        {
            if (vertices.Max(v => v.Id) >= _patrollingMap.Vertices.Count)
            {
                throw new System.Exception($"Vertex ID {vertices.Max(v => v.Id)} is out of bounds for the patrolling map with {vertices.Count} vertices.");
            }

            var distanceMatrix = new float[vertices.Count, vertices.Count];
            foreach (var v1 in vertices)
            {
                foreach (var v2 in vertices)
                {
                    if (v1.Id == v2.Id)
                    {
                        distanceMatrix[v1.Id, v2.Id] = 0;
                    }
                    else
                    {
                        distanceMatrix[v1.Id, v2.Id] = _controller.TravelEstimator.EstimateDistance(v1.Position, v2.Position) ?? float.MaxValue;
                    }
                }
            }
            return distanceMatrix;
        }

        protected float PathLength(IReadOnlyList<Vertex> path, float[,] distanceMatrix)
        {
            var length = 0f;
            for (var i = 0; i < path.Count - 1; i++)
            {
                length += distanceMatrix[path[i].Id, path[i + 1].Id];
            }
            return length;
        }
    }
}