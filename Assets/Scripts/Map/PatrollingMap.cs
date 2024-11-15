
using System;
using System.Collections.Generic;
using System.Linq;

namespace Maes.Map
{
    public class PatrollingMap : ICloneable
    {
        public readonly IReadOnlyList<Vertex> Vertices;

        //TODO: Add a better way to debug point generation, its is currently outcommented
        // public IReadOnlyList<Vertex> DebugPoints { get; set; }

        public PatrollingMap(IEnumerable<Vertex> vertices)
        {
            Vertices = vertices.ToList();
        }

        public object Clone()
        {
            return new PatrollingMap(Vertices.Select(v => (Vertex)v.Clone()).ToArray());
        }
    }
}