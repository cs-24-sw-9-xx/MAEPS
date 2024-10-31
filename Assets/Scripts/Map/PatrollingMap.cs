#nullable enable

using System.Collections.Generic;
using System.Linq;
using Maes.Map.MapGen;
using UnityEngine;

namespace Maes.Map
{
    public class PatrollingMap
    {
        public readonly IReadOnlyList<Vertex> Verticies;

        public PatrollingMap(SimulationMap<Tile> simulationMap)
        {
            var roomTiles = new List<Vector2Int>();
            var width = simulationMap.WidthInTiles;
            var height = simulationMap.HeightInTiles;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var tile = simulationMap.GetTileByLocalCoordinate(x, y);
                    var firstTri = tile.GetTriangles()[0];
                    if (!Tile.IsWall(firstTri.Type))
                    {
                        roomTiles.Add(new Vector2Int(x, y));
                    }
                }
            }

            Verticies = CreateVerticiesFromRooms(new SplitRoom(roomTiles.ToArray(), "Start"));
        }

        private static Vertex[] CreateVerticiesFromRooms(SplitRoom splitRoom)
        {
            var roomVerticies = new Dictionary<SplitRoom, Vertex>();
            var splitRooms = SplitTheRoomToPieces(splitRoom);

            // Create a vertex for each room
            foreach (var room in splitRooms)
            {
                var centerPoint = GetCenterPointOfRoom(room);
                var vertex = new Vertex(1.0f, centerPoint);
                roomVerticies.Add(room, vertex);
            }

            // Connect rooms with each other
            foreach (var (room, vertex) in roomVerticies)
            {
                foreach (var (otherRoom, otherVertex) in roomVerticies)
                {
                    if (room == otherRoom)
                    {
                        continue;
                    }

                    if (DoRoomsConnect(room, otherRoom))
                    {
                        vertex.AddNeighbor(otherVertex);
                    }

                    if (DoRoomsOverlap(room, otherRoom))
                    {
                        Debug.LogError($"Room {room.FromWhichAlgo} overlaps with Room {otherRoom.FromWhichAlgo}");
                    }
                }
            }

            // Visualize all tiles
            // return roomVerticies.Keys
            //     .Select(r => (Random.ColorHSV(0.2f, 1.0f), r))
            //     .SelectMany(p => p.r.Tiles.Select(t => new Vertex(1.0f, t, p.Item1)))
            //     .ToArray();

            return roomVerticies.Values.ToArray();
        }

        private static Vector2Int GetCenterPointOfRoom(SplitRoom room)
        {
            var minX = room.Tiles.Min(t => t.x);
            var maxX = room.Tiles.Max(t => t.x);

            var minY = room.Tiles.Min(t => t.y);
            var maxY = room.Tiles.Max(t => t.y);

            return new Vector2Int((maxX - minX) / 2 + minX, (maxY - minY) / 2 + minY);
        }

        /// <summary>
        /// Splits the room into pieces that is connected with each other
        /// </summary>
        /// <param name="splitRoom"></param>
        /// <returns></returns>
        private static SplitRoom[] SplitTheRoomToPieces(SplitRoom splitRoom)
        {
            var nonSplittableRooms = new List<SplitRoom>();

            SplitTheRoomRecursive(splitRoom);

            return nonSplittableRooms.ToArray();

            void SplitTheRoomRecursive(SplitRoom splitRoom)
            {
                var (leftRoom, rightRoom) = SplitTheRoom(splitRoom);

                if (rightRoom == null)
                {
                    nonSplittableRooms.Add(leftRoom);
                    return;
                }

                SplitTheRoomRecursive(leftRoom);
                SplitTheRoomRecursive(rightRoom);
            }
        }

        /// <summary>
        /// Splits the room into two rooms if possible.
        /// </summary>
        /// <param name="splitRoom">The room to split</param>
        /// <returns></returns>
        private static (SplitRoom, SplitRoom?) SplitTheRoom(SplitRoom splitRoom)
        {
            var verticalSplitPointRight = RightVerticalSplit(splitRoom);
            // If there is no right vertical split, try left vertical split
            if (verticalSplitPointRight == null)
            {
                var verticalSplitPointLeft = LeftVerticalSplit(splitRoom);
                // If there is no left vertical split, try top horizontal split
                if (verticalSplitPointLeft == null)
                {
                    var horizontalSplitPoint = HorizontalSplit(splitRoom);
                    // If there is no top horizontal split, return the room as is
                    if (horizontalSplitPoint == null)
                    {
                        return (splitRoom, null);
                    }

                    // Splits the room horizontally
                    var horizonDownSplitRoom = new SplitRoom(
                        splitRoom.Tiles.Where(t => t.y <= horizontalSplitPoint).ToArray(), "Horizontal Down"
                    );
                    var horizonUpSplitRoom = new SplitRoom(
                        splitRoom.Tiles.Where(t => t.y > horizontalSplitPoint).ToArray(), "Horizontal Up"
                    );
                    return (horizonDownSplitRoom, horizonUpSplitRoom);
                }

                // Splits the room vertically left
                var leftVerticalLeftSplitRoom = new SplitRoom(
                    splitRoom.Tiles.Where(t => t.x < verticalSplitPointLeft).ToArray(), "Left Vertical Left"
                );
                var leftVerticalRightSplitRoom = new SplitRoom(
                    splitRoom.Tiles.Where(t => t.x >= verticalSplitPointLeft).ToArray(), "Left Vertical Right"
                );

                return (leftVerticalLeftSplitRoom, leftVerticalRightSplitRoom);
            }

            // Splits the room vertically right
            var rightVerticalLeftSplitRoom = new SplitRoom(
                splitRoom.Tiles.Where(t => t.x <= verticalSplitPointRight).ToArray(), "Right Vertical Left"
            );
            var rightVerticalRightSplitRoom = new SplitRoom(
                splitRoom.Tiles.Where(t => t.x > verticalSplitPointRight).ToArray(), "Right Vertical Right"
            );

            return (rightVerticalLeftSplitRoom, rightVerticalRightSplitRoom);
        }

        /// <summary>
        /// Finds the left tile where a right vertical split should happen.
        /// Returns y axis.
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        private static int? RightVerticalSplit(SplitRoom splitRoom)
        {
            var potentialSplits = new HashSet<int>();

            foreach (var tile in splitRoom.Tiles)
            {
                // If no tiles are on the right side add it as potential split
                if (!splitRoom.Tiles.Any(t => t == tile + Vector2Int.right))
                {
                    potentialSplits.Add(tile.x);
                }
            }

            foreach (var potentialSplit in potentialSplits)
            {
                // If there are a tile to the right of the split
                if (splitRoom.Tiles.Any(t => t.x > potentialSplit))
                {
                    return potentialSplit;
                }
            }

            return null;
        }

        /// <summary>
        /// Finds the left tile where a left vertical split should happen.
        /// Returns y axis.
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        private static int? LeftVerticalSplit(SplitRoom splitRoom)
        {
            var potentialSplits = new HashSet<int>();
            foreach (var tile in splitRoom.Tiles)
            {
                if (!splitRoom.Tiles.Any(t => t == tile + Vector2Int.left))
                {
                    potentialSplits.Add(tile.x);
                }
            }
            foreach (var potentialSplit in potentialSplits)
            {
                if (splitRoom.Tiles.Any(t => t.x < potentialSplit))
                {
                    return potentialSplit;
                }
            }
            return null;
        }

        /// <summary>
        /// Finds the top tile where a horizontal split should happen.
        /// </summary>
        /// <param name="splitRoom"></param>
        /// <returns></returns>
        private static int? HorizontalSplit(SplitRoom splitRoom)
        {
            var potentialSplits = new HashSet<int>();

            foreach (var tile in splitRoom.Tiles)
            {
                if (!splitRoom.Tiles.Any(t => t == tile + Vector2Int.up))
                {
                    potentialSplits.Add(tile.y);
                }
            }

            foreach (var potentialSplit in potentialSplits)
            {
                if (splitRoom.Tiles.Any(t => t.y > potentialSplit))
                {
                    return potentialSplit;
                }
            }

            return null;
        }

        private static bool DoRoomsConnect(SplitRoom first, SplitRoom second)
        {
            return first.Tiles.Any(f => second.Tiles.Any(s => DoTilesConnect(f, s)));
        }

        private static bool DoTilesConnect(Vector2Int first, Vector2Int second)
        {
            return Vector2Int.Distance(first, second) <= 1.0f;
        }

        private static bool DoRoomsOverlap(SplitRoom first, SplitRoom second)
        {
            return first.Tiles.Any(f => second.Tiles.Any(s => f == s));
        }

        private class SplitRoom
        {
            public readonly IReadOnlyCollection<Vector2Int> Tiles;

            public readonly string FromWhichAlgo;

            public SplitRoom(Vector2Int[] tiles, string algo)
            {
                Tiles = tiles;
                FromWhichAlgo = algo;
            }
        }
    }
}
