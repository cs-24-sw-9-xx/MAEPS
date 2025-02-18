using System;

using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

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
        public NativeArray<uint4> Map;

        [NoAlias]
        public NativeArray<uint4> Visibility;

        public void Execute()
        {
            Log();

            for (var outerY = 0; outerY < Height; outerY++)
            {
                for (var x = 0; x < Width; x++)
                {
                    for (var y = 0; y < Height; y++)
                    {
                        var index = GetIndex(x, y);
                        if (!GetValue(index))
                        {
                            GridRayTracingLineOfSight(x, y, outerY);
                        }
                    }
                }
            }
        }

        [BurstDiscard]
        private void Log()
        {
            Debug.Log("NOT RUNNING IN BURST MODE!!?=!?!?!??");
        }

        private int GetIndex(int x, int y)
        {
            return x * Height + y;
        }

        private int GetIndex(int x, int y, int outerY)
        {
            return GetIndex(x, y) + Map.Length * outerY * 128;
        }

        private bool GetValue(int index)
        {
            var bitIndex = 1u << (index % 32);
            var innerIndex = (index / 32) % 4;
            var arrayIndex = index / 128;

            var value = Map[arrayIndex];
            var mask = new uint4(innerIndex == 0 ? uint.MaxValue : uint.MinValue, innerIndex == 1 ? uint.MaxValue : uint.MinValue, innerIndex == 2 ? uint.MaxValue : uint.MinValue, innerIndex == 3 ? uint.MaxValue : uint.MinValue);

            var result = value & mask & bitIndex;

            return (result.x | result.y | result.z | result.w) != 0;
        }

        private void SetValue(int index)
        {
            var bitIndex = 1u << (index % 32);
            var innerIndex = (index / 32) % 4;
            var arrayIndex = index / 128;

            var value = Visibility[arrayIndex];
            var mask = new uint4(innerIndex == 0 ? uint.MaxValue : uint.MinValue, innerIndex == 1 ? uint.MaxValue : uint.MinValue, innerIndex == 2 ? uint.MaxValue : uint.MinValue, innerIndex == 3 ? uint.MaxValue : uint.MinValue);

            var result = value | mask & bitIndex;

            Visibility[arrayIndex] = result;
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

            for (var t = 0; t < manhattenDistance + 1; t++)
            {
                var mapIndex = GetIndex(x, y);
                if (GetValue(mapIndex))
                {
                    return;
                }

                var visibilityIndex = GetIndex(x, y, outerY);
                SetValue(visibilityIndex);
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
            }
        }
    }
}