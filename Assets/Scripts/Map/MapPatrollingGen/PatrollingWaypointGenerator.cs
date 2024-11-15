using System.Collections.Generic;
using System.Linq;

using DelaunatorSharp;

using Maes.Map.MapGen;

using UnityEngine;

namespace Maes.Map.MapPatrollingGen
{
    public class PatrollingWaypointGenerator
    {
        public static List<Vertex> GetPossibleWaypoints(SimulationMap<Tile> simulationMap)
        {
            // Get all wall tiles
            var wallTiles = GetWallsTiles(simulationMap);

            // The delaunator library requires a list of IPoint, so we convert the Vector2Int to IPoint
            var points = new List<IPoint>();

            // The centerCoordinatePoints will be used to generate the centerpoints of the delaunator triangles
            var centerCoordinatePoints = new List<Vertex>();

            foreach (var wallTile in wallTiles)
            {
                if (IsCornerTile(wallTile, wallTiles))
                {
                    points.Add(new Point(wallTile.x, wallTile.y));
                }
            }

            var delaunator = new Delaunator(points.ToArray());
            for (var i = 0; i < delaunator.Triangles.Length; i++)
            {
                var triangle = delaunator.Triangles[i];
                var centerpoint = delaunator.GetCentroid(triangle);
                centerCoordinatePoints.Add(new Vertex(i, 0, new Vector2Int((int)centerpoint.X, (int)centerpoint.Y)));
            }

            //TODO: Connect neighboring centerpoints with edges, currently only the centerpoints are generated
            return centerCoordinatePoints;
        }
        private static List<Vector2Int> GetWallsTiles(SimulationMap<Tile> simulationMap)
        {
            var wallTiles = new List<Vector2Int>();
            var width = simulationMap.WidthInTiles;
            var height = simulationMap.HeightInTiles;

            // The outer wall is two tiles thick, so we start at 1 and end at width-1 and height-1
            // we asume that the walls are one tile thick
            for (var x = 0; x <= width; x++)
            {
                for (var y = 0; y <= height; y++)
                {
                    var tile = simulationMap.GetTileByLocalCoordinate(x, y);
                    var firstTri = tile.GetTriangles()[0];
                    if (Tile.IsWall(firstTri.Type))
                    {
                        wallTiles.Add(new Vector2Int(x, y));
                    }
                }
            }

            return wallTiles;
        }

        /// <summary>
        /// Checks if the tile is a corner tile. We asume the walls are one tile thick
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="tiles"></param>
        /// <returns></returns>
        private static bool IsCornerTile(Vector2Int tile, List<Vector2Int> tiles)
        {
            // Horizontal grid check
            if (tiles.Any(t => t == tile + Vector2Int.right)
            && tiles.Any(t => t == tile + Vector2Int.left)
            && tiles.All(t => t != tile + Vector2Int.up)
            && tiles.All(t => t != tile + Vector2Int.down))
            {
                return false;
            }

            // Vertical grid check
            if (tiles.Any(t => t == tile + Vector2Int.up)
            && tiles.Any(t => t == tile + Vector2Int.down)
            && tiles.All(t => t != tile + Vector2Int.right)
            && tiles.All(t => t != tile + Vector2Int.left))
            {
                return false;
            }

            // Returns true otherwise due to the fact that it is a corner tile
            return true;
        }
    }
}