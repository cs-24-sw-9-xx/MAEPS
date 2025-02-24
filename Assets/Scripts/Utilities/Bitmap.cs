using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

using JetBrains.Annotations;

using Unity.Collections;

using UnityEngine;

namespace Maes.Utilities
{
    [MustDisposeResource]
    public sealed class Bitmap : IEnumerable<Vector2Int>, IDisposable, ICloneable<Bitmap>
    {
        public int Width => XEnd - XStart;
        public int Height => YEnd - YStart;

        public readonly int XStart;
        public readonly int YStart;

        public readonly int XEnd;
        public readonly int YEnd;

        private readonly int _length;

#if DEBUG
        private ulong[] _bits;
#else
        private readonly ulong[] _bits;
#endif

#if DEBUG
        public string DebugView
        {
            get
            {
                var width = XEnd;
                var height = YEnd;

                var output = new StringBuilder();

                output.AppendLine("Legend: '#': Out of bounds 'X': True ' ': False");
                output.AppendFormat("XStart: {0}, YStart: {1}, XEnd: {2}, YEnd: {3}\n", XStart, YStart, XEnd, YEnd);

                for (var y = -1; y < height + 1; y++)
                {
                    for (var x = -1; x < width + 1; x++)
                    {
                        if (x < XStart || x >= XEnd || y < YStart || y >= YEnd)
                        {
                            // Out of bounds
                            output.Append('#');
                        }
                        else
                        {
                            output.Append(Contains(x, y) ? 'X' : ' ');
                        }
                    }

                    output.Append('\n');
                }

                return output.ToString();
            }
        }
#endif

        public Bitmap(int xStart, int yStart, int xEnd, int yEnd)
        {
            XStart = xStart;
            YStart = yStart;

            XEnd = xEnd;
            YEnd = yEnd;

            // Ceil division
            _length = ((Width * Height) - 1) / 64 + 1;
            _bits = ArrayPool<ulong>.Shared.Rent(_length);
            for (var i = 0; i < _length; i++)
            {
                _bits[i] = 0;
            }
        }

        private Bitmap(int xStart, int yStart, int xEnd, int yEnd, ulong[] bits)
        {
            XStart = xStart;
            YStart = yStart;

            XEnd = xEnd;
            YEnd = yEnd;

            // Ceil division
            _length = ((Width * Height) - 1) / 64 + 1;
            _bits = bits;
        }

        [MustDisposeResource]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bitmap Intersection(Bitmap first, Bitmap second)
        {
            if (first.XStart == second.XStart && first.YStart == second.YStart && first.XEnd == second.XEnd &&
                first.YEnd == second.YEnd)
            {
                return IntersectionSameSize(first, second);
            }

            return IntersectionDifferentSizes(first, second);
        }

        [MustDisposeResource]
        private static Bitmap IntersectionSameSize(Bitmap first, Bitmap second)
        {
            var length = first._length;
            var bits = ArrayPool<ulong>.Shared.Rent(length);

            for (var i = 0; i < length; i++)
            {
                bits[i] = first._bits[i] & second._bits[i];
            }

            return new Bitmap(first.XStart, first.YStart, first.XEnd, first.YEnd, bits);
        }

        [MustDisposeResource]
        private static Bitmap IntersectionDifferentSizes(Bitmap first, Bitmap second)
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

        public NativeArray<ulong> ToNativeArray()
        {
            var nativeArray = new NativeArray<ulong>(_length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            for (var i = 0; i < _length; i++)
            {
                nativeArray[i] = _bits[i];
            }

            return nativeArray;
        }

        public static Bitmap[] FromNativeArray(int width, int height, int nativeMapLength, NativeArray<ulong> nativeArray)
        {
            var bitmapAmount = nativeArray.Length / nativeMapLength;
            var bitmaps = new Bitmap[bitmapAmount];

            for (var b = 0; b < bitmapAmount; b++)
            {
                var length = nativeArray.Length / bitmapAmount;
                var bits = ArrayPool<ulong>.Shared.Rent(length);

                for (var i = 0; i < length; i++)
                {
                    var nativeIndex = length * b + i;
                    bits[i] = nativeArray[nativeIndex];
                }

                bitmaps[b] = new Bitmap(0, 0, width, height, bits);
            }

            return bitmaps;
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

        public bool this[int x, int y]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get =>
                Contains(x, y);
        }

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
            var bitIndex = 1ul << (index % 64);
            var arrayIndex = index / 64;

            var value = _bits[arrayIndex];

            var result = value | bitIndex;

            _bits[arrayIndex] = result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Unset(int index)
        {
            var bitIndex = 1ul << (index % 64);
            var arrayIndex = index / 64;

            var value = _bits[arrayIndex];
            var result = value & ~(bitIndex);

            _bits[arrayIndex] = result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool Contains(int index)
        {
            var bitIndex = 1ul << (index % 64);
            var arrayIndex = index / 64;

            var value = _bits[arrayIndex];

            return (value & bitIndex) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExceptWith(Bitmap other)
        {
            if (XStart == other.XStart && YStart == other.YStart && XEnd == other.XEnd &&
                YEnd == other.YEnd)
            {
                ExceptWithSameSize(other);
            }
            else
            {
                ExceptWithDifferentSizes(other);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ExceptWithSameSize(Bitmap other)
        {
            for (var i = 0; i < _length; i++)
            {
                _bits[i] &= ~other._bits[i];
            }
        }

        private void ExceptWithDifferentSizes(Bitmap other)
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
                for (var i = 0; i < _length; i++)
                {
                    var value = _bits[i];
                    var result = value - ((value >> 1) & 0x5555555555555555UL);
                    result = (result & 0x3333333333333333UL) + ((result >> 2) & 0x3333333333333333UL);
                    count += (byte)(unchecked(((result + (result >> 4)) & 0xF0F0F0F0F0F0F0FUL) * 0x101010101010101UL) >> 56);
                }

                return count;
            }
        }

        public bool Any
        {
            get
            {
                for (var i = 0; i < _length; i++)
                {
                    var value = _bits[i];
                    var result = value - ((value >> 1) & 0x5555555555555555UL);
                    result = (result & 0x3333333333333333UL) + ((result >> 2) & 0x3333333333333333UL);
                    if ((byte)(unchecked(((result + (result >> 4)) & 0xF0F0F0F0F0F0F0FUL) * 0x101010101010101UL) >>
                               56) != 0)
                    {
                        return true;
                    }
                }

                return false;
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
                    if (_bitmap._length <= (++_index / 64))
                    {
                        return false;
                    }

                    if (_bitmap.Contains(_index))
                    {
                        Current = new Vector2Int(_index / _bitmap.Height + _bitmap.XStart, _index % _bitmap.Height + _bitmap.YStart);
                        return true;
                    }
                }
            }

            public void Reset()
            {
                throw new NotImplementedException();
            }

            public Vector2Int Current { get; private set; }

            object IEnumerator.Current => Current;

            public void Dispose()
            {
                // Nothing to dispose
            }
        }

        public void Dispose()
        {
            ArrayPool<ulong>.Shared.Return(_bits, clearArray: false);
#if DEBUG
            // Catch usage after dispose.
            // It would be very bad as the array may be rented out to another bitmap,
            // and now we have a reference to the array of another bitmap.
            _bits = null!;
#endif
            GC.SuppressFinalize(this);
        }

        ~Bitmap()
        {
            Dispose();
        }

        [MustDisposeResource]
        public Bitmap Clone()
        {
            var bits = ArrayPool<ulong>.Shared.Rent(_length);
            for (var i = 0; i < _length; i++)
            {
                bits[i] = _bits[i];
            }

            return new Bitmap(XStart, YStart, XEnd, YEnd, bits);
        }
    }
}