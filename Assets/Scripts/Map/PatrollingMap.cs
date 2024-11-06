#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace Maes.Map
{
    public class PatrollingMap : ICloneable
    {
        public readonly IReadOnlyList<Vertex> Verticies;

        public PatrollingMap(IEnumerable<Vertex> verticies)
        {
            Verticies = verticies.ToList();
        }

        public object Clone()
        {
            return new PatrollingMap(this.Verticies.Select(v => (Vertex)v.Clone()).ToArray());
        }
    }
}