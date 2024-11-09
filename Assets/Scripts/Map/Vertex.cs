using System;
using System.Collections.Generic;
using UnityEngine;


namespace Maes.Map {
    public class Vertex : ICloneable
    {
        private readonly HashSet<Vertex> _neighbors = new();
        public float Weight { get; }
        public int LastTimeVisitedTick { get; private set; }
        public Vector2Int Position { get; }
        public Color Color { get; }
        public int NumberOfVisits { get; private set; }

        public Vertex(float weight, Vector2Int position, Color? color = null)
        {
            Weight = weight;
            Position = position;
            Color = color ?? Color.green;
        }
        
        public IReadOnlyCollection<Vertex> Neighbors => _neighbors;
        
        public void VisitedAtTick(int tick)
        {
            LastTimeVisitedTick = tick;
            NumberOfVisits++;
        }

        public void AddNeighbor(Vertex neighbor){
            if (!Equals(neighbor, this)) {
                _neighbors.Add(neighbor);
            }
        }

        public void RemoveNeighbor(Vertex neighbor) {
            _neighbors.Remove(neighbor);
        }

        public object Clone()
        {
            var vertex = new Vertex(Weight, Position, Color);
            foreach (var neighbor in _neighbors)
            {
                vertex.AddNeighbor(neighbor);
            }
            
            return vertex;
        }
    }
}
