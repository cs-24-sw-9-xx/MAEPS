using System;
using System.Collections.Generic;

using UnityEngine;


namespace Maes.Map
{
    public class Vertex
    {
        private readonly HashSet<Vertex> _neighbors;
        public int Id { get; }
        public float Weight { get; }
        public int LastTimeVisitedTick { get; private set; }
        public Vector2Int Position { get; }
        public Color Color { get; set; }
        public int NumberOfVisits { get; private set; }

        public int Partition { get; set; }

        public Vertex(int id, float weight, Vector2Int position, int partition = 0, Color? color = null)
        {
            Id = id;
            Weight = weight;
            Position = position;
            Color = color ?? Color.green;
            LastTimeVisitedTick = 0;
            NumberOfVisits = 0;
            Partition = partition;

            _neighbors = new();
        }

        public IReadOnlyCollection<Vertex> Neighbors => _neighbors;

        public void VisitedAtTick(int tick)
        {
            LastTimeVisitedTick = tick;
            NumberOfVisits++;
        }

        public void AddNeighbor(Vertex neighbor)
        {
#if DEBUG
            if (Equals(neighbor))
            {
                throw new InvalidOperationException("Cannot add self as neighbor");
            }
#endif

            _neighbors.Add(neighbor);
        }

        public void RemoveNeighbor(Vertex neighbor)
        {
            _neighbors.Remove(neighbor);
        }

        public override string ToString()
        {
            return $"Vertex {Id} @{Position}";
        }
    }
}