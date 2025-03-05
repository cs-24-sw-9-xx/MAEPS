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
        public override string AlgorithmName => "Heuristic Conscientious Reactive Algorithm";

        // Set by CreateComponents
        private GoToNextVertexComponent _goToNextVertexComponent = null!;

        protected override IComponent[] CreateComponents(Robot2DController controller, PatrollingMap patrollingMap)
        {
            _goToNextVertexComponent = new GoToNextVertexComponent(NextVertex, this, controller, patrollingMap);

            return new IComponent[] { _goToNextVertexComponent };
        }

        public delegate float DistanceEstimator(Vector2Int position);

        private Vertex NextVertex(Vertex currentVertex)
        {
            Debug.Log($"Current vertex {currentVertex.Id}");
            // Calculate the normalized idleness of the neighbors
            var normalizedIdleness = CalculateNormalizedIdleness(currentVertex.Neighbors);

            // Calculate the normalized distance estimation of the neighbors
            var normalizedDistances = CalculateNormalizedDistance(currentVertex.Neighbors, DistanceEstimatorMethod);
            var result = CalculateNextVertex(normalizedIdleness, normalizedDistances);
            Debug.Log($"Next vertex {result.First().Id}, with neighbours: {string.Join(", ", result.First().Neighbors.Select(x => x.Id))}");
            return result.First();
        }

        private float DistanceEstimatorMethod(Vector2Int position)
        {
            return _controller.EstimateDistanceToTarget(position) ?? throw new Exception("Distance estimation must not be null. Check if the target is reachable.");
        }

        private IEnumerable<Vertex> CalculateNextVertex(IEnumerable<NormalizedValue> normalizedIdleness, IEnumerable<NormalizedValue> normalizedDistances)
        {
            if (normalizedIdleness.Count() != normalizedDistances.Count())
            {
                throw new Exception("Length of normalizedIdleness and distanceEstimation must be equal");
            }
#if DEBUG
            var stringBuilder = new StringBuilder();
            stringBuilder = stringBuilder.AppendLine("Normalized Idleness");
            foreach (var item in normalizedIdleness.OrderBy(x => x.Vertex.Id))
            {
                stringBuilder = stringBuilder.AppendLine(item.Vertex.Id + " " + item.Value);
            }
            stringBuilder = stringBuilder.AppendLine("Normalized Distance");
            foreach (var item in normalizedDistances.OrderBy(x => x.Vertex.Id))
            {
                stringBuilder = stringBuilder.AppendLine(item.Vertex.Id + " " + item.Value);
            }
            Debug.Log(stringBuilder.ToString());
#endif
            var result = from idleness in normalizedIdleness
                         join distance in normalizedDistances on idleness.Vertex.Id equals distance.Vertex.Id
                         orderby idleness.Value + distance.Value ascending
                         select idleness.Vertex;
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

        private static IEnumerable<NormalizedValue> CalculateNormalizedDistance(IEnumerable<Vertex> verticies, DistanceEstimator distanceEstimator)
        {
            var distanceEstimations = verticies.Select(x => (Vertex: x, dist: distanceEstimator(x.Position)));
            if (distanceEstimations.Any(x => x.dist < 0))
            {
                throw new Exception("Distance estimation must be positive");
            }
            var distances = distanceEstimations.Select(x => (x.Vertex, dist: x.dist));
            var minDistance = distances.Min(x => x.dist);
            var maxDistance = distances.Max(x => x.dist);
            var normalizedDistance = new List<NormalizedValue>();
            foreach (var (vertex, distanceValue) in distances)
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