using Maes.Map;

using UnityEngine;

namespace Maes.Trackers
{
    public class VertexDetails : Vertex
    {
        public int NumberOfVisits { get; set; } = 0;
        public int MaxIdleness { get; set; } = 0;

        public VertexDetails(float weight, Vector2Int position, Color? color = null) : base(weight, position, color)
        {
        }

        public VertexDetails(Vertex vertex) : base(vertex.Weight, vertex.Position, vertex.Color) { }
    }
}