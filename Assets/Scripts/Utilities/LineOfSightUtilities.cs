using System.Collections.Generic;

using Maes.Map.MapPatrollingGen;

using UnityEngine;

namespace Maes.Utilities
{
    public static class LineOfSightUtilities
    {
        // Precompute visibility using an efficient line-drawing algorithm
        public static HashSet<Vector2Int> ComputeVisibilityOfPoint(Vector2Int start, BitMap2D map)
        {
            var visibilitySet = new HashSet<Vector2Int>();
            for (var height = 0; height < map.Height; height++)
            {
                for (var width = 0; width < map.Width; width++)
                {
                    var target = new Vector2Int(width, height);
                    if (!map[height, width] && IsInLineOfSight(start, target, map))
                    {
                        visibilitySet.Add(target);
                    }
                }
            }
            return visibilitySet;
        }

        // Method to check visibility using a line-of-sight algorithm
        private static bool IsInLineOfSight(Vector2Int start, Vector2Int end, BitMap2D map)
        {
            // Implement Bresenham's line algorithm for visibility check
            // Return true if there is a clear line-of-sight, otherwise false
            var dx = Mathf.Abs(end.x - start.x);
            var dy = Mathf.Abs(end.y - start.y);
            var sx = start.x < end.x ? 1 : -1;
            var sy = start.y < end.y ? 1 : -1;
            var err = dx - dy;

            var x = start.x;
            var y = start.y;

            while (x != end.x || y != end.y)
            {
                if (map[y, x])
                {
                    return false; // Hit a wall
                }

                var e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y += sy;
                }
            }

            return true;
        }

        // Precompute visibility using an efficient line-drawing algorithm
        public static HashSet<Vector2Int> ComputeVisibilityOfPointFastBreak(Vector2Int start, BitMap2D map)
        {
            var visibilitySet = new HashSet<Vector2Int>();

            TraverseHalf(start, map, visibilitySet, 1);  // up direction
            TraverseHalf(start, map, visibilitySet, -1); // down direction

            return visibilitySet;
        }

        // Traverses the map in one direction(up/down) from the starting point, pruning the search based on visibility
        private static void TraverseHalf(Vector2Int start, BitMap2D map, HashSet<Vector2Int> visibilitySet, int direction)
        {
            for (var height = start.y; height >= 0 && height < map.Height; height += direction)
            {
                var resultInLastIteration = false;
                for (var width = 0; width < map.Width; width++)
                {
                    var target = new Vector2Int(width, height);

                    if (!map[height, width] && IsInLineOfSight(start, target, map))
                    {
                        visibilitySet.Add(target);
                        resultInLastIteration = true;
                    }
                }

                // Stop if the current iteration has no visible tiles
                if (!resultInLastIteration && height != start.y)
                {
                    break;
                }
            }
        }
    }
}