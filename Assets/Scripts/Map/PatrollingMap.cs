
using System.Collections.Generic;
using System.Linq;

namespace Maes.Map
{
    public class PatrollingMap : ICloneable<PatrollingMap>
    {
        public readonly Vertex[] Vertices;

        //TODO: Add a better way to debug point generation, its is currently outcommented
        // public IReadOnlyList<Vertex> DebugPoints { get; set; }

        public PatrollingMap(Vertex[] vertices)
        {
            Vertices = vertices;
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

            return new PatrollingMap(originalToCloned.Values.ToArray());
        }
    }
}