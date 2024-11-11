
using System;
using System.Collections.Generic;
using System.Linq;

namespace Maes.Map
{
    public class PatrollingMap : ICloneable
    {
        public readonly IReadOnlyList<Vertex> Vertices;

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