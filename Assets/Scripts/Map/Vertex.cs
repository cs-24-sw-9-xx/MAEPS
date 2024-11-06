#nullable enable

using System;
using System.Collections.Generic;

using UnityEngine;


namespace Maes.Map
{
    public class Vertex : ICloneable
    {
        private readonly HashSet<Vertex> _neighbors = new HashSet<Vertex>();
        public float Weight { get; }
        public int LastTimeVisitedTick { get; private set; }
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
            LastTimeVisitedTick = tick;
        }

        public void AddNeighbor(Vertex neighbor)
        {
            if (!Equals(neighbor, this))
            {
                _neighbors.Add(neighbor);
            }
        }

        public void RemoveNeighbor(Vertex neighbor)
        {
            _neighbors.Remove(neighbor);
        }

        public override bool Equals(object obj)
        {
            if (obj is not Vertex v)
            {
                return false;
            }

            return Position.Equals(v.Position);
        }

        public override int GetHashCode()
        {
            return Position.GetHashCode();
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