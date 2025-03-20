using System.Collections.Generic;

using Maes.Utilities;

using UnityEngine;

namespace Maes.Map.Generators.Patrolling.Waypoints.Connectors
{
    public static class AllConnectedWaypointConnector
    {
        public static Vertex[] ConnectVertices(Bitmap map,
            Dictionary<Vector2Int, Bitmap> vertexPositions)
        {
            var vertices = new Vertex[vertexPositions.Count];
            var id = 0;
            foreach (var (vertexPosition, _) in vertexPositions)
            {
                vertices[id] = new Vertex(id, 1f, vertexPosition);
                id++;
            }

            foreach (var vertex in vertices)
            {
                foreach (var neighborVertex in vertices)
                {
                    if (vertex == neighborVertex)
                    {
                        continue;
                    }

                    vertex.AddNeighbor(neighborVertex);
                }
            }

            return vertices;
        }
    }
}