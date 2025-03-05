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

        public readonly IReadOnlyDictionary<(int, int), PathStep[]> Paths;

        public readonly Dictionary<Vector2Int, Bitmap> VertexPositions;

        public PatrollingMap(Vertex[] vertices, SimulationMap<Tile> simulationMap, Dictionary<Vector2Int, Bitmap> vertexPositions)
        : this(vertices, CreatePaths(vertices, simulationMap), vertexPositions)
        {
        }

        private PatrollingMap(Vertex[] vertices, IReadOnlyDictionary<(int, int), PathStep[]> paths, Dictionary<Vector2Int, Bitmap> vertexPositions)
        {
            Vertices = vertices;
            Paths = paths;
            VertexPositions = vertexPositions;
        }

        /// <summary>
        /// Creates a deep copy of the current PatrollingMap instance.
        /// </summary>
        /// <remarks>
        /// Clones all vertices—including their Id, Weight, Position, Partition, and Color—and re-establishes their neighbor relationships.
        /// The cloned map retains the original path mappings and vertex positions.
        /// </remarks>
        public PatrollingMap Clone()
        {
            var originalToCloned = new Dictionary<Vertex, Vertex>();
            foreach (var originalVertex in Vertices)
            {
                var clonedVertex = new Vertex(originalVertex.Id, originalVertex.Weight, originalVertex.Position, originalVertex.Partition, originalVertex.Color);
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

        private static IReadOnlyDictionary<(int, int), PathStep[]> CreatePaths(Vertex[] vertices, SimulationMap<Tile> simulationMap)
        {
            // TODO: Skip this if we can use the breath first search stuff from WatchmanRouteSolver.
            // TODO: Of cause this requires specific code for that waypoint generation algorithm.

            var startTime = Time.realtimeSinceStartup;

            // HACK: Creating a slam map with robot constraints seems a bit hacky tbh :(
            var slamMap = new SlamMap(simulationMap, new RobotConstraints(mapKnown: true), 0);
            var coarseMap = slamMap.CoarseMap;
            var aStar = new MyAStar();
            var paths = new Dictionary<(int, int), PathStep[]>();
            foreach (var vertex in vertices)
            {
                foreach (var neighbor in vertex.Neighbors)
                {
                    //var path = aStar.GetOptimisticPath(vertex.Position, neighbor.Position, coarseMap) ?? throw new InvalidOperationException("No path from vertex to neighbor");
                    var path = aStar.GetNonBrokenPath(vertex.Position, neighbor.Position, coarseMap) ?? throw new InvalidOperationException("No path from vertex to neighbor");
                    var pathSteps = MyAStar.PathToStepsCheap(path).ToArray();

                    paths.Add((vertex.Id, neighbor.Id), pathSteps);
                }
            }

            Debug.LogFormat("Create Paths took {0} s", Time.realtimeSinceStartup - startTime);

            return paths;
        }
    }
}