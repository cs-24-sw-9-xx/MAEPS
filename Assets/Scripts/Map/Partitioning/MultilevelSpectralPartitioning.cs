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
using System.Linq;


using UnityEngine;
namespace Maes.Map.Partitioning
{
    public static class MultilevelSpectralPartitioning
    {
        public static Dictionary<int, List<Vector2Int>> PartitionGraph(
            Dictionary<(Vector2Int, Vector2Int), int> distanceMatrix,
            List<Vector2Int> vertexPositions, int levels, int amountOfPartitions)
        {
            var hierarchy = new List<Dictionary<(Vector2Int, Vector2Int), int>>();
            var vertexHierarchy = new List<List<Vector2Int>>();
            var coarseToOriginalMap = new Dictionary<Vector2Int, List<Vector2Int>>();

            var coarseGraph = distanceMatrix;
            var coarseVertices = vertexPositions;

            for (int i = 0; i < levels; i++)
            {
                (coarseGraph, coarseVertices, coarseToOriginalMap) = CoarsenGraph(coarseGraph, coarseVertices);
                hierarchy.Add(coarseGraph);
                vertexHierarchy.Add(coarseVertices);
            }

            var partition = SpectralBisectionPartitions.Generator(coarseGraph, coarseVertices, amountOfPartitions);

            for (int i = levels - 1; i >= 0; i--)
            {
                partition = UncoarsenAndRefine(partition, coarseToOriginalMap);
            }

            return partition;
        }
        private static (Dictionary<(Vector2Int, Vector2Int), int>, List<Vector2Int>, Dictionary<Vector2Int, List<Vector2Int>>)
    CoarsenGraph(Dictionary<(Vector2Int, Vector2Int), int> graph, List<Vector2Int> vertices)
        {
            var newGraph = new Dictionary<(Vector2Int, Vector2Int), int>();
            var newVertices = new List<Vector2Int>();
            var coarseToOriginal = new Dictionary<Vector2Int, List<Vector2Int>>();
            var matched = new HashSet<Vector2Int>();
            var vertexToCoarse = new Dictionary<Vector2Int, Vector2Int>();

            var random = new System.Random();
            var shuffledVertices = vertices.OrderBy(v => random.Next()).ToList();

            foreach (var v in shuffledVertices)
            {
                if (!matched.Contains(v))
                {
                    var closest = vertices
                        .Where(u => u != v && !matched.Contains(u))
                        .OrderBy(u => graph[(v, u)])
                        .FirstOrDefault();

                    if (closest != default)
                    {
                        var newVertex = new Vector2Int((v.x + closest.x) / 2, (v.y + closest.y) / 2);
                        newVertices.Add(newVertex);
                        coarseToOriginal[newVertex] = new List<Vector2Int> { v, closest };
                        matched.Add(v);
                        matched.Add(closest);
                        vertexToCoarse[v] = newVertex;
                        vertexToCoarse[closest] = newVertex;
                    }
                    else
                    {
                        newVertices.Add(v);
                        coarseToOriginal[v] = new List<Vector2Int> { v };
                        vertexToCoarse[v] = v;
                    }
                }
            }

            foreach (var ((v1, v2), weight) in graph)
            {
                var coarseV1 = vertexToCoarse[v1];
                var coarseV2 = vertexToCoarse[v2];
                if (coarseV1 != coarseV2)
                {
                    if (!newGraph.ContainsKey((coarseV1, coarseV2)))
                    {
                        newGraph[(coarseV1, coarseV2)] = weight;
                    }
                    else
                    {
                        newGraph[(coarseV1, coarseV2)] += weight;
                    }
                }
            }

            return (newGraph, newVertices, coarseToOriginal);
        }

        // private static (Dictionary<(Vector2Int, Vector2Int), int>, List<Vector2Int>, Dictionary<Vector2Int, List<Vector2Int>>)
        // CoarsenGraph(Dictionary<(Vector2Int, Vector2Int), int> graph, List<Vector2Int> vertices)
        // {
        //     var newGraph = new Dictionary<(Vector2Int, Vector2Int), int>();
        //     var newVertices = new List<Vector2Int>();
        //     var coarseToOriginal = new Dictionary<Vector2Int, List<Vector2Int>>();
        //     var matched = new HashSet<Vector2Int>();

        //     var random = new System.Random();
        //     var shuffledVertices = vertices.OrderBy(v => random.Next()).ToList();

        //     foreach (var v in shuffledVertices)
        //     {
        //         if (!matched.Contains(v))
        //         {
        //             var closest = vertices
        //                 .Where(u => u != v && !matched.Contains(u))
        //                 .OrderBy(u => graph[(v, u)])
        //                 .FirstOrDefault();

        //             if (closest != default)
        //             {
        //                 var newVertex = new Vector2Int((v.x + closest.x) / 2, (v.y + closest.y) / 2);
        //                 newVertices.Add(newVertex);
        //                 coarseToOriginal[newVertex] = new List<Vector2Int> { v, closest };
        //                 matched.Add(v);
        //                 matched.Add(closest);
        //             }
        //             else
        //             {
        //                 newVertices.Add(v);
        //                 coarseToOriginal[v] = new List<Vector2Int> { v };
        //             }
        //         }
        //     }

        //     foreach (var (v1, v2) in graph.Keys)
        //     {
        //         var coarseV1 = coarseToOriginal.First(kv => kv.Value.Contains(v1)).Key;
        //         var coarseV2 = coarseToOriginal.First(kv => kv.Value.Contains(v2)).Key;
        //         if (coarseV1 != coarseV2)
        //         {
        //             newGraph[(coarseV1, coarseV2)] = graph[(v1, v2)];
        //         }
        //     }

        //     return (newGraph, newVertices, coarseToOriginal);
        // }

        // private static Dictionary<int, List<Vector2Int>> SpectralBisection(
        //     Dictionary<(Vector2Int, Vector2Int), int> graph, List<Vector2Int> vertices)
        // {
        //     int n = vertices.Count;
        //     var adjacencyMatrix = Matrix<double>.Build.Dense(n, n);

        //     for (int i = 0; i < n; i++)
        //     {
        //         for (int j = 0; j < n; j++)
        //         {
        //             if (i != j && graph.ContainsKey((vertices[i], vertices[j])))
        //             {
        //                 adjacencyMatrix[i, j] = -graph[(vertices[i], vertices[j])];
        //             }
        //         }
        //     }

        //     var degreeMatrix = Matrix<double>.Build.DenseDiagonal(n, i => adjacencyMatrix.Row(i).Sum());
        //     var laplacianMatrix = degreeMatrix - adjacencyMatrix;
        //     var eigen = laplacianMatrix.Evd(Symmetricity.Symmetric);
        //     var secondEigenvector = eigen.EigenVectors.Column(1);

        //     var partitions = new Dictionary<int, List<Vector2Int>>
        //     {
        //         [0] = new List<Vector2Int>(),
        //         [1] = new List<Vector2Int>()
        //     };

        //     for (int i = 0; i < n; i++)
        //     {
        //         if (secondEigenvector[i] < 0)
        //             partitions[0].Add(vertices[i]);
        //         else
        //             partitions[1].Add(vertices[i]);
        //     }

        //     return partitions;
        // }

        private static Dictionary<int, List<Vector2Int>> UncoarsenAndRefine(
            Dictionary<int, List<Vector2Int>> partition, Dictionary<Vector2Int, List<Vector2Int>> coarseToOriginal)
        {
            var refinedPartition = new Dictionary<int, List<Vector2Int>>();

            foreach (var (key, coarseVertices) in partition)
            {
                refinedPartition[key] = new List<Vector2Int>();
                foreach (var coarseVertex in coarseVertices)
                {
                    if (coarseToOriginal.ContainsKey(coarseVertex))
                    {
                        refinedPartition[key].AddRange(coarseToOriginal[coarseVertex]);
                    }
                }
            }

            return refinedPartition;
        }

        private static Dictionary<int, List<Vector2Int>> KernighanLinRefinement(
            Dictionary<int, List<Vector2Int>> partition)
        {
            bool improved;
            do
            {
                improved = false;
                foreach (var (key, nodes) in partition)
                {
                    foreach (var node in nodes.ToList())
                    {
                        var bestGain = 0;
                        var bestMove = -1;
                        foreach (var (targetKey, targetNodes) in partition)
                        {
                            if (targetKey != key)
                            {
                                int gain = targetNodes.Count - nodes.Count;
                                if (gain > bestGain)
                                {
                                    bestGain = gain;
                                    bestMove = targetKey;
                                }
                            }
                        }
                        if (bestMove != -1)
                        {
                            partition[key].Remove(node);
                            partition[bestMove].Add(node);
                            improved = true;
                        }
                    }
                }
            } while (improved);
            return partition;
        }
    }
}
