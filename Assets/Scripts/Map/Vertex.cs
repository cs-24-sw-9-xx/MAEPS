#nullable enable

using System.Collections.Generic;
using UnityEngine;


namespace Maes.Map {
    public class Vertex
    {
        private readonly HashSet<Vertex> _neighbors = new HashSet<Vertex>();
        public float Weight { get; }
        public int Idleness { get; private set; }
        public Vector2Int Position { get; }
        public Color Color { get; }

        public Vertex(float weight, Vector2Int position, Color? color = null)
        {
            Weight = weight;
            Position = position;
            Color = color ?? Color.green;
        }
        
        public IReadOnlyCollection<Vertex> Neighbors => _neighbors;
        
        public void VisitedAtTick(int tick)
        {
            Idleness = tick;
        }

        public void AddNeighbor(Vertex neighbor){
            if (neighbor != this) {
                _neighbors.Add(neighbor);
            }
        }

        public void RemoveNeighbor(Vertex neighbor) {
            _neighbors.Remove(neighbor);
        }

        public override bool Equals(object obj)
        {
            if (obj is not Vertex v) {
                return false;
            }

            return Position.Equals(v.Position);
        }

        public override int GetHashCode()
        {
            return Position.GetHashCode();
        }
    }
}
