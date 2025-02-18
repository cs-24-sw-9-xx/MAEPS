using System.Collections;

using UnityEngine;

namespace Maes.Map.MapPatrollingGen
{
    /// <summary>
    /// Lightweight 2D bit array implementation.
    /// Value indicates whether a cell is a wall. 
    /// </summary>
    public class BitMap2D
    {
        private readonly BitArray _bits;
        public int Width { get; }
        public int Height { get; }

        public BitMap2D(int width, int height)
        {
            Width = width;
            Height = height;
            _bits = new BitArray(width * height);
        }

        public BitMap2D(bool[,] map)
        {
            Width = map.GetLength(0);
            Height = map.GetLength(1);
            _bits = new BitArray(Width * Height);
            for (var height = 0; height < Height; height++)
            {
                for (var width = 0; width < Width; width++)
                {
                    this[height, width] = map[width, height];
                }
            }
        }

        /// <summary>
        /// width should be in the inner loop for cache efficiency. 
        /// </summary>
        /// <value>bool indicating whether a cell is a wall.</value>
        public bool this[int height, int width]
        {
            get => _bits[height * Width + width];
            set => _bits[height * Width + width] = value;
        }

        // Function to check if a position is within bounds and walkable
        public bool IsWalkable(Vector2Int pos)
        {
            return pos.x >= 0 && pos.x < Width &&
                    pos.y >= 0 && pos.y < Height &&
                    !this[pos.y, pos.x]; // true if the position is not a wall
        }
    }
}