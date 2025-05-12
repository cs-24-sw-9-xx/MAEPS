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

using System.Collections.Generic;
using System.Linq;

using Maes.Map;

namespace Maes.Algorithms.Patrolling
{
    /// <summary>
    /// Implementation of the Single Cycle algorithm but using the solver approximation christofides as in: https://doi.org/10.1007/978-3-540-28645-5_48.
    /// Christofides runs in O(n^3 lg n) time, where n is the number of vertices in the partition.
    /// For reference (on my machine):
    /// For 21 vertices, it took 8 seconds to compute the cycle.
    /// For 9 vertices, it took 0.2 seconds to compute the cycle.
    /// An implementation can be found here: https://github.com/matteoprata/DRONET-for-Patrolling/blob/main_july_2023/src/patrolling/tsp_cycle.py
    /// </summary>
    public sealed class SingleCycleChristofides : BaseCyclicAlgorithm
    {
        public override string AlgorithmName => "SingleCycleChristofides Algorithm";

        /// <summary>
        /// Use Christofides to get an approximate solution to TSP to cycle of all vertices in this robots partition.
        /// </summary>
        /// <param name="startVertex"></param>
        protected override List<Vertex> CreatePatrollingCycle(Vertex startVertex)
        {
            var verticesInPartition = PatrollingMap.Vertices.Where(v => v.Partition == startVertex.Partition).ToList();
            var estimatedDistanceMatrix = EstimatedDistanceMatrix(verticesInPartition);
            var solution = Christofides(verticesInPartition, estimatedDistanceMatrix);
            return solution.Select(id => verticesInPartition.Single(v => v.Id == id)).ToList();
        }

        private readonly struct Edge
        {
            public Edge(int from, int to, float weight)
            {
                From = from;
                To = to;
                Weight = weight;
            }
            public readonly int From, To;
            public readonly float Weight;
        }

        /// <summary>
        /// Christofides algorithm to find a cycle in a complete graph.
        /// <see cref="https://en.wikipedia.org/wiki/Christofides_algorithm"/>
        /// </summary>
        /// <returns>List of vertex ids in the order that gives a cycle, which is within 3/2 of the optimum.</returns>
        private static List<int> Christofides(IReadOnlyList<Vertex> vertices, float[,] dist)
        {
            var n = vertices.Count;

            var mst = MinimumSpanningTree(dist);

            var degree = new int[n];
            foreach (var edge in mst)
            {
                degree[edge.From]++;
                degree[edge.To]++;
            }

            var oddVertices = new List<int>();
            for (var i = 0; i < n; i++)
            {
                if (degree[i] % 2 != 0)
                {
                    oddVertices.Add(i);
                }
            }

            var matching = MinimumWeightMatching(oddVertices, dist);

            var multigraph = new List<Edge>(mst);
            multigraph.AddRange(matching);

            var eulerianCircuit = EulerianCircuit(multigraph, n);

            var tspPath = Shortcut(eulerianCircuit);

            return tspPath;
        }

        private static List<Edge> MinimumSpanningTree(float[,] dist)
        {
            var n = dist.GetLength(0);
            var inTree = new bool[n];
            var minDist = new float[n];
            var parent = new int[n];

            for (var i = 0; i < n; i++)
            {
                minDist[i] = float.MaxValue;
                parent[i] = -1;
            }

            minDist[0] = 0;

            for (var count = 0; count < n; count++)
            {
                var u = -1;
                var min = float.MaxValue;

                for (var i = 0; i < n; i++)
                {
                    if (!inTree[i] && minDist[i] < min)
                    {
                        min = minDist[i];
                        u = i;
                    }
                }

                inTree[u] = true;

                for (var v = 0; v < n; v++)
                {
                    if (!inTree[v] && dist[u, v] < minDist[v])
                    {
                        minDist[v] = dist[u, v];
                        parent[v] = u;
                    }
                }
            }

            var mst = new List<Edge>();
            for (var i = 1; i < n; i++)
            {
                mst.Add(new Edge(from: parent[i], to: i, weight: dist[parent[i], i]));
            }

            return mst;
        }

        private static List<Edge> MinimumWeightMatching(List<int> oddVertices, float[,] dist)
        {
            var matched = new HashSet<int>();
            var result = new List<Edge>();

            while (matched.Count < oddVertices.Count)
            {
                var u = oddVertices.First(i => !matched.Contains(i));
                var minWeight = float.MaxValue;
                var bestV = -1;

                foreach (var v in oddVertices)
                {
                    if (u == v || matched.Contains(v))
                    {
                        continue;
                    }

                    if (dist[u, v] < minWeight)
                    {
                        minWeight = dist[u, v];
                        bestV = v;
                    }
                }

                if (bestV != -1)
                {
                    result.Add(new Edge(from: u, to: bestV, weight: dist[u, bestV]));
                    matched.Add(u);
                    matched.Add(bestV);
                }
            }

            return result;
        }

        private static List<int> EulerianCircuit(List<Edge> multigraph, int n)
        {
            var adj = new Dictionary<int, List<int>>();

            // Build an undirected adjacency list
            foreach (var edge in multigraph)
            {
                if (!adj.ContainsKey(edge.From))
                {
                    adj[edge.From] = new List<int>();
                }

                if (!adj.ContainsKey(edge.To))
                {
                    adj[edge.To] = new List<int>();
                }

                adj[edge.From].Add(edge.To);
                adj[edge.To].Add(edge.From);
            }

            var circuit = new List<int>();
            var stack = new Stack<int>();
            stack.Push(0);

            while (stack.Count > 0)
            {
                var v = stack.Peek();

                if (adj.ContainsKey(v) && adj[v].Count > 0)
                {
                    var u = adj[v][0]; // Pick any adjacent node
                    adj[v].Remove(u);
                    adj[u].Remove(v); // Undirected edge removal
                    stack.Push(u);
                }
                else
                {
                    circuit.Add(stack.Pop());
                }
            }

            return circuit;
        }

        private static List<int> Shortcut(List<int> circuit)
        {
            var visited = new HashSet<int>();
            var path = new List<int>();

            foreach (var node in circuit)
            {
                if (!visited.Contains(node))
                {
                    visited.Add(node);
                    path.Add(node);
                }
            }
            // We wrap-round the list to make it a cycle, so we don't need to add the first node again.
            // path.Add(path[0]); // complete the cycle
            return path;
        }
    }
}