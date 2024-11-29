using System.Collections.Generic;

using UnityEngine;

namespace Maes.Utilities
{
    public class LineOfSightUtilities
    {
        // Precompute visibility using an efficient line-drawing algorithm
        public static HashSet<Vector2Int> ComputeVisibilityOfPoint(Vector2Int start, bool[,] map)
        {
            var visibilitySet = new HashSet<Vector2Int>();
            for (var x = 0; x < map.GetLength(0); x++)
            {
                for (var y = 0; y < map.GetLength(1); y++)
                {
                    var target = new Vector2Int(x, y);
                    if (!map[x, y] && IsInLineOfSight(start, target, map))
                    {
                        visibilitySet.Add(target);
                    }
                }
            }
            return visibilitySet;
        }

        // Method to check visibility using a line-of-sight algorithm
        private static bool IsInLineOfSight(Vector2Int start, Vector2Int end, bool[,] map)
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
                if (map[x, y])
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
        public static HashSet<Vector2Int> ComputeVisibilityOfPointFastBreakColumn(Vector2Int start, bool[,] map)
        {
            var visibilitySet = new HashSet<Vector2Int>();

            // Traverse columns in both directions
            TraverseColumn(start, map, visibilitySet, 1);  // Right direction
            TraverseColumn(start, map, visibilitySet, -1); // Left direction

            return visibilitySet;
        }

        // Traverses the map column by column, pruning the search based on visibility
        private static void TraverseColumn(Vector2Int start, bool[,] map, HashSet<Vector2Int> visibilitySet, int direction)
        {
            for (var x = start.x; x >= 0 && x < map.GetLength(0); x += direction)
            {
                var resultInCurrentColumn = false;
                for (var y = 0; y < map.GetLength(1); y++)
                {
                    var target = new Vector2Int(x, y);

                    if (!map[x, y] && IsInLineOfSight(start, target, map))
                    {
                        visibilitySet.Add(target);
                        resultInCurrentColumn = true;
                    }
                }

                // Stop if the current column has no visible tiles
                if (!resultInCurrentColumn && x != start.x)
                {
                    break;
                }
            }
        }
    }
}