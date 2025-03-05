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

        /// <summary>
        /// Initializes a new instance of the <see cref="Vertex"/> class with the specified identifier, weight, position, partition, and color.
        /// The vertex's visit counters are set to zero and its neighbor collection is initialized as empty.
        /// </summary>
        /// <param name="id">A unique identifier for the vertex.</param>
        /// <param name="weight">The weight associated with the vertex.</param>
        /// <param name="position">The vertex's position represented as a <see cref="Vector2Int"/>.</param>
        /// <param name="partition">The partition identifier for the vertex. Defaults to 0.</param>
        /// <param name="color">Optional color for the vertex; if null, defaults to green.</param>
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