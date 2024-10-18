#nullable enable

using System.Collections.Generic;
using UnityEngine;


namespace Maes.Map {
    public class Vertex
    {
        private HashSet<Vertex> _neighbors = new HashSet<Vertex>();
        private float _weight;
        private int _idleness;
        private Vector2Int _position;
        private int _totalIdleness;
        private int _visits;
        private int _maxIdleness;

        private Color _color;

        public Vertex(float weight, Vector2Int position, Color? color = null)
        {
            _weight = weight;
            _position = position;
            _color = color ?? Color.green;
        }

        public IReadOnlyCollection<Vertex> Neighbors
        {
            get { return _neighbors; }
        }

        public float Weight
        {
            get { return _weight; }
        }

        public int Idleness
        {
            get { return _idleness; }
        }

        public Vector2Int Position
        {
            get { return _position; }
        }

        public float AverageIdleness
        {
            get { return (float)_totalIdleness / (float)_visits; }
        }

        public Color Color => _color;

        public void ResetIdleness()
        {
            _visits++;
            _totalIdleness += _idleness;

            if (_idleness > _maxIdleness)
            {
                _maxIdleness = _idleness;
            }
            _idleness = 0;
        }

        public void AddNeighbor(Vertex neighbor){
            if (neighbor != this) {
                _neighbors.Add(neighbor);
            }
        }

        public void RemoveNeighbor(Vertex neighbor) {
            _neighbors.Remove(neighbor);
        }

        public void UpdateIdleness(){
            _idleness++;
        }

        public override bool Equals(object obj)
        {
            if (obj is not Vertex v) {
                return false;
            }

            return _position.Equals(v._position);
        }

        public override int GetHashCode()
        {
            return _position.GetHashCode();
        }
    }
}
