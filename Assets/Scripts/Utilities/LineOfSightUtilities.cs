using System;

using UnityEngine;

namespace Maes.Utilities
{
    public static class LineOfSightUtilities
    {
        // Precompute visibility using an efficient grid-traversal algorithm
        public static Bitmap ComputeVisibilityOfPoint(Vector2Int start, Bitmap map)
        {
            var visibilityBitmap = new Bitmap(0, 0, map.Width, map.Height);
            for (var x = 0; x < map.Width; x++)
            {
                for (var y = 0; y < map.Height; y++)
                {
                    var target = new Vector2Int(x, y);
                    if (!map[x, y] && !visibilityBitmap.Contains(x, y))
                    {
                        GridRayTracingLineOfSight(start, target, map, visibilityBitmap);
                    }
                }
            }
            return visibilityBitmap;
        }

        // Precompute visibility using an efficient grid-traversal algorithm
        public static Bitmap ComputeVisibilityOfPoint(Vector2Int start, Bitmap map, int range)
        {
            var xEnd = Math.Min(map.Width, start.x + range + 1);
            var yEnd = Math.Min(map.Height, start.y + range + 1);

            var xStart = Math.Max(0, start.x - range - 1);
            var yStart = Math.Max(0, start.y - range - 1);

            var squaredRange = range * range;

            var visibilityBitmap = new Bitmap(xStart, yStart, xEnd, yEnd);

            for (var x = xStart; x < xEnd; x++)
            {
                for (var y = yStart; y < yEnd; y++)
                {
                    var target = new Vector2Int(x, y);
                    if (!map[x, y] && !visibilityBitmap.Contains(x, y) && squaredRange >= (start.x - x) * (start.x - x) + (start.y - y) * (start.y - y))
                    {
                        GridRayTracingLineOfSight(start, target, map, visibilityBitmap);
                    }
                }
            }

            return visibilityBitmap;
        }

        // Implements http://www.cse.yorku.ca/~amana/research/grid.pdf
        private static void GridRayTracingLineOfSight(Vector2Int start, Vector2Int end, Bitmap map, Bitmap bitmap)
        {
            var x = start.x;
            var y = start.y;

            var diffX = end.x - start.x;
            var diffY = end.y - start.y;

            var stepX = Math.Sign(diffX);
            var stepY = Math.Sign(diffY);

            var angle = Mathf.Atan2(-diffY, diffX);

            var tMaxX = 0.5f / Mathf.Cos(angle);
            var tMaxY = 0.5f / Mathf.Sin(angle);

            var tDeltaX = 1.0f / Mathf.Cos(angle);
            var tDeltaY = 1.0f / Mathf.Sin(angle);

            var manhattenDistance = Math.Abs(end.x - start.x) + Math.Abs(end.y - start.y);

            for (var t = 0; t < manhattenDistance; t++)
            {
                bitmap.Set(x, y);

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

                if (map[x, y])
                {
                    return;
                }
            }

            bitmap.Set(x, y);
        }
    }
}