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

using Accord.MachineLearning;

using MathNet.Numerics.LinearAlgebra;

using UnityEngine;

namespace Maes.Map.Generators.Patrolling.Partitioning
{
    public static class KMeansPartitioningGenerator
    {
        /// <summary>
        /// Generate partitions using the k-means algorithm which is non-deterministic.
        /// </summary>
        /// <param name="distanceMatrix"></param>
        /// <param name="vertexPositions"></param>
        /// <param name="amountOfPartitions"></param>
        /// <returns></returns>
        public static Dictionary<int, List<Vector2Int>> Generator(Dictionary<(Vector2Int, Vector2Int), int> distanceMatrix, List<Vector2Int> vertexPositions, int amountOfPartitions)
        {
            var amountOfVertices = vertexPositions.Count;
            var adjacencyMatrix = PartitioningGenerator.AdjacencyMatrix(distanceMatrix, vertexPositions, amountOfVertices);
            var degreeMatrix = PartitioningGenerator.DegreeMatrix(amountOfVertices, adjacencyMatrix);

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
    }
}