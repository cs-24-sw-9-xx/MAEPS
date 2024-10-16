#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Maes.Map.MapGen;
using UnityEngine;

namespace Maes.Map {
    public class PatrollingMap {
        public readonly IReadOnlyList<Vertex> Verticies;


        public PatrollingMap(SimulationMap<Tile> simulationMap)
        {
            var width = simulationMap.WidthInTiles;
            var height = simulationMap.HeightInTiles;

            var tiles = new SimulationMapTile<Tile>[width, height];

            var roomTiles = new List<Vector2Int>();

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var tile = simulationMap.GetTileByLocalCoordinate(x, y);
                    var firstTri = tile.GetTriangles()[0];
                    if (firstTri.Type == TileType.Wall || firstTri.Type == TileType.Brick || firstTri.Type == TileType.Concrete || firstTri.Type == TileType.Wood) {
                        continue;
                    }

                    roomTiles.Add(new Vector2Int(x, y));
                }
            }

            Verticies = CreateVerticiesFromRooms(new SplitRoom(roomTiles.ToArray()));
        }

        private static Vertex[] CreateVerticiesFromRooms(SplitRoom splitRoom) {
            var roomVerticies = new Dictionary<SplitRoom, Vertex>();
/*
            // Split all rooms
            var splitRooms = rooms.SelectMany(r => r.IsHallWay ? SplitRoomsUntilDone(new SplitRoom(r)) : new SplitRoom[] { new SplitRoom(r)}).ToList();

            // Create connecting rooms
            var connectingRooms = new List<SplitRoom>();
            for (int x = 0; x < tiles.GetLength(0); x++) {

                for (int y = 0; y < tiles.GetLength(1); y++) {
                    var firstTri = tiles[x, y].GetTriangles()[0];
                    if (firstTri.Type == TileType.Wall || firstTri.Type == TileType.Brick || firstTri.Type == TileType.Concrete || firstTri.Type == TileType.Wood) {
                        continue;
                    }

                    if (!splitRooms.Any(r => r.Tiles.Any(t => t == new Vector2Int(x, y)))) {
                        connectingRooms.Add(new SplitRoom(new Vector2Int[] { new Vector2Int(x, y)}));
                    }
                }
            }

            // Merge doorways
            for (int i = connectingRooms.Count() - 1; i >= 0; i--) {
                for (int j = i - 1; j >= 0; j--) {
                    var firstRoom = connectingRooms[i];
                    var secondRoom = connectingRooms[j];
                    if (DoRoomsConnect(firstRoom, secondRoom)) {
                        var mergedRoom = new SplitRoom(firstRoom.Tiles.Concat(secondRoom.Tiles).ToArray());
                        connectingRooms.Remove(firstRoom);
                        connectingRooms.Remove(secondRoom);
                        connectingRooms.Add(mergedRoom);
                        goto nextItem;
                    }
                }
                nextItem:;
            }

            splitRooms.AddRange(connectingRooms);
            */

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
                foreach (var (otherRoom, otherVertex) in roomVerticies) {
                    if (room == otherRoom) {
                        continue;
                    }

                    if (DoRoomsConnect(room, otherRoom)) {
                        vertex.AddNeighbor(otherVertex);
                    }
                }
            }

/*
            // Remove all connecting rooms
            foreach (var connectingRoom in connectingRooms) {
                // Connect all neighbors to each other
                var vertex = roomVerticies[connectingRoom];
                foreach (var firstNeighbor in vertex.Neighbors) {
                    foreach (var secondNeighbor in vertex.Neighbors) {
                        if (firstNeighbor == secondNeighbor) {
                            continue;
                        }

                        firstNeighbor.AddNeighbor(secondNeighbor);
                        firstNeighbor.RemoveNeighbor(vertex);
                    }
                }

                roomVerticies.Remove(connectingRoom);
            }
            */

            // Visualize all tiles
            return roomVerticies.Keys
                .Select(r => (Random.ColorHSV(0.2f, 1.0f), r))
                .SelectMany(p => p.r.Tiles.Select(t => new Vertex(1.0f, t, p.Item1)))
                .ToArray();

            //return roomVerticies.Values.ToArray();
        }

        private static Vector2Int GetCenterPointOfRoom(SplitRoom room) {
            var minX = room.Tiles.Min(t => t.x);
            var maxX = room.Tiles.Max(t => t.x);

            var minY = room.Tiles.Min(t => t.y);
            var maxY = room.Tiles.Max(t => t.y);

            return new Vector2Int((maxX - minX) / 2 + minX, (maxY - minY) / 2 + minY);
        }

        private static SplitRoom[] SplitRoomsUntilDone(SplitRoom splitRoom) {
            var splitRooms = SplitTheRoomToPieces(splitRoom);
            var count = splitRooms.Length;
            do {
                count = splitRooms.Length;
                splitRooms = splitRooms.SelectMany(r => SplitTheRoomToPieces(r)).ToArray();
            } while (splitRooms.Length != count);

            return splitRooms;
        }

        private static SplitRoom[] SplitTheRoomToPieces(SplitRoom splitRoom) {
            var nonSplittableRooms = new List<SplitRoom>();

            SplitTheRoomPlease(splitRoom);

            return nonSplittableRooms.ToArray();

            void SplitTheRoomPlease(SplitRoom splitRoom) {
                var (leftRoom, rightRoom) = SplitTheRoom(splitRoom);

                if (rightRoom == null)
                {
                    nonSplittableRooms.Add(leftRoom);
                    return;
                }

                SplitTheRoomPlease(leftRoom);
                SplitTheRoomPlease(rightRoom);
            }
        }

        private static (SplitRoom, SplitRoom?) SplitTheRoom(SplitRoom splitRoom)
        {
            var verticalSplitPoint = FindVerticalSplitPoint(splitRoom);

            if (verticalSplitPoint == null) {
                var horizontalSplitPoint = FindHorizontalSplitPoint(splitRoom);

                if (horizontalSplitPoint == null) {
                    return (splitRoom, null);
                }


                var upSplitRoom = new SplitRoom(splitRoom.Tiles.Where(t => t.y <= horizontalSplitPoint).ToArray());
                var downSplitRoom = new SplitRoom(splitRoom.Tiles.Where(t => t.y > horizontalSplitPoint).ToArray());

                return (upSplitRoom, downSplitRoom);
            }

            var leftSplitRoom = new SplitRoom(splitRoom.Tiles.Where(t => t.x <= verticalSplitPoint).ToArray());
            var rightSplitRoom = new SplitRoom(splitRoom.Tiles.Where(t => t.x > verticalSplitPoint).ToArray());

            return (leftSplitRoom, rightSplitRoom);
        }

        /// <summary>
        /// Finds the left tile where a split should happen.
        /// Returns y axis.
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        private static int? FindVerticalSplitPoint(SplitRoom splitRoom)
        {
            var potentialSplits = new HashSet<int>();

            foreach (var tile in splitRoom.Tiles)
            {
                // If no tiles are on the right side add it as potential split
                if (!splitRoom.Tiles.Any(t => t == tile + Vector2Int.right)) {
                    potentialSplits.Add(tile.x);
                }
            }

            foreach (var potentialSplit in potentialSplits) 
            {
                // If there are a tile to the right of the split
                if (splitRoom.Tiles.Any(t => t.x > potentialSplit)) {
                    return potentialSplit;
                }
            }

            return null;
        }

        private static int? FindHorizontalSplitPoint(SplitRoom splitRoom) {
            var potentialSplits = new HashSet<int>();

            foreach (var tile in splitRoom.Tiles)
            {
                // If no tiles are on the right side add it as potential split
                if (!splitRoom.Tiles.Any(t => t == tile + Vector2Int.up)) {
                    potentialSplits.Add(tile.y);
                }
            }

            foreach (var potentialSplit in potentialSplits) 
            {
                // If there are a tile to the right of the split
                if (splitRoom.Tiles.Any(t => t.y > potentialSplit)) {
                    return potentialSplit;
                }
            }

            return null;
        }

        private static bool DoRoomsConnect(SplitRoom first, SplitRoom second) {
            return first.Tiles.Any(f => second.Tiles.Any(s => DoTilesConnect(f, s)));
        }

        private static bool DoTilesConnect(Vector2Int first, Vector2Int second) {
            return Vector2Int.Distance(first, second) <= 1.0f;
        }


        private class SplitRoom {
            public readonly IReadOnlyCollection<Vector2Int> Tiles;

            public SplitRoom(Vector2Int[] tiles) {
                Tiles = tiles;
            }

            public SplitRoom(Room room) {
                Tiles = room.Tiles;
            }
        }
    }
}