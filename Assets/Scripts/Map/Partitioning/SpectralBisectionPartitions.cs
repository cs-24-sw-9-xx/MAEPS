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

using MathNet.Numerics.LinearAlgebra;

using UnityEngine;

namespace Maes.Map.Partitioning
{
    public static class SpectralBisectionPartitions
    {
        /// <summary>
        /// Generate partitions using the spectral bisection algorithm.
        /// </summary>
        /// <param name="distanceMatrix"></param>
        /// <param name="vertexPositions"></param>
        /// <param name="amountOfPartitions"></param>
        /// <returns></returns>
        public static Dictionary<int, List<Vector2Int>> Generator(
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


        // Bisection algorithm to partition the graph
        private static List<List<Vector2Int>> Bisection(Dictionary<(Vector2Int, Vector2Int), int> distanceMatrix, List<Vector2Int> vertexPositions)
        {
            var amountOfVertices = vertexPositions.Count;
            var adjacencyMatrix = PartitioningGen.AdjacencyMatrix(distanceMatrix, vertexPositions, amountOfVertices);
            var degreeMatrix = PartitioningGen.DegreeMatrix(amountOfVertices, adjacencyMatrix);

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