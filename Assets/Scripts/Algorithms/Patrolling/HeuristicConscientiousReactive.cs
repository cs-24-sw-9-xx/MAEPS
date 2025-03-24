// Copyright 2024 MAES
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
// Henrik van Peet,
// Mads Beyer Mogensen,
// Puvikaran Santhirasegaram
// 
// Original repository: https://github.com/Molitany/MAES

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Maes.Algorithms.Patrolling.Components;
using Maes.Map;
using Maes.Robot;

using UnityEngine;

namespace Maes.Algorithms.Patrolling
{
    /// <summary>
    /// Original implementation of the Heuristic Conscientious Reactive Algorithm of https://repositorio.ufpe.br/handle/123456789/2474.
    /// Pseudocode can be found in another paper: https://doi.org/10.1080/01691864.2013.763722
    /// Heuristic: The vertex with the lowest idleness and distance estimation
    /// </summary>
    public sealed class HeuristicConscientiousReactiveAlgorithm : PatrollingAlgorithm
    {
        public HeuristicConscientiousReactiveAlgorithm(int randomSeed = 0)
        {
            _random = new(randomSeed);
        }
        private readonly int _randomSeed;
        public override string AlgorithmName => "Heuristic Conscientious Reactive Algorithm";

        // Set by CreateComponents
        private GoToNextVertexComponent _goToNextVertexComponent = null!;
        private CollisionRecoveryComponent _collisionRecoveryComponent = null!;

        private readonly System.Random _random;

        protected override IComponent[] CreateComponents(Robot2DController controller, PatrollingMap patrollingMap)
        {
            _goToNextVertexComponent = new GoToNextVertexComponent(NextVertex, this, controller, patrollingMap);
            _collisionRecoveryComponent = new CollisionRecoveryComponent(controller, _goToNextVertexComponent);

            return new IComponent[] { _goToNextVertexComponent, _collisionRecoveryComponent };
        }

        public delegate float DistanceEstimator(Vertex source, Vertex target);

        private Vertex NextVertex(Vertex currentVertex)
        {
            Debug.Log($"Current vertex {currentVertex.Id}");
            // Calculate the normalized idleness of the neighbors
            var normalizedIdleness = CalculateNormalizedIdleness(currentVertex.Neighbors);

            // Calculate the normalized distance estimation of the neighbors
            var normalizedDistances = CalculateNormalizedDistance(currentVertex, ActualDistanceMethod);
            var bestVertices = UtilityFunction(normalizedIdleness, normalizedDistances);
            var minUtilityValue = bestVertices.Select(i => i.Value).Min();
            bestVertices = bestVertices.Where(i => i.Value == minUtilityValue);
            var nextVertex = bestVertices.ElementAt(_random.Next(bestVertices.Count())).Vertex;
            Debug.Log($"Next vertex {nextVertex.Id}, with neighbours: {string.Join(", ", nextVertex.Neighbors.Select(x => x.Id))}");
            return nextVertex;
        }

        /// <summary>
        /// Realistically, this method should be used, since the distance estimation is not always accurate.
        /// Currently, we use the actual path distance as the distance estimation. 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        private float DistanceEstimatorMethod(Vertex source, Vertex target)
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

        private IEnumerable<NormalizedValue> UtilityFunction(IEnumerable<NormalizedValue> normalizedIdleness, IEnumerable<NormalizedValue> normalizedDistances)
        {
            if (normalizedIdleness.Count() != normalizedDistances.Count())
            {
                throw new Exception("Length of normalizedIdleness and distanceEstimation must be equal");
            }
#if DEBUG
            var stringBuilder = new StringBuilder();
            stringBuilder = stringBuilder.AppendLine("Vertex Id | Normalized Idleness | Normalized Distance | Sum");
            foreach (var idleness in normalizedIdleness.OrderBy(x => x.Vertex.Id))
            {
                var normalizedDistance = normalizedDistances.Single(x => x.Vertex.Id == idleness.Vertex.Id);
                stringBuilder = stringBuilder.AppendLine(idleness.Vertex.Id + " | " + idleness.Value + " | " + normalizedDistance.Value + " | " + (idleness.Value + normalizedDistance.Value));
            }
            Debug.Log(stringBuilder.ToString());
#endif
            var result = from idleness in normalizedIdleness
                         join distance in normalizedDistances on idleness.Vertex.Id equals distance.Vertex.Id
                         select new NormalizedValue(idleness.Vertex, idleness.Value + distance.Value);
            return result;
        }

        private readonly struct NormalizedValue
        {
            public Vertex Vertex { get; }
            public float Value { get; }

            public NormalizedValue(Vertex vertex, float value)
            {
                Vertex = vertex;
                Value = value;
            }
        }

        private static IEnumerable<NormalizedValue> CalculateNormalizedIdleness(IEnumerable<Vertex> verticies)
        {
            var maxLastTick = verticies.Max(x => x.LastTimeVisitedTick);
            var minLastTick = verticies.Min(x => x.LastTimeVisitedTick);
            var idleness = verticies.Select(x => (x, Idleness: maxLastTick - x.LastTimeVisitedTick));
            var normalizedIdleness = new List<NormalizedValue>();
            foreach (var (vertex, idlenessValue) in idleness)
            {
                // Avoid division by zero
                if ((maxLastTick - minLastTick) == 0)
                {
                    normalizedIdleness.Add(new NormalizedValue(vertex, 0));
                }
                else
                {
                    normalizedIdleness.Add(new NormalizedValue(vertex, 1 - (float)idlenessValue / (maxLastTick - minLastTick)));
                }
            }
            return normalizedIdleness;
        }

        private static IEnumerable<NormalizedValue> CalculateNormalizedDistance(Vertex currentVertex, DistanceEstimator distanceEstimator)
        {
            var neighbours = currentVertex.Neighbors;
            var distanceEstimations = neighbours.Select(vertex => (Vertex: vertex, dist: distanceEstimator(currentVertex, vertex)));
            if (distanceEstimations.Any(estimation => estimation.dist < 0))
            {
                throw new Exception("Distance estimation must be positive");
            }
            var minDistance = distanceEstimations.Min(x => x.dist);
            var maxDistance = distanceEstimations.Max(x => x.dist);
            var normalizedDistance = new List<NormalizedValue>();
            foreach (var (vertex, distanceValue) in distanceEstimations)
            {
                // Avoid division by zero
                if (maxDistance - minDistance == 0)
                {
                    normalizedDistance.Add(new NormalizedValue(vertex, 0));
                }
                else
                {
                    normalizedDistance.Add(new NormalizedValue(vertex, (distanceValue - minDistance) / (maxDistance - minDistance)));
                }
            }
            return normalizedDistance;
        }
    }
}