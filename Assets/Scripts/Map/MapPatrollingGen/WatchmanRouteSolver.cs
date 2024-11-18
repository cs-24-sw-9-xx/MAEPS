using System.Collections.Generic;
using System.Linq;
using Maes.Map.MapGen;
using UnityEngine;

namespace Maes.Map.MapPatrollingGen
{
    public static class WatchmanRouteSolver
    {
        public static PatrollingMap MakePatrollingMap(SimulationMap<Tile> simulationMap)
        {
            var map = MapToBitMap(simulationMap);
            var vertexPositions = SolveWatchmanRoute(map).Select(pos => MoveGuardsAwayFromWalls(simulationMap, pos)).ToList();
            var distanceMatrix = CalculateDistanceMatrix(map, vertexPositions);
            var connectedvertices = ConnectVerticies(vertexPositions, distanceMatrix);
            return new PatrollingMap(connectedvertices);
        }

        private static List<Vertex> ConnectVerticies(List<Vector2Int> guardPositions, Dictionary<(Vector2Int, Vector2Int), int> distanceMatrix)
        {
            const int numberOfReverstNearestNeighbors = 1;
            var reverseNearestNeighbors = FindReverseNearestNeighbors(distanceMatrix, numberOfReverstNearestNeighbors);

            var vertices = guardPositions.ConvertAll(pos => new Vertex(0, pos));
            var vertexMap = vertices.ToDictionary(v => v.Position);

            foreach (var (position, neighbors) in reverseNearestNeighbors)
            {
                if (vertexMap.TryGetValue(position, out var vertex))
                {
                    var color = Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.5f, 1f);
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

        private static Vector2Int MoveGuardsAwayFromWalls(SimulationMap<Tile> simulationMap, Vector2Int pos)
        {
            var newPos = Vector2Int.zero;
            for (var x = -1; x <= 1; x++)
            {
                for (var y = -1; y <= 1; y++)
                {
                    var tile = simulationMap.GetTileByLocalCoordinate(pos.x + x, pos.y + y);
                    var firstTri = tile.GetTriangles()[0];
                    var isWall = Tile.IsWall(firstTri.Type);

                    if (isWall)
                    {
                        newPos += new Vector2Int(-x, -y);
                    }
                }
            }

            newPos.Clamp(new Vector2Int(-1, -1), new Vector2Int(1, 1));
            return pos + newPos;
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

        private static List<Vector2Int> SolveWatchmanRoute(bool[,] map)
        {
            var guardPositions = new List<Vector2Int>();
            var uncoveredTiles = new HashSet<Vector2Int>();
            var precomputedVisibility = new Dictionary<Vector2Int, HashSet<Vector2Int>>();

            // Collect all floor tiles
            for (var x = 0; x < map.GetLength(0); x++)
            {
                for (var y = 0; y < map.GetLength(1); y++)
                {
                    if (!map[x, y])
                    {
                        var tile = new Vector2Int(x, y);
                        uncoveredTiles.Add(tile);

                        // Precompute visibility for each tile
                        precomputedVisibility[tile] = ComputeVisibility(tile, map);
                    }
                }
            }

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
                foreach (var coveredTile in bestCoverage)
                {
                    uncoveredTiles.Remove(coveredTile);
                }
            }
            return guardPositions;
        }

        // Precompute visibility using an efficient line-drawing algorithm
        private static HashSet<Vector2Int> ComputeVisibility(Vector2Int start, bool[,] map)
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

        // Depth-first search to find connected components
        private static void DepthFirstSearch(Vertex vertex, HashSet<Vertex> visited, List<Vertex> component)
        {
            if (visited.Contains(vertex))
            {
                return;
            }

            visited.Add(vertex);
            component.Add(vertex);

            foreach (var neighbor in vertex.Neighbors)
            {
                if (!visited.Contains(neighbor))
                {
                    DepthFirstSearch(neighbor, visited, component);
                }
            }
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

            List<Vertex> closestIsland = null;
            Vertex closestVertexInCurrent = null;
            Vertex closestVertexInNew = null;
            var minDistance = int.MaxValue;

            // Find the closest island and vertices to merge
            foreach (var island in remainingIslands)
            {
                foreach (var currentVertex in currentIsland)
                {
                    foreach (var newVertex in island)
                    {
                        if (distanceDict.TryGetValue((currentVertex.Position, newVertex.Position), out var distance) ||
                            distanceDict.TryGetValue((newVertex.Position, currentVertex.Position), out distance))
                        {
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
            }

            // Connect the closest vertices between the current island and the closest island
            if (closestVertexInCurrent != null && closestVertexInNew != null)
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
            List<Vertex> vertices,
            Dictionary<(Vector2Int, Vector2Int), int> distanceDict)
        {
            var visited = new HashSet<Vertex>();
            var components = new List<List<Vertex>>();

            // Identify all connected components using DFS
            foreach (var vertex in vertices)
            {
                if (!visited.Contains(vertex))
                {
                    var component = new List<Vertex>();
                    DepthFirstSearch(vertex, visited, component);
                    components.Add(component);
                }
            }

            if (components.Count > 1)
            {
                // Start with the first component as the initial island
                var initialIsland = components[0];
                components.RemoveAt(0);

                // Recursively merge all components into one island
                MergeIslandsRecursively(initialIsland, components, distanceDict);
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

        // BFS to find the shortest path from the start position to all other verticies
        private static void BreathFirstSearch(Vector2Int startPosition, Dictionary<(Vector2Int, Vector2Int), int> shortestGridPath, List<Vector2Int> guardPositions, bool[,] map)
        {
            var directions = new Vector2Int[]
            {
            new(0, 1),
            new(0, -1),
            new(1, 0),
            new(-1, 0)
            };

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

                foreach (var direction in directions)
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
