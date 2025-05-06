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
// Contributors: 
// Henrik van Peet,
// Mads Beyer Mogensen,
// Puvikaran Santhirasegaram

using System;
using System.Linq;

using Maes.Algorithms.Patrolling.Components;
using Maes.Map;
using Maes.Robot;

using UnityEngine;

namespace Maes.Algorithms.Patrolling.HeuristicConscientiousReactive
{
    public sealed class HeuristicConscientiousReactiveAlgorithm : PatrollingAlgorithm
    {
        public HeuristicConscientiousReactiveAlgorithm(int seed = 0)
        {
            _heuristicConscientiousReactiveLogic = new HeuristicConscientiousReactiveLogic(ActualDistanceMethod, seed);
        }
        public override string AlgorithmName => "Heuristic Conscientious Reactive Algorithm";
        public override Vertex TargetVertex => _goToNextVertexComponent.ApproachingVertex;

        private readonly HeuristicConscientiousReactiveLogic _heuristicConscientiousReactiveLogic;
        private IRobotController _controller = null!;

        // Set by CreateComponents
        private GoToNextVertexComponent _goToNextVertexComponent = null!;
        private CollisionRecoveryComponent _collisionRecoveryComponent = null!;

        protected override IComponent[] CreateComponents(IRobotController controller, PatrollingMap patrollingMap)
        {
            _controller = controller;
            _goToNextVertexComponent = new GoToNextVertexComponent(NextVertex, this, controller, patrollingMap);
            _collisionRecoveryComponent = new CollisionRecoveryComponent(controller, _goToNextVertexComponent);

            return new IComponent[] { _goToNextVertexComponent, _collisionRecoveryComponent };
        }

        private Vertex NextVertex(Vertex currentVertex)
        {
            return _heuristicConscientiousReactiveLogic.NextVertex(currentVertex, currentVertex.Neighbors);
        }

        /// <summary>
        /// Realistically, this method should be used, since the distance estimation is not always accurate.
        /// Currently, we use the actual path distance as the distance estimation. 
        /// </summary>
        /// <param name="_"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        private float DistanceEstimatorMethod(Vertex _, Vertex target)
        {
            return _controller.EstimateDistanceToTarget(target.Position) ?? throw new Exception($"Distance estimation must not be null. Check if the target is reachable. VertexId: {target.Id}, x:{target.Position.x}, y:{target.Position.y}");
        }

        private float ActualDistanceMethod(Vertex source, Vertex target)
        {
            if (_patrollingMap.Paths.TryGetValue((source.Id, target.Id), out var path))
            {
                return path.Sum(p => Vector2Int.Distance(p.Start, p.End));
            }
            throw new Exception($"Path from {source.Id} to {target.Id} not found");
        }
    }
}