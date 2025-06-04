using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
                new Partition(0, vertices, () => vertices.ToDictionary(v => v.Position, _ => new Bitmap(1, 1)))
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

            using var bitmap = MapUtilities.MapToBitMap(coarseMap);

            var paths = new ConcurrentDictionary<(int, int), IReadOnlyList<PathStep>>();
            Parallel.ForEach(vertices, vertex =>
            {
                foreach (var neighbor in vertex.Neighbors)
                {
                    // This way we make sure that we don't calculate the reverse ones as well.
                    if (vertex.Id > neighbor.Id)
                    {
                        continue;
                    }


                    var path = NewAStar.FindPath(vertex.Position, neighbor.Position, bitmap, acceptPartialPaths: false, dependOnBrokenBehaviour: false)?.ToArray() ??
                               throw new InvalidOperationException("No path from vertex to neighbor");
                    var pathSteps = MyAStar.PathToStepsCheap(path);

                    var reversedPathSteps = new List<PathStep>(pathSteps.Count);
                    for (var i = pathSteps.Count - 1; i >= 0; i--)
                    {
                        var pathStep = pathSteps[i];
                        var reversedPathStep = new PathStep(pathStep.End, pathStep.Start, null!);
                        reversedPathSteps.Add(reversedPathStep);
                    }

                    var success1 = paths.TryAdd((vertex.Id, neighbor.Id), pathSteps);
                    var success2 = paths.TryAdd((neighbor.Id, vertex.Id), reversedPathSteps);

                    Debug.Assert(success1);
                    Debug.Assert(success2);
                }
            });

            Debug.LogFormat("Create Paths took {0} s", Time.realtimeSinceStartup - startTime);

            return paths;
        }
    }
}