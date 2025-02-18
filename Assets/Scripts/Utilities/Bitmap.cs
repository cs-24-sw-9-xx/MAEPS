using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using UnityEngine;

namespace Maes.Utilities
{
    public sealed class Bitmap : IEnumerable<Vector2Int>
    {
        public int Width => XEnd - XStart;
        public int Height => YEnd - YStart;

        public readonly int XStart;
        public readonly int YStart;

        public readonly int XEnd;
        public readonly int YEnd;

        private readonly ulong[] _bits;

        public Bitmap(int xStart, int yStart, int xEnd, int yEnd)
        {
            XStart = xStart;
            YStart = yStart;

            XEnd = xEnd;
            YEnd = yEnd;

            // Ceil division
            var length = ((Width * Height) - 1) / 64 + 1;
            _bits = new ulong[length];
        }

        public static Bitmap Intersection(Bitmap first, Bitmap second)
        {
            var xStart = Math.Max(first.XStart, second.XStart);
            var yStart = Math.Max(first.YStart, second.YStart);

            var xEnd = Math.Min(first.XEnd, second.XEnd);
            var yEnd = Math.Min(first.YEnd, second.YEnd);

            var intersected = new Bitmap(xStart, yStart, xEnd, yEnd);
            for (var x = xStart; x < xEnd; x++)
            {
                for (var y = yStart; y < yEnd; y++)
                {
                    if (first.Contains(x, y) && second.Contains(x, y))
                    {
                        intersected.Set(x, y);
                    }
                }
            }

            return intersected;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int x, int y)
        {
            Set((x - XStart) * Height + (y - YStart));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Unset(int x, int y)
        {
            Unset((x - XStart) * Height + (y - YStart));
        }

        public bool this[int x, int y] => Contains(x, y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(Vector2Int point)
        {
            return Contains(point.x, point.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(int x, int y)
        {
            return Contains((x - XStart) * Height + (y - YStart));
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Set(int index)
        {
            _bits[index / 64] |= 1ul << index % 64;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Unset(int index)
        {
            _bits[index / 64] &= ~(1ul << index % 64);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool Contains(int index)
        {
            return (_bits[index / 64] & (1ul << (index % 64))) != 0;
        }

        public void ExceptWith(Bitmap other)
        {
            var xStart = Math.Max(XStart, other.XStart);
            var yStart = Math.Max(YStart, other.YStart);

            var xEnd = Math.Min(XEnd, other.XEnd);
            var yEnd = Math.Min(YEnd, other.YEnd);


            for (var x = xStart; x < xEnd; x++)
            {
                for (var y = yStart; y < yEnd; y++)
                {
                    if (other.Contains(x, y))
                    {
                        Unset(x, y);
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public VisibilityBitmapEnumerator GetEnumerator()
        {
            return new VisibilityBitmapEnumerator(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator<Vector2Int> IEnumerable<Vector2Int>.GetEnumerator()
        {
            return GetEnumerator();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count
        {
            get
            {
                var count = 0;
                foreach (var bits in _bits)
                {
                    // Popcount
                    // https://stackoverflow.com/a/11517887
                    var result = bits - ((bits >> 1) & 0x5555555555555555UL);
                    result = (result & 0x3333333333333333UL) + ((result >> 2) & 0x3333333333333333UL);
                    count += (int)(unchecked(((result + (result >> 4)) & 0xF0F0F0F0F0F0F0FUL) * 0x101010101010101UL) >> 56);
                }

                return count;
            }
        }

        public struct VisibilityBitmapEnumerator : IEnumerator<Vector2Int>
        {
            private readonly Bitmap _bitmap;

            private int _index;

            public VisibilityBitmapEnumerator(Bitmap bitmap)
            {
                _bitmap = bitmap;
                Current = Vector2Int.zero;

                _index = -1;
            }

            public bool MoveNext()
            {
                while (true)
                {
                    if (_bitmap._bits.Length <= (++_index / 64))
                    {
                        return false;
                    }

                    if (_bitmap.Contains(_index))
                    {
                        Current = new Vector2Int(_index % _bitmap.Width + _bitmap.XStart, _index / _bitmap.Width + _bitmap.YStart);
                        return true;
                    }
                }
            }

            public void Reset()
            {
                throw new NotImplementedException();
            }

            public Vector2Int Current { get; private set; }

            object? IEnumerator.Current => Current;

            public void Dispose()
            {
                // Nothing to dispose
            }
        }
    }
}