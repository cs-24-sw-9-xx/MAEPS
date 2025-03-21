
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

        public readonly IReadOnlyDictionary<(int, int), IReadOnlyList<PathStep>> Paths;

        public readonly IReadOnlyDictionary<Vector2Int, Bitmap> VertexPositions;

        public PatrollingMap(IReadOnlyList<Vertex> vertices, SimulationMap<Tile> simulationMap, IReadOnlyDictionary<Vector2Int, Bitmap> vertexPositions)
        : this(vertices, CreatePaths(vertices, simulationMap), vertexPositions)
        {
        }

        private PatrollingMap(IReadOnlyList<Vertex> vertices, IReadOnlyDictionary<(int, int), IReadOnlyList<PathStep>> paths, IReadOnlyDictionary<Vector2Int, Bitmap> vertexPositions)
        {
            Vertices = vertices;
            Paths = paths;
            VertexPositions = vertexPositions;
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

            return new PatrollingMap(originalToCloned.Values.ToArray(), Paths, VertexPositions);
        }

        private static IReadOnlyDictionary<(int, int), IReadOnlyList<PathStep>> CreatePaths(IReadOnlyList<Vertex> vertices, SimulationMap<Tile> simulationMap)
        {
            // TODO: Skip this if we can use the breath first search stuff from WatchmanRouteSolver.
            // TODO: Of cause this requires specific code for that waypoint generation algorithm.

            var startTime = Time.realtimeSinceStartup;

            // HACK: Creating a slam map with robot constraints seems a bit hacky tbh :(
            var slamMap = new SlamMap(simulationMap, new RobotConstraints(mapKnown: true), 0);
            var coarseMap = slamMap.CoarseMap;
            var aStar = new MyAStar();
            var paths = new Dictionary<(int, int), IReadOnlyList<PathStep>>();
            foreach (var vertex in vertices)
            {
                foreach (var neighbor in vertex.Neighbors)
                {
                    var path = aStar.GetNonBrokenPath(vertex.Position, neighbor.Position, coarseMap) ?? throw new InvalidOperationException("No path from vertex to neighbor");
                    var pathSteps = MyAStar.PathToStepsCheap(path);

                    paths.Add((vertex.Id, neighbor.Id), pathSteps);
                }
            }

            Debug.LogFormat("Create Paths took {0} s", Time.realtimeSinceStartup - startTime);

            return paths;
        }
    }
}