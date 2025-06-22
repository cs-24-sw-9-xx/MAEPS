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

using System.Runtime.CompilerServices;

using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Maes.Utilities
{
    [BurstCompile(FloatPrecision.Low, FloatMode.Default, CompileSynchronously = true, DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
    public struct VisibilityJob : IJob
    {
        [ReadOnly]
        public bool InaccurateButFast;

        [ReadOnly]
        public int Width;

        [ReadOnly]
        public int Height;

        [ReadOnly]
        public int X;

        [ReadOnly]
        public float MaxDistance;

        [NoAlias]
        [ReadOnly]
        public NativeArray<ulong> Map;

        [NoAlias]
        public NativeArray<ulong> Visibility;

        public void Execute()
        {
            Hint.Assume(Height > 0);
            Hint.Assume(Width > 0);
            Hint.Assume(X >= 0);
            Hint.Assume(MaxDistance >= 0f);

            var heightIndex = X * Height;

            for (var outerY = 0; outerY < Height; outerY++)
            {
                var index = heightIndex + outerY;
                if (Hint.Unlikely(GetMapValue(index)))
                {
                    continue;
                }

                if (InaccurateButFast)
                {
                    // Top row
                    for (var x = 1; x < Width - 1; x++)
                    {
                        GridRayTracingLineOfSight(x, 0, outerY);
                    }

                    // Bottom row
                    for (var x = 1; x < Width - 1; x++)
                    {
                        GridRayTracingLineOfSight(x, Height - 1, outerY);
                    }

                    // Left column
                    for (var y = 0; y < Height; y++)
                    {
                        GridRayTracingLineOfSight(0, y, outerY);
                    }

                    // Right column
                    for (var y = 0; y < Height; y++)
                    {
                        GridRayTracingLineOfSight(Width - 1, y, outerY);
                    }
                }
                else
                {
                    for (var x = 0; x < Width; x++)
                    {
                        for (var y = 0; y < Height; y++)
                        {
                            var innerIndex = GetMapIndex(x, y);
                            if (Hint.Likely(!GetMapValue(innerIndex)))
                            {
                                GridRayTracingLineOfSight(x, y, outerY);
                            }
                        }
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [return: AssumeRange(0, int.MaxValue)]
        private int GetMapIndex([AssumeRange(0, int.MaxValue)] int x, [AssumeRange(0, int.MaxValue)] int y)
        {
            return x * Height + y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [return: AssumeRange(0, int.MaxValue)]
        private int GetMapIndex(int2 pos)
        {
            return pos.x * Height + pos.y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [return: AssumeRange(0, int.MaxValue)]
        private int GetVisibilityMapIndex([AssumeRange(0, int.MaxValue)] int x, [AssumeRange(0, int.MaxValue)] int y, [AssumeRange(0, int.MaxValue)] int outerY)
        {
            return GetMapIndex(x, y) + Map.Length * outerY * 64;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [return: AssumeRange(0, int.MaxValue)]
        private int GetVisibilityMapIndex(int2 pos, [AssumeRange(0, int.MaxValue)] int outerY)
        {
            return GetMapIndex(pos) + Map.Length * outerY * 64;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool GetMapValue([AssumeRange(0, int.MaxValue)] int index)
        {
            return (Map[index >> 6] & (1ul << (index & 63))) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetVisibilityMapValue([AssumeRange(0, int.MaxValue)] int index)
        {
            Visibility[index >> 6] |= (1ul << (index & 63));
        }

        private void GridRayTracingLineOfSight([AssumeRange(0, int.MaxValue)] int endX, [AssumeRange(0, int.MaxValue)] int endY, [AssumeRange(0, int.MaxValue)] int outerY)
        {
            var pos = new int2(X, outerY);
            var end = new int2(endX, endY);

            var diff = end - pos;

            var step = math.sign(diff);

            var delta = math.abs(diff);
            var tDelta = math.rcp(delta);
            var tMax = tDelta * 0.5f;

            var manhattenDistance = math.csum(math.abs(diff));
            Hint.Assume(manhattenDistance >= 0);

            SetVisibilityMapValue(GetVisibilityMapIndex(pos, outerY));

            for (var t = 0; t < manhattenDistance; t++)
            {
                var selectX = math.abs(tMax.x) < math.abs(tMax.y);
                tMax += math.select(new float2(0f, tDelta.y), new float2(tDelta.x, 0f), selectX);
                pos += math.select(new int2(0, step.y), new int2(step.x, 0), selectX);

                if (MaxDistance != 0f)
                {
                    // Exit if we are over MaxDistance
                    var distX = X - pos.x;
                    var distY = outerY - pos.y;
                    if (distX * distX + distY * distY > MaxDistance * MaxDistance)
                    {
                        return;
                    }
                }

                if (GetMapValue(GetMapIndex(pos)))
                {
                    return;
                }

                SetVisibilityMapValue(GetVisibilityMapIndex(pos, outerY));
            }
        }
    }
}