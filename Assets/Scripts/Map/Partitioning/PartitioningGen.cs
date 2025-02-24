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

using static Maes.Map.PatrollingMap;

namespace Maes.Map.Partitioning
{
    public static class PartitioningGen
    {
        public static PatrollingMap MakePatrollingMapWithSpectralBisectionPartitions(SimulationMap<Tile> simulationMap, bool colorIslands, int amountOfPartitions, bool useOptimizedLOS = true)
        {
            var map = MapUtilities.MapToBitMap(simulationMap);
            VisibilityMethod visibilityAlgorithm = useOptimizedLOS ? LineOfSightUtilities.ComputeVisibilityOfPointFastBreakColumn : LineOfSightUtilities.ComputeVisibilityOfPoint;
            var vertexPositions = GreedyWaypointGenerator.TSPHeuresticSolver(map, visibilityAlgorithm);
            var distanceMatrix = MapUtilities.CalculateDistanceMatrix(map, vertexPositions);
            var clusters = SpectralBisectionPartitions.Generator(distanceMatrix, vertexPositions, amountOfPartitions);
            var allVertices = new List<Vertex>();
            foreach (var cluster in clusters)
            {
                var localDistanceMatrix = MapUtilities.CalculateDistanceMatrix(map, cluster.Value);
                var vertices = WaypointConnection.ConnectVertices(cluster.Value, localDistanceMatrix, colorIslands);
                allVertices.AddRange(vertices);
            }
            return new PatrollingMap(allVertices.ToArray(), simulationMap, visibilityAlgorithm);
        }

        public static PatrollingMap MakePatrollingMapWithKMeansPartitions(SimulationMap<Tile> simulationMap, bool colorIslands, int amountOfPartitions, bool useOptimizedLOS = true)
        {
            var map = MapUtilities.MapToBitMap(simulationMap);
            VisibilityMethod visibilityAlgorithm = useOptimizedLOS ? LineOfSightUtilities.ComputeVisibilityOfPointFastBreakColumn : LineOfSightUtilities.ComputeVisibilityOfPoint;
            var vertexPositions = GreedyWaypointGenerator.TSPHeuresticSolver(map, visibilityAlgorithm);
            var distanceMatrix = MapUtilities.CalculateDistanceMatrix(map, vertexPositions);
            var clusters = KMeansPartitions.Generator(distanceMatrix, vertexPositions, amountOfPartitions);
            var allVertices = new List<Vertex>();
            foreach (var cluster in clusters)
            {
                var localDistanceMatrix = MapUtilities.CalculateDistanceMatrix(map, cluster.Value);
                var vertices = WaypointConnection.ConnectVertices(cluster.Value, localDistanceMatrix, colorIslands);
                allVertices.AddRange(vertices);
            }
            return new PatrollingMap(allVertices.ToArray(), simulationMap, visibilityAlgorithm);
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

        public static double StandardDeviation(Dictionary<(Vector2Int, Vector2Int), int> distanceMatrix)
        {
            var distances = distanceMatrix.Values.ToList();
            return Math.Sqrt(distances.Average(d => Math.Pow(d - distances.Average(), 2)));
        }

        public static double GaussianKernel(double distance, double sigma)
        {
            return Math.Exp(-Math.Pow(distance, 2) / (2 * Math.Pow(sigma, 2)));
        }
    }
}