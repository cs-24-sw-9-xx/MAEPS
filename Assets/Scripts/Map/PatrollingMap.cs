
using System;
using System.Collections.Generic;
using System.Linq;

using Maes.Map.MapGen;
using Maes.Map.PathFinding;
using Maes.Robot;
using Maes.Utilities;

using UnityEngine;

namespace Maes.Map
{
    public class PatrollingMap : ICloneable<PatrollingMap>
    {
        public readonly Vertex[] Vertices;

        public readonly IReadOnlyDictionary<(int, int), Vector2Int[]> Paths;

        public PatrollingMap(Vertex[] vertices, SimulationMap<Tile> simulationMap)
        {
            Vertices = vertices;
            Paths = CreatePaths(vertices, simulationMap);
        }

        private PatrollingMap(Vertex[] vertices, IReadOnlyDictionary<(int, int), Vector2Int[]> paths)
        {
            Vertices = vertices;
            Paths = paths;
        }

        public PatrollingMap Clone()
        {
            var originalToCloned = new Dictionary<Vertex, Vertex>();
            foreach (var originalVertex in Vertices)
            {
                var clonedVertex = new Vertex(originalVertex.Id, originalVertex.Weight, originalVertex.Position, originalVertex.Color);
                originalToCloned.Add(originalVertex, clonedVertex);
            }

            foreach (var (originalVertex, clonedVertex) in originalToCloned)
            {
                foreach (var originalVertexNeighbor in originalVertex.Neighbors)
                {
                    var clonedVertexNeighbor = originalToCloned[originalVertexNeighbor];
                    clonedVertex.AddNeighbor(clonedVertexNeighbor);
                }
            }

            return new PatrollingMap(originalToCloned.Values.ToArray(), Paths);
        }

        private static IReadOnlyDictionary<(int, int), Vector2Int[]> CreatePaths(Vertex[] vertices, SimulationMap<Tile> simulationMap)
        {
            // HACK: Creating a slam map with robot constraints seems a bit hacky tbh :(
            var slamMap = new SlamMap(simulationMap, new RobotConstraints(mapKnown: true), 0);
            var coarseMap = slamMap.CoarseMap;
            var aStar = new AStar();
            var paths = new Dictionary<(int, int), Vector2Int[]>();
            foreach (var vertex in vertices)
            {
                foreach (var neighbor in vertex.Neighbors)
                {
                    if (paths.ContainsKey((vertex.Id, neighbor.Id)))
                    {
                        continue;
                    }

                    var path = aStar.GetOptimisticPath(vertex.Position, neighbor.Position, coarseMap) ?? throw new InvalidOperationException("No path from vertex to neighbor");

                    paths.Add((vertex.Id, neighbor.Id), path);
                    paths.Add((neighbor.Id, vertex.Id), path.Reverse().ToArray());
                }
            }

            return paths;
        }
    }
}