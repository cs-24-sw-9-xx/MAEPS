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

using Accord.MachineLearning;

using Maes.Map.MapGen;
using Maes.Utilities;

using MathNet.Numerics.LinearAlgebra;

using UnityEngine;

using static Maes.Map.PatrollingMap;

namespace Maes.Map.MapPatrollingGen
{
    public static class PartitioningGen
    {
        public static PatrollingMap MakePatrollingMapWithPartitions(SimulationMap<Tile> simulationMap, bool colorIslands, int amountOfPartitions, bool useOptimizedLOS = true)
        {
            var map = MapUtilities.MapToBitMap(simulationMap);
            VisibilityMethod visibilityAlgorithm = useOptimizedLOS ? LineOfSightUtilities.ComputeVisibilityOfPointFastBreakColumn : LineOfSightUtilities.ComputeVisibilityOfPoint;
            var vertexPositions = GreedyWaypointGenerator.TSPHeuresticSolver(map, visibilityAlgorithm);
            var distanceMatrix = GreedyWaypointGenerator.CalculateDistanceMatrix(map, vertexPositions);
            var clusters = SpectralBisectionPartitions(distanceMatrix, vertexPositions, amountOfPartitions);
            var allVertices = new List<Vertex>();
            foreach (var cluster in clusters)
            {
                var localDistanceMatrix = GreedyWaypointGenerator.CalculateDistanceMatrix(map, cluster.Value);
                var vertices = GreedyWaypointGenerator.ConnectVertices(cluster.Value, localDistanceMatrix, colorIslands);
                allVertices.AddRange(vertices);
            }
            return new PatrollingMap(allVertices.ToArray(), simulationMap, visibilityAlgorithm);
        }

        /// <summary>
        /// Generate partitions using the k-means algorithm which is non-deterministic.
        /// </summary>
        /// <param name="distanceMatrix"></param>
        /// <param name="vertexPositions"></param>
        /// <param name="amountOfPartitions"></param>
        /// <returns></returns>
        private static Dictionary<int, List<Vector2Int>> KMeansPartitionsGenerator(Dictionary<(Vector2Int, Vector2Int), int> distanceMatrix, List<Vector2Int> vertexPositions, int amountOfPartitions)
        {
            var amountOfVertices = vertexPositions.Count;
            var adjacencyMatrix = Matrix<double>.Build.Dense(amountOfVertices, amountOfVertices);

            var sigma = StandardDeviation(distanceMatrix);

            // Construct the weighted adjacency matrix using Gaussian kernel
            for (var i = 0; i < amountOfVertices; i++)
            {
                for (var j = 0; j < amountOfVertices; j++)
                {
                    if (i != j)
                    {
                        var distance = distanceMatrix[(vertexPositions[i], vertexPositions[j])];
                        adjacencyMatrix[i, j] = GaussianKernel(distance, sigma);
                    }
                }
            }

            // Calculate the degree matrix
            var degreeMatrix = Matrix<double>.Build.DenseDiagonal(amountOfVertices, 0.0);
            for (var i = 0; i < amountOfVertices; i++)
            {
                degreeMatrix[i, i] = adjacencyMatrix.Row(i).Sum();
            }

            // Compute the Laplacian matrix
            var laplacianMatrix = degreeMatrix - adjacencyMatrix;

            // Compute eigenvalues and eigenvectors
            var eigen = laplacianMatrix.Evd(Symmetricity.Symmetric);
            var eigenVectors = eigen.EigenVectors;

            // Select the smallest non-trivial k eigenvectors (excluding the first)
            var featureMatrix = Matrix<double>.Build.Dense(amountOfVertices, amountOfPartitions);
            for (var i = 0; i < amountOfVertices; i++)
            {
                for (var j = 0; j < amountOfPartitions; j++)
                {
                    featureMatrix[i, j] = eigenVectors[i, j + 1];
                }
            }

            // Apply k-means clustering
            var kmeans = new KMeans(amountOfPartitions);
            var clusters = kmeans.Learn(featureMatrix.ToRowArrays());
            var labels = clusters.Decide(featureMatrix.ToRowArrays());

            // Initialize the cluster dictionary
            var clusterDictionary = new Dictionary<int, List<Vector2Int>>();
            for (var i = 0; i < amountOfPartitions; i++)
            {
                clusterDictionary[i] = new List<Vector2Int>();
            }

            // Assign vertices to clusters
            for (var i = 0; i < amountOfVertices; i++)
            {
                clusterDictionary[labels[i]].Add(vertexPositions[i]);
            }

            return clusterDictionary;
        }

        private static double StandardDeviation(Dictionary<(Vector2Int, Vector2Int), int> distanceMatrix)
        {
            var distances = distanceMatrix.Values.ToList();
            return Math.Sqrt(distances.Average(d => Math.Pow(d - distances.Average(), 2)));
        }

        private static double GaussianKernel(double distance, double sigma)
        {
            return Math.Exp(-Math.Pow(distance, 2) / (2 * Math.Pow(sigma, 2)));
        }

        /// <summary>
        /// Generate partitions using the spectral bisection algorithm.
        /// </summary>
        /// <param name="distanceMatrix"></param>
        /// <param name="vertexPositions"></param>
        /// <param name="amountOfPartitions"></param>
        /// <returns></returns>
        private static Dictionary<int, List<Vector2Int>> SpectralBisectionPartitions(
            Dictionary<(Vector2Int, Vector2Int), int> distanceMatrix,
            List<Vector2Int> vertexPositions,
            int amountOfPartitions)
        {
            var initialClusters = Bisection(distanceMatrix, vertexPositions);

            if (amountOfPartitions == 2 || initialClusters[0].Count == 0 || initialClusters[1].Count == 0)
            {
                return new Dictionary<int, List<Vector2Int>> {
                    { 0, initialClusters[0] },
                    { 1, initialClusters[1] }
                };
            }

            var finalClusters = new Dictionary<int, List<Vector2Int>>();
            var queue = new Queue<(List<Vector2Int>, int)>();

            queue.Enqueue((initialClusters[0], 0));
            queue.Enqueue((initialClusters[1], 1));

            var clusterIndex = 2;

            while (queue.Count > 0 && finalClusters.Count + queue.Count < amountOfPartitions)
            {
                var (currentCluster, index) = queue.Dequeue();

                if (currentCluster.Count <= 1)
                {
                    finalClusters[index] = currentCluster;
                    continue;
                }

                var subPartition = Bisection(distanceMatrix, currentCluster);

                foreach (var sub in subPartition)
                {
                    if (finalClusters.Count < amountOfPartitions)
                    {
                        queue.Enqueue((sub, clusterIndex++));
                    }
                    else
                    {
                        finalClusters[index] = sub;
                    }
                }
            }

            while (queue.Count > 0)
            {
                var (remainingCluster, index) = queue.Dequeue();
                finalClusters[index] = remainingCluster;
            }

            return finalClusters;
        }



        private static List<List<Vector2Int>> Bisection(Dictionary<(Vector2Int, Vector2Int), int> distanceMatrix, List<Vector2Int> vertexPositions)
        {
            var amountOfVertices = vertexPositions.Count;
            var adjacencyMatrix = Matrix<double>.Build.Dense(amountOfVertices, amountOfVertices);
            var sigma = StandardDeviation(distanceMatrix);

            for (var i = 0; i < amountOfVertices; i++)
            {
                for (var j = 0; j < amountOfVertices; j++)
                {
                    if (i != j)
                    {
                        var distance = distanceMatrix[(vertexPositions[i], vertexPositions[j])];
                        adjacencyMatrix[i, j] = GaussianKernel(distance, sigma);
                    }
                }
            }

            var degreeMatrix = Matrix<double>.Build.DenseDiagonal(amountOfVertices, 0.0);
            for (var i = 0; i < amountOfVertices; i++)
            {
                degreeMatrix[i, i] = adjacencyMatrix.Row(i).Sum();
            }

            var laplacianMatrix = degreeMatrix - adjacencyMatrix;

            var eigen = laplacianMatrix.Evd(Symmetricity.Symmetric);
            var eigenVectors = eigen.EigenVectors;

            var fiedlerVector = eigenVectors.Column(1);

            var cluster1 = new List<Vector2Int>();
            var cluster2 = new List<Vector2Int>();

            for (var i = 0; i < amountOfVertices; i++)
            {
                if (fiedlerVector[i] < 0)
                {
                    cluster1.Add(vertexPositions[i]);
                }
                else
                {
                    cluster2.Add(vertexPositions[i]);
                }
            }

            return new List<List<Vector2Int>> { cluster1, cluster2 };
        }
    }
}