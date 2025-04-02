using System;
using System.Collections.Generic;

using UnityEngine;


namespace Maes.Map
{
    public sealed class Vertex
    {
        private readonly HashSet<Vertex> _neighbors = new();

        public int Id { get; }

        public int Partition { get; set; }

        public Vector2Int Position { get; }

        public Color Color { get; set; }

        public IReadOnlyCollection<Vertex> Neighbors => _neighbors;

        public int LastTimeVisitedTick { get; private set; }

        public int NumberOfVisits { get; private set; }


        public Vertex(int id, Vector2Int position, int partition = 1, Color? color = null)
        {
            Id = id;
            Partition = partition;
            Position = position;
            Color = color ?? Color.green;
            LastTimeVisitedTick = 0;
            NumberOfVisits = 0;
        }

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