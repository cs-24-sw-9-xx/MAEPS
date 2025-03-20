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

using System;
using System.Collections.Generic;
using System.Linq;

using Maes.Map.Generators.Patrolling.Waypoints.Connectors;
using Maes.Map.Generators.Patrolling.Waypoints.Generators;
using Maes.Utilities;

using MathNet.Numerics.LinearAlgebra;

using UnityEngine;

using Random = UnityEngine.Random;

namespace Maes.Map.Generators.Patrolling.Partitioning
{
    public static class PartitioningGenerator
    {
        public static PatrollingMap MakePatrollingMapWithSpectralBisectionPartitions(SimulationMap<Tile> simulationMap, bool colorIslands, int amountOfPartitions)
        {
            using var map = MapUtilities.MapToBitMap(simulationMap);
            var vertexPositionsDictionary = GreedyMostVisibilityWaypointGenerator.VertexPositionsFromMap(map);
            var vertexPositions = vertexPositionsDictionary.Select(kv => kv.Key).ToList();
            var distanceMatrix = MapUtilities.CalculateDistanceMatrix(map, vertexPositions);
            var clusters = SpectralBisectionPartitioningGenerator.Generator(distanceMatrix, vertexPositions, amountOfPartitions);
            var allVertices = new List<Vertex>();
            var nextId = 0;
            foreach (var cluster in clusters)
            {
                var clusterColor = Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.5f, 1f);
                var vertices = ReverseNearestNeighborWaypointConnector.ConnectVertices(map, cluster.Value, colorIslands, clusterColor, nextId);
                Array.ForEach(vertices, vertex => vertex.Partition = cluster.Key);
                allVertices.AddRange(vertices);
                nextId = vertices.Select(v => v.Id).Max() + 1;
            }
            return new PatrollingMap(allVertices.ToArray(), simulationMap, vertexPositionsDictionary);
        }

        public static PatrollingMap MakePatrollingMapWithKMeansPartitions(SimulationMap<Tile> simulationMap, bool colorIslands, int amountOfPartitions, bool useOptimizedLOS = true)
        {
            using var map = MapUtilities.MapToBitMap(simulationMap);
            var vertexPositionsDictionary = GreedyMostVisibilityWaypointGenerator.VertexPositionsFromMap(map);
            var vertexPositions = vertexPositionsDictionary.Select(kv => kv.Key).ToList();
            var distanceMatrix = MapUtilities.CalculateDistanceMatrix(map, vertexPositions);
            var clusters = KMeansPartitioningGenerator.Generator(distanceMatrix, vertexPositions, amountOfPartitions);
            var allVertices = new List<Vertex>();
            var nextId = 0;
            foreach (var cluster in clusters)
            {
                var clusterColor = Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.5f, 1f);
                var vertices = ReverseNearestNeighborWaypointConnector.ConnectVertices(map, cluster.Value, colorIslands, clusterColor, nextId);
                allVertices.AddRange(vertices);
                nextId = vertices.Select(v => v.Id).Max() + 1;
            }
            return new PatrollingMap(allVertices.ToArray(), simulationMap, vertexPositionsDictionary);
        }


        // Construct the weighted adjacency matrix using Gaussian kernel
        public static Matrix<double> AdjacencyMatrix(Dictionary<(Vector2Int, Vector2Int), int> distanceMatrix, List<Vector2Int> vertexPositions, int amountOfVertices)
        {
            var adjacencyMatrix = Matrix<double>.Build.Dense(amountOfVertices, amountOfVertices);
            var sd = StandardDeviation(distanceMatrix);

            for (var i = 0; i < amountOfVertices; i++)
            {
                for (var j = 0; j < amountOfVertices; j++)
                {
                    if (i != j)
                    {
                        var distance = distanceMatrix[(vertexPositions[i], vertexPositions[j])];
                        adjacencyMatrix[i, j] = GaussianKernel(distance, sd);
                    }
                }
            }
            return adjacencyMatrix;
        }

        public static Matrix<double> DegreeMatrix(int amountOfVertices, Matrix<double> adjacencyMatrix)
        {
            var degreeMatrix = Matrix<double>.Build.DenseDiagonal(amountOfVertices, 0.0);
            for (var i = 0; i < amountOfVertices; i++)
            {
                degreeMatrix[i, i] = adjacencyMatrix.Row(i).Sum();
            }

            return degreeMatrix;
        }

        private static double StandardDeviation(Dictionary<(Vector2Int, Vector2Int), int> distanceMatrix)
        {
            var distances = distanceMatrix.Values.ToList();
            var meanDistance = distances.Average();
            return Math.Sqrt(distances.Average(d => Math.Pow(d - meanDistance, 2)));
        }

        private static double GaussianKernel(double distance, double sigma)
        {
            return Math.Exp(-Math.Pow(distance, 2) / (2 * Math.Pow(sigma, 2)));
        }
    }
}