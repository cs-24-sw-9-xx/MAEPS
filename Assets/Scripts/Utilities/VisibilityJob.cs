using System;
using System.Runtime.CompilerServices;

using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

using UnityEngine;

namespace Maes.Utilities
{
    [BurstCompile(CompileSynchronously = true)]
    public struct VisibilityJob : IJob
    {
        [NoAlias]
        [ReadOnly]
        public int Width;

        [NoAlias]
        [ReadOnly]
        public int Height;

        [NoAlias]
        [ReadOnly]
        public int X;

        [NoAlias]
        [ReadOnly]
        public NativeArray<ulong> Map;

        [NoAlias]
        public NativeArray<ulong> Visibility;

        public void Execute()
        {
            for (var outerY = 0; outerY < Height; outerY++)
            {
                for (var x = 0; x < Width; x++)
                {
                    for (var y = 0; y < Height; y++)
                    {
                        var index = GetMapIndex(x, y);
                        if (!GetMapValue(index))
                        {
                            GridRayTracingLineOfSight(x, y, outerY);
                        }
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetMapIndex(int x, int y)
        {
            return x * Height + y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetVisibilityMapIndex(int x, int y, int outerY)
        {
            return GetMapIndex(x, y) + Map.Length * outerY * 64;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool GetMapValue(int index)
        {
            return (Map[index >> 6] & 1ul << (index & 63)) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool GetVisibilityMapValue(int index)
        {
            return (Visibility[index >> 6] & 1ul << (index & 63)) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetVisibilityMapValue(int index)
        {
            Visibility[index >> 6] |= (1ul << (index & 63));
        }

        private void GridRayTracingLineOfSight(int endX, int endY, int outerY)
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

            var tDeltaX = 1.0f / cosAngle;
            var tDeltaY = 1.0f / sinAngle;

            var manhattenDistance = Math.Abs(endX - X) + Math.Abs(endY - outerY);

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