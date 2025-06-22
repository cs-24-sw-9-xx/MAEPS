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

using System.Collections.Generic;

using Maes.Utilities;

using MathNet.Numerics.LinearAlgebra;

using UnityEngine;

namespace Maes.Map.Generators.Patrolling.Partitioning
{
    /// <summary>
    /// This was algorithm was implemented based on part of a masters thesis,
    /// "Graph Partitioning Using Spectral Methods" by Pavla Kabelikova - page 16.
    /// </summary>
    public static class SpectralBisectionPartitioningGenerator
    {
        public static IEnumerable<List<Vector2Int>> Generator(
            Dictionary<(Vector2Int, Vector2Int), int> distanceMatrix,
            List<Vector2Int> vertexPositions,
            int amountOfPartitions
        )
        {
            var priorityQueue = new PriorityQueue<List<Vector2Int>, int>(new IntDescendingComparer());
            priorityQueue.Enqueue(vertexPositions, amountOfPartitions);

            while (priorityQueue.Count < amountOfPartitions)
            {
                var biggestCluster = priorityQueue.Dequeue();
                var (cluster1, cluster2) = Bisection(distanceMatrix, biggestCluster);

                priorityQueue.Enqueue(cluster1, cluster1.Count);
                priorityQueue.Enqueue(cluster2, cluster2.Count);
            }

            while (priorityQueue.TryDequeue(out var cluster, out _))
            {
                yield return cluster;
            }
        }

        private sealed class IntDescendingComparer : IComparer<int>
        {
            public int Compare(int x, int y)
            {
                return y.CompareTo(x);
            }
        }

        /// <summary>
        /// Generate partitions using the spectral bisection algorithm.
        /// </summary>
        /// <param name="distanceMatrix"></param>
        /// <param name="vertexPositions"></param>
        /// <param name="amountOfPartitions"></param>
        /// <returns></returns>
        public static IEnumerable<List<Vector2Int>> GeneratorOld(
            Dictionary<(Vector2Int, Vector2Int), int> distanceMatrix,
            List<Vector2Int> vertexPositions,
            int amountOfPartitions)
        {
            var startTime = Time.realtimeSinceStartup;

            if (amountOfPartitions == 1)
            {
                return new HashSet<List<Vector2Int>>() {
                    { vertexPositions }
                };
            }

            var (initialCluster1, initialCluster2) = Bisection(distanceMatrix, vertexPositions);

            var finalClusters = new Dictionary<int, List<Vector2Int>>();
            var queue = new Queue<(List<Vector2Int>, int)>();

            queue.Enqueue((initialCluster1, 0));
            queue.Enqueue((initialCluster2, 1));

            var clusterIndex = 2;

            while (queue.Count > 0 && finalClusters.Count + queue.Count < amountOfPartitions)
            {
                var (currentCluster, index) = queue.Dequeue();

                if (currentCluster.Count <= 1)
                {
                    finalClusters[index] = currentCluster;
                    continue;
                }

                var (subPartition1, subPartition2) = Bisection(distanceMatrix, currentCluster);

                foreach (var sub in new[] { subPartition1, subPartition2 })
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
            Debug.LogFormat("Partitioning took {0} seconds", Time.realtimeSinceStartup - startTime);

            return finalClusters.Values;
        }


        // Bisection algorithm to partition the graph
        private static (List<Vector2Int> Cluster1, List<Vector2Int> Cluster2) Bisection(Dictionary<(Vector2Int, Vector2Int), int> distanceMatrix, List<Vector2Int> vertexPositions)
        {
            var amountOfVertices = vertexPositions.Count;
            var adjacencyMatrix = PartitioningGenerator.AdjacencyMatrix(distanceMatrix, vertexPositions, amountOfVertices);
            var degreeMatrix = PartitioningGenerator.DegreeMatrix(amountOfVertices, adjacencyMatrix);

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

            return (cluster1, cluster2);
        }
    }
}