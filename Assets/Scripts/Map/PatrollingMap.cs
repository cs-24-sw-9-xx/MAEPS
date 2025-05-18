using System;
using System.Collections.Generic;
using System.Linq;

using Maes.Map.Generators;
using Maes.Map.PathFinding;
using Maes.Robot;
using Maes.Utilities;

using UnityEngine;

namespace Maes.Map
{
    public sealed class PatrollingMap : ICloneable<PatrollingMap>
    {
        public readonly IReadOnlyList<Vertex> Vertices;

        public readonly IReadOnlyList<Partition> Partitions;

        public readonly IReadOnlyDictionary<(int, int), IReadOnlyList<PathStep>> Paths;

        public PatrollingMap(IReadOnlyList<Vertex> vertices, SimulationMap<Tile> simulationMap)
        : this(vertices, CreatePaths(vertices, simulationMap))
        {
        }


        public PatrollingMap(IReadOnlyList<Vertex> vertices, SimulationMap<Tile> simulationMap, IReadOnlyList<Partition> partitions)
        : this(vertices, CreatePaths(vertices, simulationMap), partitions)
        {
        }

        private PatrollingMap(IReadOnlyList<Vertex> vertices, IReadOnlyDictionary<(int, int), IReadOnlyList<PathStep>> paths)
        {
            Vertices = vertices;
            Paths = paths;
            Partitions = new List<Partition>
            {
                new Partition(0, vertices, vertices.ToDictionary(v => v.Position, _ => new Bitmap(1, 1)))
            };
        }

        private PatrollingMap(IReadOnlyList<Vertex> vertices, IReadOnlyDictionary<(int, int), IReadOnlyList<PathStep>> paths, IReadOnlyList<Partition> partitions)
        {
            Vertices = vertices;
            Paths = paths;
            Partitions = partitions;
        }

        public PatrollingMap Clone()
        {
            var originalToCloned = new Dictionary<Vertex, Vertex>(Vertices.Count);
            foreach (var originalVertex in Vertices)
            {
                var clonedVertex = new Vertex(originalVertex.Id, originalVertex.Position, originalVertex.Partition, originalVertex.Color);
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

            return new PatrollingMap(originalToCloned.Values.ToArray(), Paths, Partitions);
        }

        private static IReadOnlyDictionary<(int, int), IReadOnlyList<PathStep>> CreatePaths(IReadOnlyList<Vertex> vertices, SimulationMap<Tile> simulationMap)
        {
            // HACK: Creating a slam map with robot constraints seems a bit hacky tbh :(
            var slamMap = new SlamMap(simulationMap, new RobotConstraints(mapKnown: true), 0);
            var coarseMap = slamMap.CoarseMap;

            return CreatePaths(vertices, coarseMap);
        }

        private static IReadOnlyDictionary<(int, int), IReadOnlyList<PathStep>> CreatePaths(IReadOnlyList<Vertex> vertices, CoarseGrainedMap coarseMap)
        {
            var startTime = Time.realtimeSinceStartup;

            var aStar = new MyAStar();
            var paths = new Dictionary<(int, int), IReadOnlyList<PathStep>>();
            foreach (var vertex in vertices)
            {
                foreach (var neighbor in vertex.Neighbors)
                {
                    var path = MyAStar.GetNonBrokenPath(vertex.Position, neighbor.Position, coarseMap) ?? throw new InvalidOperationException("No path from vertex to neighbor");
                    var pathSteps = MyAStar.PathToStepsCheap(path);

                    paths.Add((vertex.Id, neighbor.Id), pathSteps);
                }
            }

            Debug.LogFormat("Create Paths took {0} s", Time.realtimeSinceStartup - startTime);

            return paths;
        }
    }
}