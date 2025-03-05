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

using Maes.Map.MapGen;
using Maes.Map.MapPatrollingGen;
using Maes.Utilities;

using MathNet.Numerics.LinearAlgebra;

using UnityEngine;

namespace Maes.Map.Partitioning
{
    public static class PartitioningGen
    {
        /// <summary>
        /// Generates a patrolling map from the given simulation map using spectral bisection partitioning.
        /// </summary>
        /// <param name="simulationMap">The simulation map to process and partition.</param>
        /// <param name="colorIslands">If true, applies distinct coloring to island regions when connecting vertices.</param>
        /// <param name="amountOfPartitions">The number of clusters to create using the spectral bisection algorithm.</param>
        /// <returns>
        /// A <see cref="PatrollingMap"/> containing connected vertices with partition assignments based on spectral clustering.
        /// </returns>
        public static PatrollingMap MakePatrollingMapWithSpectralBisectionPartitions(SimulationMap<Tile> simulationMap, bool colorIslands, int amountOfPartitions)
        {
            using var map = MapUtilities.MapToBitMap(simulationMap);
            var vertexPositionsDictionary = GreedyWaypointGenerator.TSPHeuresticSolver(map);
            var vertexPositions = vertexPositionsDictionary.Select(kv => kv.Key).ToList();
            var distanceMatrix = MapUtilities.CalculateDistanceMatrix(map, vertexPositions);
            var clusters = SpectralBisectionPartitions.Generator(distanceMatrix, vertexPositions, amountOfPartitions);
            var allVertices = new List<Vertex>();
            var nextId = 0;
            foreach (var cluster in clusters)
            {
                var localDistanceMatrix = MapUtilities.CalculateDistanceMatrix(map, cluster.Value);
                var vertices = WaypointConnection.ConnectVertices(cluster.Value
                    .ToDictionary(p => p, p => vertexPositionsDictionary[p]), localDistanceMatrix, colorIslands, nextId);
                Array.ForEach(vertices, vertex => vertex.Partition = cluster.Key);
                allVertices.AddRange(vertices);
                nextId = vertices.Select(v => v.Id).Max() + 1;
            }
            return new PatrollingMap(allVertices.ToArray(), simulationMap, vertexPositionsDictionary);
        }

        public static PatrollingMap MakePatrollingMapWithKMeansPartitions(SimulationMap<Tile> simulationMap, bool colorIslands, int amountOfPartitions, bool useOptimizedLOS = true)
        {
            using var map = MapUtilities.MapToBitMap(simulationMap);
            var vertexPositionsDictionary = GreedyWaypointGenerator.TSPHeuresticSolver(map);
            var vertexPositions = vertexPositionsDictionary.Select(kv => kv.Key).ToList();
            var distanceMatrix = MapUtilities.CalculateDistanceMatrix(map, vertexPositions);
            var clusters = KMeansPartitions.Generator(distanceMatrix, vertexPositions, amountOfPartitions);
            var allVertices = new List<Vertex>();
            var nextId = 0;
            foreach (var cluster in clusters)
            {
                var localDistanceMatrix = MapUtilities.CalculateDistanceMatrix(map, cluster.Value);
                var vertices = WaypointConnection.ConnectVertices(cluster.Value.ToDictionary(p => p, p => vertexPositionsDictionary[p]), localDistanceMatrix, colorIslands, nextId);
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

        /// <summary>
        /// Constructs a diagonal degree matrix from the given adjacency matrix, where each diagonal element is the sum of the corresponding row.
        /// </summary>
        /// <param name="amountOfVertices">The total number of vertices, which defines the dimensions of the matrix.</param>
        /// <param name="adjacencyMatrix">A square matrix representing edge weights between vertices.</param>
        /// <returns>
        /// A diagonal matrix of the same dimension, with each diagonal entry equal to the sum of the corresponding row in the adjacency matrix.
        /// </returns>
        public static Matrix<double> DegreeMatrix(int amountOfVertices, Matrix<double> adjacencyMatrix)
        {
            var degreeMatrix = Matrix<double>.Build.DenseDiagonal(amountOfVertices, 0.0);
            for (var i = 0; i < amountOfVertices; i++)
            {
                degreeMatrix[i, i] = adjacencyMatrix.Row(i).Sum();
            }

            return degreeMatrix;
        }

        /// <summary>
        /// Computes the standard deviation of the distance values contained in the provided dictionary.
        /// </summary>
        /// <param name="distanceMatrix">A dictionary mapping pairs of vertex positions to their respective distance values.</param>
        /// <returns>The computed standard deviation of the distance values.</returns>
        private static double StandardDeviation(Dictionary<(Vector2Int, Vector2Int), int> distanceMatrix)
        {
            var distances = distanceMatrix.Values.ToList();
            return Math.Sqrt(distances.Average(d => Math.Pow(d - distances.Average(), 2)));
        }

        /// <summary>
        /// Computes the Gaussian kernel weight for a given distance using the specified sigma.
        /// </summary>
        /// <param name="distance">The distance value between points.</param>
        /// <param name="sigma">The standard deviation parameter used in the Gaussian calculation.</param>
        /// <returns>The computed weight based on the Gaussian function.</returns>
        private static double GaussianKernel(double distance, double sigma)
        {
            return Math.Exp(-Math.Pow(distance, 2) / (2 * Math.Pow(sigma, 2)));
        }
    }
}