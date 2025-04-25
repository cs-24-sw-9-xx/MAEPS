// Copyright 2025 MAEPS
// 
// This file is part of MAEPS
// 
// MAEPS is free software: you can redistribute it and/or modify it under
// the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your option)
// any later version.
// 
// MAEPS is distributed in the hope that it will be useful, but WITHOUT
// ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
// or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General
// Public License for more details.
// 
// You should have received a copy of the GNU General Public License along
// with MAEPS. If not, see http://www.gnu.org/licenses/.
// 
// Contributors: Mads Beyer Mogensen

using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

using JetBrains.Annotations;

using Unity.Collections;
using Unity.Mathematics;

using UnityEngine;

namespace Maes.Utilities
{
    /// <summary>
    /// An efficient 2D array of tightly packed booleans.
    /// </summary>
    [MustDisposeResource]
    public sealed class Bitmap : IEnumerable<Vector2Int>, IDisposable, ICloneable<Bitmap>, IEquatable<Bitmap>
    {
        public readonly int Width;
        public readonly int Height;

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
                var width = Width;
                var height = Height;

                var output = new StringBuilder();

                output.AppendLine("Legend: '#': Out of bounds 'X': True ' ': False");
                output.AppendFormat("Width: {0}, Height: {1}\n", Width, Height);

                for (var y = -1; y < height + 1; y++)
                {
                    for (var x = -1; x < width + 1; x++)
                    {
                        if (x < 0 || x >= Width || y < 0 || y >= Height)
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

        /// <summary>
        /// Creates a new instance of <see cref="Bitmap"/>.
        /// </summary>
        /// <param name="width">The exclusive maximum x-coordinate of the bitmap.</param>
        /// <param name="height">The exclusive maximum y-coordinate of the bitmap.</param>
        public Bitmap(int width, int height)
        {
            Width = width;
            Height = height;

            // Ceil division
            _length = ((Width * Height) - 1) / 64 + 1;
            _bits = ArrayPool<ulong>.Shared.Rent(_length);
            for (var i = 0; i < _length; i++)
            {
                _bits[i] = 0;
            }
        }

        private Bitmap(int width, int height, ulong[] bits)
        {
            Width = width;
            Height = height;

            // Ceil division
            _length = ((Width * Height) - 1) / 64 + 1;
            _bits = bits;
        }

        [MustDisposeResource]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bitmap Intersection(Bitmap first, Bitmap second)
        {
            if (first.Width == second.Width && first.Height == second.Height)
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

            return new Bitmap(first.Width, first.Height, bits);
        }

        [MustDisposeResource]
        private static Bitmap IntersectionDifferentSizes(Bitmap first, Bitmap second)
        {
            var width = Math.Min(first.Width, second.Width);
            var height = Math.Min(first.Height, second.Height);

            var intersected = new Bitmap(width, height);
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
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
            NativeArray<ulong>.Copy(_bits, nativeArray, _length);

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

                NativeArray<ulong>.Copy(nativeArray, length * b, bits, 0, length);

                bitmaps[b] = new Bitmap(width, height, bits);
            }

            return bitmaps;
        }

        /// <summary>
        /// Set the bit at position (x,y) to true.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int x, int y)
        {
            Set(x * Height + y);
        }

        /// <summary>
        /// Set the bit at position (x,y) to false.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Unset(int x, int y)
        {
            Unset(x * Height + y);
        }

        /// <summary>
        /// Whether the bit at <paramref name="point"/> is set.
        /// </summary>
        /// <returns><see langword="true"/> if the bit is set otherwise <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(Vector2Int point)
        {
            return Contains(point.x, point.y);
        }

        /// <summary>
        /// Whether the bit at (x,y) is set.
        /// </summary>
        /// <returns><see langword="true"/> if the bit is set otherwise <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(int x, int y)
        {
            return Contains(x * Height + y);
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
        public void Union(Bitmap other)
        {
            if (Width == other.Width && Height == other.Height)
            {
                UnionSameSize(other);
            }
            else
            {
                UnionDifferentSizes(other);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UnionSameSize(Bitmap other)
        {
            for (var i = 0; i < _length; i++)
            {
                _bits[i] |= other._bits[i];
            }
        }

        private void UnionDifferentSizes(Bitmap other)
        {
            var width = Math.Min(Width, other.Width);
            var height = Math.Min(Height, other.Height);


            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    if (other.Contains(x, y))
                    {
                        Set(x, y);
                    }
                }
            }
        }

        /// <summary>
        /// Unset all bits where they are set in <paramref name="other"/>.
        /// </summary>
        /// <param name="other"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExceptWith(Bitmap other)
        {
            if (Width == other.Width && Height == other.Height)
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
            var width = Math.Min(Width, other.Width);
            var height = Math.Min(Height, other.Height);

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
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

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator<Vector2Int> IEnumerable<Vector2Int>.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Gets how many bits are set.
        /// </summary>
        public int Count
        {
            get
            {
                var count = 0;
                for (var i = 0; i < _length; i++)
                {
                    count += math.countbits(_bits[i]);
                }

                return count;
            }
        }

        /// <summary>
        /// Whether any bit is set.
        /// </summary>
        public bool Any
        {
            get
            {
                for (var i = 0; i < _length; i++)
                {
                    if (math.countbits(_bits[i]) > 0)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Enumerates a Bitmap returning a <see cref="Vector2Int"/> for each bit set.
        /// </summary>
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
                        Current = new Vector2Int(_index / _bitmap.Height, _index % _bitmap.Height);
                        return true;
                    }
                }
            }

            public void Reset()
            {
                throw new NotImplementedException();
            }

            /// <inheritdoc />
            public Vector2Int Current { get; private set; }

            /// <inheritdoc />
            object IEnumerator.Current => Current;

            public void Dispose()
            {
                // Nothing to dispose
            }
        }

        public void Dispose()
        {
            // Return the borrowed array to the pool
            ArrayPool<ulong>.Shared.Return(_bits, clearArray: false);
#if DEBUG
            // Catch usage after dispose.
            // It would be very bad as the array may be rented out to another bitmap,
            // and now we have a reference to the array of another bitmap.
            _bits = null!;
#endif
            // The destructor (finalizer) runs dispose so tell the runtime to not run the destructor, as we have already done that.
            GC.SuppressFinalize(this);
        }

        ~Bitmap()
        {
            Dispose();
        }

        /// <inheritdoc />
        public bool Equals(Bitmap other)
        {
            if (Width != other.Width || Height != other.Height)
            {
                return false;
            }

            for (var i = 0; i < _length; i++)
            {
                if (_bits[i] != other._bits[i])
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object? obj)
        {
            if (obj is Bitmap other)
            {
                return Equals(other);
            }

            return false;
        }

        /// <inheritdoc />
        [MustDisposeResource]
        public Bitmap Clone()
        {
            var bits = ArrayPool<ulong>.Shared.Rent(_length);
            Array.Copy(_bits, bits, _length);

            return new Bitmap(Width, Height, bits);
        }
    }
}