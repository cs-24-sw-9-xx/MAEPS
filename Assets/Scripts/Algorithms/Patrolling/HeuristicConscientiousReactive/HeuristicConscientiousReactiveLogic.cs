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

using System;
using System.Collections.Generic;
using System.Linq;

using Maes.Map;

using UnityEngine;

using Random = System.Random;


namespace Maes.Algorithms.Patrolling.HeuristicConscientiousReactive
{
    public sealed class HeuristicConscientiousReactiveLogic
    {
        private readonly Random _random;
        private readonly DistanceEstimator _distanceEstimator;

        public HeuristicConscientiousReactiveLogic(DistanceEstimator distanceEstimator, int seed = 0)
        {
            _random = new Random(seed);
            _distanceEstimator = distanceEstimator;
        }

        public delegate float DistanceEstimator(Vertex source, Vertex target);

        public Vertex NextVertex(Vertex currentVertex, IReadOnlyCollection<Vertex> neighbors)
        {
            // If the current vertex has no neighbors, return it
            if (neighbors.Count == 0)
            {
                return currentVertex;
            }

            // Calculate the normalized idleness of the neighbors
            var normalizedIdleness = CalculateNormalizedIdleness(neighbors);

            // Calculate the normalized distance estimation of the neighbors
            var normalizedDistances = CalculateNormalizedDistance(currentVertex, neighbors, _distanceEstimator);

            var bestVertices = UtilityFunction(normalizedIdleness, normalizedDistances).ToArray();
            var minUtilityValue = bestVertices.Min(i => i.Value);
            bestVertices = bestVertices.Where(i => i.Value == minUtilityValue).ToArray();
            var nextVertex = bestVertices.ElementAt(_random.Next(bestVertices.Length)).Vertex;

            return nextVertex;
        }

        private static IEnumerable<NormalizedValue> UtilityFunction(List<NormalizedValue> normalizedIdleness, List<NormalizedValue> normalizedDistances)
        {
            Debug.Assert(normalizedIdleness.Count == normalizedDistances.Count);

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

        private static List<NormalizedValue> CalculateNormalizedIdleness(IReadOnlyCollection<Vertex> vertices)
        {
            var maxLastTick = vertices.Max(x => x.LastTimeVisitedTick);
            var minLastTick = vertices.Min(x => x.LastTimeVisitedTick);
            var normalizedIdleness = new List<NormalizedValue>(vertices.Count);
            foreach (var vertex in vertices)
            {
                // Avoid division by zero
                if ((maxLastTick - minLastTick) == 0)
                {
                    normalizedIdleness.Add(new NormalizedValue(vertex, 0));
                }
                else
                {
                    normalizedIdleness.Add(new NormalizedValue(vertex, 1 - ((float)(maxLastTick - vertex.LastTimeVisitedTick) / (maxLastTick - minLastTick))));
                }
            }
            return normalizedIdleness;
        }

        private static List<NormalizedValue> CalculateNormalizedDistance(Vertex currentVertex, IReadOnlyCollection<Vertex> neighbours, DistanceEstimator distanceEstimator)
        {
            var distanceEstimations = neighbours.Select(vertex => (Vertex: vertex, dist: distanceEstimator(currentVertex, vertex))).ToList();
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