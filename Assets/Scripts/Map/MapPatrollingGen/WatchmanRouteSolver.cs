using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Maes.Map.MapGen;

using UnityEngine;

namespace Maes.Map.MapPatrollingGen
{
    public static class WatchmanRouteSolver
    {
        public static PatrollingMap MakePatrollingMap(SimulationMap<Tile> simulationMap)
        {
            var map = MapToBitMap(simulationMap);
            var vertexPositions = SolveWatchmanRoute(map);
            var distanceMatrix = CalculateDistanceMatrix(map, vertexPositions);
            var connectedvertices = ConnectVerticies(vertexPositions, distanceMatrix);
            return new PatrollingMap(connectedvertices, simulationMap);
        }

        private static Vertex[] ConnectVerticies(List<Vector2Int> guardPositions, Dictionary<(Vector2Int, Vector2Int), int> distanceMatrix)
        {
            const int numberOfReverstNearestNeighbors = 1;
            var reverseNearestNeighbors = FindReverseNearestNeighbors(distanceMatrix, numberOfReverstNearestNeighbors);
            var i = 0;
            var vertices = guardPositions.Select(pos => new Vertex(i++, 0, pos)).ToArray();
            var vertexMap = vertices.ToDictionary(v => v.Position);

            foreach (var (position, neighbors) in reverseNearestNeighbors)
            {
                if (vertexMap.TryGetValue(position, out var vertex))
                {
                    var color = UnityEngine.Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.5f, 1f);
                    vertex.Color = color;
                    foreach (var neighborPos in neighbors)
                    {
                        if (vertexMap.TryGetValue(neighborPos, out var neighborVertex))
                        {
                            neighborVertex.Color = color;
                            vertex.AddNeighbor(neighborVertex);
                            neighborVertex.AddNeighbor(vertex);
                        }
                    }
                }
            }

            ConnectIslands(vertices, distanceMatrix);
            return vertices;
        }

        private static bool[,] MapToBitMap(SimulationMap<Tile> simulationMap)
        {
            var map = new bool[simulationMap.WidthInTiles, simulationMap.HeightInTiles];
            for (var x = 0; x < simulationMap.WidthInTiles; x++)
            {
                for (var y = 0; y < simulationMap.HeightInTiles; y++)
                {
                    var tile = simulationMap.GetTileByLocalCoordinate(x, y);
                    var firstTri = tile.GetTriangles()[0];
                    map[x, y] = Tile.IsWall(firstTri.Type);
                }
            }

            return map;
        }

        // Solve the watchman route problem using a greedy algorithm.
        // The inspiration for the code can be found in this paper https://www.researchgate.net/publication/37987286_An_Approximate_Algorithm_for_Solving_the_Watchman_Route_Problem
        private static List<Vector2Int> SolveWatchmanRoute(bool[,] map)
        {
            var precomputedVisibility = ComputeVisibility(map);
            var guardPositions = ComputeVertexCoordinates(precomputedVisibility);
            return guardPositions;
        }

        private static List<Vector2Int> ComputeVertexCoordinates(IDictionary<Vector2Int, HashSet<Vector2Int>> precomputedVisibility)
        {
            var guardPositions = new List<Vector2Int>();
            var uncoveredTiles = precomputedVisibility.Keys.ToHashSet();

            // Greedy algorithm to find the best guard positions
            while (uncoveredTiles.Count > 0)
            {
                var bestGuardPosition = Vector2Int.zero;
                var bestCoverage = new HashSet<Vector2Int>();

                // Find the guard position that covers the most uncovered tiles
                foreach (var candidate in uncoveredTiles)
                {
                    var coverage = new HashSet<Vector2Int>(precomputedVisibility[candidate]);
                    coverage.IntersectWith(uncoveredTiles);

                    if (coverage.Count > bestCoverage.Count ||
                       (coverage.Count == bestCoverage.Count &&
                        AveargeEuclideanDistance(candidate, guardPositions) < AveargeEuclideanDistance(bestGuardPosition, guardPositions)))
                    {
                        bestGuardPosition = candidate;
                        bestCoverage = coverage;
                    }
                }

                guardPositions.Add(bestGuardPosition);
                uncoveredTiles.ExceptWith(bestCoverage);
            }

            return guardPositions;
        }

        private static IDictionary<Vector2Int, HashSet<Vector2Int>> ComputeVisibility(bool[,] map)
        {
            var precomputedVisibility = new ConcurrentDictionary<Vector2Int, HashSet<Vector2Int>>();
            var width = map.GetLength(0);
            var height = map.GetLength(1);

            // Outermost loop parallelized to improve performance
            Parallel.For(0, width, x =>
            {
                for (var y = 0; y < height; y++)
                {
                    var tile = new Vector2Int(x, y);
                    if (!map[x, y])
                    {
                        // Precompute visibility for each tile
                        // Optionally use ComputeVisibilityOfPointFastBreakColumn for improved performance
                        precomputedVisibility[tile] = ComputeVisibilityOfPoint(tile, map);
                    }
                }
            });
            // To debug the ComputeVisibility method, use the following utility method to save as image
            // SaveAsImage.SaveVisibileTiles();

            return precomputedVisibility.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        // Precompute visibility using an efficient line-drawing algorithm
        public static HashSet<Vector2Int> ComputeVisibilityOfPointFastBreakColumn(Vector2Int start, bool[,] map)
        {
            var visibilitySet = new HashSet<Vector2Int>();

            // Traverse columns in both directions
            TraverseColumn(start, map, visibilitySet, 1);  // Right direction
            TraverseColumn(start, map, visibilitySet, -1); // Left direction

            return visibilitySet;
        }

        // Traverses the map column by column, pruning the search based on visibility
        private static void TraverseColumn(Vector2Int start, bool[,] map, HashSet<Vector2Int> visibilitySet, int direction)
        {
            for (var x = start.x; x >= 0 && x < map.GetLength(0); x += direction)
            {
                var resultInCurrentColumn = false;
                for (var y = 0; y < map.GetLength(1); y++)
                {
                    var target = new Vector2Int(x, y);

                    if (!map[x, y] && IsInLineOfSight(start, target, map))
                    {
                        visibilitySet.Add(target);
                        resultInCurrentColumn = true;
                    }
                }

                // Stop if the current column has no visible tiles
                if (!resultInCurrentColumn && x != start.x)
                {
                    break;
                }
            }
        }

        // Precompute visibility using an efficient line-drawing algorithm
        public static HashSet<Vector2Int> ComputeVisibilityOfPoint(Vector2Int start, bool[,] map)
        {
            var visibilitySet = new HashSet<Vector2Int>();
            for (var x = 0; x < map.GetLength(0); x++)
            {
                for (var y = 0; y < map.GetLength(1); y++)
                {
                    var target = new Vector2Int(x, y);
                    if (!map[x, y] && IsInLineOfSight(start, target, map))
                    {
                        visibilitySet.Add(target);
                    }
                }
            }
            return visibilitySet;
        }

        // Helper method to calculate the average Euclidean distance of a guard position to a list of other guard positions
        private static float AveargeEuclideanDistance(Vector2Int guardPosition, List<Vector2Int> currentGuardPositions)
        {
            var sum = 0f;
            foreach (var pos in currentGuardPositions)
            {
                sum += Vector2Int.Distance(guardPosition, pos);
            }

            return sum / currentGuardPositions.Count;
        }

        // Method to check visibility using a line-of-sight algorithm
        private static bool IsInLineOfSight(Vector2Int start, Vector2Int end, bool[,] map)
        {
            // Implement Bresenham's line algorithm for visibility check
            // Return true if there is a clear line-of-sight, otherwise false
            var dx = Mathf.Abs(end.x - start.x);
            var dy = Mathf.Abs(end.y - start.y);
            var sx = start.x < end.x ? 1 : -1;
            var sy = start.y < end.y ? 1 : -1;
            var err = dx - dy;

            var x = start.x;
            var y = start.y;

            while (x != end.x || y != end.y)
            {
                if (map[x, y])
                {
                    return false; // Hit a wall
                }

                var e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y += sy;
                }
            }

            return true;
        }

        // Find the k nearest neighbors for each point and return the reverse mapping
        private static Dictionary<Vector2Int, List<Vector2Int>> FindReverseNearestNeighbors(Dictionary<(Vector2Int, Vector2Int), int> distanceDict, int k)
        {
            // Dictionary to store the k nearest neighbors of each point
            var nearestNeighbors = new Dictionary<Vector2Int, List<(Vector2Int, int)>>();

            // Populate nearest neighbors with distances for each start point
            foreach (var ((start, end), distance) in distanceDict)
            {
                if (!nearestNeighbors.ContainsKey(start))
                {
                    nearestNeighbors[start] = new List<(Vector2Int, int)>();
                }

                var neighborsList = nearestNeighbors[start];
                neighborsList.Add((end, distance));

                // Sort by distance and keep only the top k nearest neighbors
                neighborsList.Sort((a, b) => a.Item2.CompareTo(b.Item2));
                if (neighborsList.Count > k)
                {
                    neighborsList.RemoveAt(neighborsList.Count - 1); // Remove the farthest neighbor
                }
            }

            // Reverse nearest neighbor mapping
            var reverseNearestNeighbors = new Dictionary<Vector2Int, List<Vector2Int>>();

            foreach (var (start, neighborsList) in nearestNeighbors)
            {
                foreach (var (neighbor, _) in neighborsList)
                {
                    if (!reverseNearestNeighbors.ContainsKey(neighbor))
                    {
                        reverseNearestNeighbors[neighbor] = new List<Vector2Int>();
                    }
                    reverseNearestNeighbors[neighbor].Add(start);
                }
            }

            return reverseNearestNeighbors;
        }

        // Merge islands (isolated connected verticies) recursively until all islands are connected
        private static List<Vertex> MergeIslandsRecursively(
            List<Vertex> currentIsland,
            List<List<Vertex>> remainingIslands,
            Dictionary<(Vector2Int, Vector2Int), int> distanceDict)
        {
            if (remainingIslands.Count == 0)
            {
                return currentIsland; // Base case: no more islands to merge
            }

            List<Vertex>? closestIsland = null;
            Vertex? closestVertexInCurrent = null;
            Vertex? closestVertexInNew = null;
            var minDistance = int.MaxValue;

            // Find the closest island and vertices to merge
            foreach (var island in remainingIslands)
            {
                foreach (var currentVertex in currentIsland)
                {
                    foreach (var newVertex in island)
                    {
                        var distance = distanceDict[(currentVertex.Position, newVertex.Position)];
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            closestVertexInCurrent = currentVertex;
                            closestVertexInNew = newVertex;
                            closestIsland = island;
                        }
                    }
                }
            }

            // Connect the closest vertices between the current island and the closest island
            if (closestIsland != null && closestVertexInCurrent != null && closestVertexInNew != null)
            {
                closestVertexInCurrent.AddNeighbor(closestVertexInNew);
                closestVertexInNew.AddNeighbor(closestVertexInCurrent);

                // Merge the closest island into the current island
                currentIsland.AddRange(closestIsland);
                remainingIslands.Remove(closestIsland);

                // Recursive call to merge the next closest island
                return MergeIslandsRecursively(currentIsland, remainingIslands, distanceDict);
            }

            return currentIsland; // Return the current island if no more connections can be made
        }

        private static void ConnectIslands(
            Vertex[] vertices,
            Dictionary<(Vector2Int, Vector2Int), int> distanceDict)
        {
            var visited = new HashSet<Vertex>();

            // Function to traverse a cluster and collect its vertices
            List<Vertex> TraverseCluster(Vertex start)
            {
                var cluster = new List<Vertex>();
                var stack = new Stack<Vertex>();
                stack.Push(start);

                while (stack.Count > 0)
                {
                    var current = stack.Pop();
                    if (!visited.Contains(current))
                    {
                        visited.Add(current);
                        cluster.Add(current);

                        foreach (var neighbor in current.Neighbors)
                        {
                            if (!visited.Contains(neighbor))
                            {
                                stack.Push(neighbor);
                            }
                        }
                    }
                }

                return cluster;
            }

            var clusters = new List<List<Vertex>>();

            // Identify disconnected clusters dynamically
            foreach (var vertex in vertices)
            {
                if (!visited.Contains(vertex))
                {
                    clusters.Add(TraverseCluster(vertex));
                }
            }

            if (clusters.Count > 1)
            {
                // Start with the first cluster as the initial island
                var initialIsland = clusters[0];
                clusters.RemoveAt(0);

                // Recursively merge all clusters into one
                MergeIslandsRecursively(initialIsland, clusters, distanceDict);
            }
        }


        // Calculate the shortest path between all pairs of verticies
        private static Dictionary<(Vector2Int, Vector2Int), int> CalculateDistanceMatrix(bool[,] map, List<Vector2Int> verticies)
        {
            Dictionary<(Vector2Int, Vector2Int), int> shortestGridPath = new();

            foreach (var vertex in verticies)
            {
                BreathFirstSearch(vertex, shortestGridPath, verticies, map);
            }

            return shortestGridPath;
        }

        // Function to check if a position is within bounds and walkable
        private static bool IsWalkable(Vector2Int pos, bool[,] map)
        {
            return pos.x >= 0 && pos.x < map.GetLength(0) &&
                   pos.y >= 0 && pos.y < map.GetLength(1) &&
                   !map[pos.x, pos.y]; // true if the position is not a wall
        }

        private static readonly Vector2Int[] Directions = new Vector2Int[]
        {
            new(0, 1),
            new(0, -1),
            new(1, 0),
            new(-1, 0)
        };

        // BFS to find the shortest path from the start position to all other verticies
        private static void BreathFirstSearch(Vector2Int startPosition, Dictionary<(Vector2Int, Vector2Int), int> shortestGridPath, List<Vector2Int> guardPositions, bool[,] map)
        {
            var queue = new Queue<Vector2Int>();
            var visited = new HashSet<Vector2Int>();
            var distanceMap = new Dictionary<Vector2Int, int>();

            queue.Enqueue(startPosition);
            visited.Add(startPosition);
            distanceMap[startPosition] = 0;

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                var currentDistance = distanceMap[current];

                foreach (var direction in Directions)
                {
                    var neighbor = current + direction;

                    if (IsWalkable(neighbor, map) && !visited.Contains(neighbor))
                    {
                        queue.Enqueue(neighbor);
                        visited.Add(neighbor);
                        distanceMap[neighbor] = currentDistance + 1;

                        if (guardPositions.Contains(neighbor))
                        {
                            shortestGridPath[(startPosition, neighbor)] = distanceMap[neighbor];
                        }
                    }
                }
            }
        }
    }
}