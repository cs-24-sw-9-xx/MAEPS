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
using System.Runtime.CompilerServices;

using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Jobs;

using UnityEngine;

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
        private int GetVisibilityMapIndex([AssumeRange(0, int.MaxValue)] int x, [AssumeRange(0, int.MaxValue)] int y, [AssumeRange(0, int.MaxValue)] int outerY)
        {
            return GetMapIndex(x, y) + Map.Length * outerY * 64;
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
            var x = X;
            var y = outerY;

            var diffX = endX - X;
            var diffY = endY - outerY;

            var stepX = Math.Sign(diffX);
            var stepY = Math.Sign(diffY);

            var angle = Mathf.Atan2(-diffY, diffX);

            var cosAngle = Mathf.Cos(angle);
            var sinAngle = Mathf.Sin(angle);

            var tMaxX = 0.5f / cosAngle;
            var tMaxY = 0.5f / sinAngle;

            var tDeltaX = tMaxX * 2.0f;
            var tDeltaY = tMaxY * 2.0f;

            var manhattenDistance = Math.Abs(endX - X) + Math.Abs(endY - outerY);
            Hint.Assume(manhattenDistance >= 0);

            var visibilityIndex = GetVisibilityMapIndex(x, y, outerY);
            SetVisibilityMapValue(visibilityIndex);

            for (var t = 0; t < manhattenDistance; t++)
            {
                if (Mathf.Abs(tMaxX) < Mathf.Abs(tMaxY))
                {
                    tMaxX += tDeltaX;
                    x += stepX;
                }
                else
                {
                    tMaxY += tDeltaY;
                    y += stepY;
                }

                if (MaxDistance != 0f)
                {
                    // Exit if we are over MaxDistance
                    var distX = X - x;
                    var distY = outerY - y;
                    if (distX * distX + distY * distY > MaxDistance * MaxDistance)
                    {
                        return;
                    }
                }

                var mapIndex = GetMapIndex(x, y);
                if (GetMapValue(mapIndex))
                {
                    return;
                }

                visibilityIndex = GetVisibilityMapIndex(x, y, outerY);
                SetVisibilityMapValue(visibilityIndex);
            }
        }
    }
}