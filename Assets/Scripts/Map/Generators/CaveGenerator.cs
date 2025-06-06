// Copyright 2024 MAES
// 
// This file is part of MAES
// 
// MAES is free software: you can redistribute it and/or modify it under
// the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your option)
// any later version.
// 
// MAES is distributed in the hope that it will be useful, but WITHOUT
// ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
// or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General
// Public License for more details.
// 
// You should have received a copy of the GNU General Public License along
// with MAES. If not, see http://www.gnu.org/licenses/.
// 
// Contributors: Rasmus Borrisholt Schmidt, Andreas Sebastian Sørensen, Thor Beregaard, Malte Z. Andreasen, Philip I. Holler and Magnus K. Jensen,
// 
// Original repository: https://github.com/Molitany/MAES

using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using Quaternion = UnityEngine.Quaternion;
using Random = System.Random;
using Vector3 = UnityEngine.Vector3;

namespace Maes.Map.Generators
{
    public class CaveGenerator : MapGenerator
    {
        private CaveMapConfig _caveConfig;
        private float _wallHeight;

        /// <summary>
        /// Generates a cave map using the unity game objects Plane, InnerWalls and WallRoof.
        /// </summary>
        /// <param name="config">Determines how the cave map should look</param>
        /// <param name="wallHeight">A smaller wall height can make it easier to see the robots. Must be a positive value.</param>
        /// <returns> A SimulationMap represents a map of square tiles, where each tile is divided into 8 triangles as
        /// used in the Marching Squares Algorithm.</returns>
        public SimulationMap<Tile> GenerateCaveMap(CaveMapConfig config, float wallHeight = 2.0f)
        {
            var timeStart = Time.realtimeSinceStartup;

            _caveConfig = config;
            _wallHeight = wallHeight;
            var random = new Random(_caveConfig.RandomSeed);
            // Clear and destroy objects from previous map
            ClearMap();

            var collisionMap = CreateCaveMapWithMesh(_caveConfig, random, _wallHeight);

            ResizePlaneToFitMap(_caveConfig.BitMapHeight, _caveConfig.BitMapWidth);

            MovePlaneAndWallRoofToFitWallHeight(_wallHeight);

            Debug.LogFormat("GenerateCaveMap took {0}s (MeshGenerator is included)", Time.realtimeSinceStartup - timeStart);

            return collisionMap;
        }

        private SimulationMap<Tile> CreateCaveMapWithMesh(CaveMapConfig caveConfig, Random random, float wallHeight = 2.0f)
        {
            // Fill map with random walls and empty tiles (Looks kinda like a QR code)
            var randomlyFilledMap = CreateRandomFillMap(caveConfig, random);

            // Use smoothing runs to make sense of the noise
            // f.x. walls can only stay walls, if they have at least N neighbouring walls
            var smoothedMap = randomlyFilledMap;
            for (var i = 0; i < caveConfig.SmoothingRuns; i++)
            {
                smoothedMap = SmoothMap(smoothedMap, caveConfig, random);
            }

            var smoothedMapWithoutNarrowCorridors = WallOffNarrowCorridors(smoothedMap);

            // Clean up regions smaller than threshold for both walls and rooms.
            var (survivingRooms, cleanedMap) = RemoveRoomsAndWallsBelowThreshold(caveConfig.WallThresholdSize,
                caveConfig.RoomThresholdSize,
                smoothedMapWithoutNarrowCorridors, random);

            // Connect all rooms to main (the biggest) room
            var connectedMap = ConnectAllRoomsToMainRoom(survivingRooms, cleanedMap, caveConfig);

            //var ensuredTraversabilityMap = EnsureAllTilesArePathable(connectedMap, cleanedMap)

            // Ensure a border around the map
            var borderedMap = CreateBorderedMap(connectedMap, caveConfig.BitMapWidth, caveConfig.BitMapHeight,
                caveConfig.BorderSize, random);

            // Draw gizmo of map for debugging. Will draw the map in Scene upon selection.
            _mapToDraw = borderedMap;

            // The rooms should now reflect their relative shifted positions after adding borders round map.
            survivingRooms.ForEach(r => r.OffsetCoordsBy(caveConfig.BorderSize, caveConfig.BorderSize));

            var meshGen = GetComponent<MeshGenerator>();
            var collisionMap = meshGen.GenerateMesh(borderedMap, wallHeight,
                true, survivingRooms, _caveConfig.BrokenCollisionMap);

            foreach (var room in survivingRooms)
            {
                room.Dispose();
            }

            // Rotate to fit 2D view
            _plane.rotation = Quaternion.AngleAxis(-90, Vector3.right);

            return collisionMap;
        }

        /**
         * In order to make sure, that all algorithms can traverse all tiles the following must hold:
         * - Every tile has 1 horizontal neighbour to each side, or 2 in either direction
         * - Every tile has 1 vertical neighbour to each side, or 2 in either direction
         * - Every tile has at least one diagonal neighbor on the line from bottom left to top right and top left to bottom right.
         * This is due to some algorithms (e.g. B&amp;M) assuming that any partially covered tile is completely covered
         * This assumption leads to some narrow corridors traversable.
         * If we block the corridors with walls in this function, the algorithm will consider them two separate rooms later on
         * which will cause the algorithm to create a corridor of width n between them. This ensures traversability of all tiles. 
         */
        private Tile[,] WallOffNarrowCorridors(Tile[,] map)
        {
            var newMap = (Tile[,])map.Clone();
            var tilesToCheck = new Queue<(int, int)>();

            // Populate queue with all tiles
            for (var x = 0; x < newMap.GetLength(0); x++)
            {
                for (var y = 0; y < newMap.GetLength(1); y++)
                {
                    if (newMap[x, y].Type == TileType.Room)
                    {
                        tilesToCheck.Enqueue((x, y));
                    }
                }
            }

            while (tilesToCheck.Count > 0)
            {
                var (x, y) = tilesToCheck.Dequeue();
                if (Tile.IsWall(newMap[x, y].Type))
                {
                    continue;
                }

                // Check 3 tiles horizontally
                var horizontalClear =
                    IsInMapRange(x - 1, y, newMap) && newMap[x - 1, y].Type == TileType.Room && IsInMapRange(x + 1, y, newMap) && newMap[x + 1, y].Type == TileType.Room ||
                    IsInMapRange(x + 1, y, newMap) && newMap[x + 1, y].Type == TileType.Room && IsInMapRange(x + 2, y, newMap) && newMap[x + 2, y].Type == TileType.Room ||
                    IsInMapRange(x - 1, y, newMap) && newMap[x - 1, y].Type == TileType.Room && IsInMapRange(x - 2, y, newMap) && newMap[x - 2, y].Type == TileType.Room;

                // Check 3 tiles vertically
                var verticalClear =
                    IsInMapRange(x, y - 1, newMap) && newMap[x, y - 1].Type == TileType.Room && IsInMapRange(x, y + 1, newMap) && newMap[x, y + 1].Type == TileType.Room ||
                    IsInMapRange(x, y + 1, newMap) && newMap[x, y + 1].Type == TileType.Room && IsInMapRange(x, y + 2, newMap) && newMap[x, y + 2].Type == TileType.Room ||
                    IsInMapRange(x, y - 1, newMap) && newMap[x, y - 1].Type == TileType.Room && IsInMapRange(x, y - 2, newMap) && newMap[x, y - 2].Type == TileType.Room;

                // Check 2 tiles from bottom left to top right clear
                var bottomLeftToTopRightClear =
                    IsInMapRange(x - 1, y - 1, newMap) && newMap[x - 1, y - 1].Type == TileType.Room ||
                    IsInMapRange(x + 1, y + 1, newMap) && newMap[x + 1, y + 1].Type == TileType.Room;

                // Check 2 tiles from top left to bottom right clear
                var topLeftToBottomRightClear =
                    IsInMapRange(x - 1, y + 1, newMap) && newMap[x - 1, y + 1].Type == TileType.Room ||
                    IsInMapRange(x + 1, y - 1, newMap) && newMap[x + 1, y - 1].Type == TileType.Room;

                if (horizontalClear && verticalClear && bottomLeftToTopRightClear && topLeftToBottomRightClear)
                {
                    continue;
                }

                var types = new List<TileType>();
                for (var neighborX = x - 1; neighborX <= x + 1; neighborX++)
                {
                    for (var neighborY = y - 1; neighborY <= y + 1; neighborY++)
                    {
                        if (IsInMapRange(neighborX, neighborY, newMap) && Tile.IsWall(newMap[neighborX, neighborY].Type))
                        {
                            types.Add(newMap[neighborX, neighborY].Type);
                        }
                    }
                }

                var type = types.GroupBy(type => type).OrderByDescending(t => t.Count()).First().Key;

                newMap[x, y] = new Tile(type);
                // enqueue neighbours to be checked again
                for (var neighborX = x - 1; neighborX <= x + 1; neighborX++)
                {
                    for (var neighborY = y - 1; neighborY <= y + 1; neighborY++)
                    {
                        if (IsInMapRange(neighborX, neighborY, newMap))
                        {
                            tilesToCheck.Enqueue((neighborX, neighborY));
                        }
                    }
                }
            }

            return newMap;
        }

        private Tile[,] ConnectAllRoomsToMainRoom(List<Room> survivingRooms, Tile[,] map, CaveMapConfig config)
        {
            var connectedMap = (Tile[,])map.Clone();
            survivingRooms.Sort();
            survivingRooms[0].IsMainRoom = true;
            survivingRooms[0].IsAccessibleFromMainRoom = true;

            return ConnectClosestRooms(survivingRooms, connectedMap, config.ConnectionPassagesWidth);
        }

        private Tile[,] ConnectClosestRooms(List<Room> allRooms, Tile[,] map, int passageWidth)
        {
            var connectedMap = (Tile[,])map.Clone();
            var roomListA = new List<Room>();
            var roomListB = new List<Room>();


            foreach (var room in allRooms)
            {
                if (room.IsAccessibleFromMainRoom)
                {
                    roomListB.Add(room);
                }
                else
                {
                    roomListA.Add(room);
                }
            }


            var bestDistance = 0;
            var bestTileA = new Vector2Int();
            var bestTileB = new Vector2Int();
            Room? bestRoomA = null;
            Room? bestRoomB = null;
            var possibleConnectionFound = false;

            foreach (var roomA in roomListA)
            {
                foreach (var roomB in roomListB.Where(roomB => roomA != roomB && !roomA.IsConnected(roomB)))
                {
                    foreach (var tileA in roomA.EdgeTiles)
                    {
                        foreach (var tileB in roomB.EdgeTiles)
                        {
                            var distanceBetweenRooms =
                                (int)(Mathf.Pow(tileA.x - tileB.x, 2) + Mathf.Pow(tileA.y - tileB.y, 2));

                            if (distanceBetweenRooms >= bestDistance && possibleConnectionFound)
                            {
                                continue;
                            }

                            bestDistance = distanceBetweenRooms;
                            possibleConnectionFound = true;
                            bestTileA = tileA;
                            bestTileB = tileB;
                            bestRoomA = roomA;
                            bestRoomB = roomB;
                        }
                    }
                }
            }

            if (!possibleConnectionFound)
            {
                return connectedMap;
            }

            if (bestRoomA == null || bestRoomB == null)
            {
                throw new InvalidOperationException("bestRoomA and / or bestRoomB are null");
            }

            CreatePassage(bestRoomA, bestRoomB, bestTileA, bestTileB, connectedMap, passageWidth);
            return ConnectClosestRooms(allRooms, connectedMap, passageWidth);
        }

        private void CreatePassage(Room roomA, Room roomB, Vector2Int tileA, Vector2Int tileB, Tile[,] map, int passageWidth)
        {
            Room.ConnectRooms(roomA, roomB);
            Debug.DrawLine(CoordToWorldPoint(tileA, map.GetLength(0), map.GetLength(1)),
                CoordToWorldPoint(tileB, map.GetLength(0), map.GetLength(1)),
                Color.green,
                100);

            var line = GetLine(tileA, tileB);
            foreach (var c in line)
            {
                MakeRoomOfLine(c, passageWidth, map);
            }
        }

        private void MakeRoomOfLine(Vector2Int c, int r, Tile[,] map)
        {
            for (var x = -r; x <= r; x++)
            {
                for (var y = -r; y <= r; y++)
                {
                    if (x * x + y * y > r * r)
                    {
                        continue;
                    }

                    var drawX = c.x + x;
                    var drawY = c.y + y;
                    if (IsInMapRange(drawX, drawY, map))
                    {
                        map[drawX, drawY] = new Tile(TileType.Room);
                    }
                }
            }
        }

        private static List<Vector2Int> GetLine(Vector2Int from, Vector2Int to)
        {
            var line = new List<Vector2Int>();

            var x = from.x;
            var y = from.y;

            var dx = to.x - from.x;
            var dy = to.y - from.y;

            var inverted = false;
            var step = Math.Sign(dx);
            var gradientStep = Math.Sign(dy);

            var longest = Mathf.Abs(dx);
            var shortest = Mathf.Abs(dy);

            if (longest < shortest)
            {
                inverted = true;
                longest = Mathf.Abs(dy);
                shortest = Mathf.Abs(dx);

                step = Math.Sign(dy);
                gradientStep = Math.Sign(dx);
            }

            var gradientAccumulation = longest / 2;
            for (var i = 0; i < longest; i++)
            {
                line.Add(new Vector2Int(x, y));

                if (inverted)
                {
                    y += step;
                }
                else
                {
                    x += step;
                }

                gradientAccumulation += shortest;
                if (gradientAccumulation < longest)
                {
                    continue;
                }

                if (inverted)
                {
                    x += gradientStep;
                }
                else
                {
                    y += gradientStep;
                }

                gradientAccumulation -= longest;
            }

            return line;
        }

        // Just used be drawing a line for debugging
        private static Vector3 CoordToWorldPoint(Vector2Int tile, int width, int height)
        {
            return new Vector3(-width / 2 + .5f + tile.x, -height / 2 + .5f + tile.y, 2);
        }

        private static Tile[,] CreateRandomFillMap(CaveMapConfig config, Random random)
        {
            var randomFillMap = new Tile[config.BitMapWidth, config.BitMapHeight];
            var pseudoRandom = new Random(config.RandomSeed);

            for (var x = 0; x < config.BitMapWidth; x++)
            {
                for (var y = 0; y < config.BitMapHeight; y++)
                {
                    if (x == 0 || x == config.BitMapWidth - 1 || y == 0 || y == config.BitMapHeight - 1)
                    {
                        randomFillMap[x, y] = Tile.GetRandomWall(random);
                    }
                    else
                    {
                        randomFillMap[x, y] = pseudoRandom.Next(0, 100) < config.RandomFillPercent
                            ? Tile.GetRandomWall(random)
                            : new Tile(TileType.Room);
                    }
                }
            }

            return randomFillMap;
        }

        private Tile[,] SmoothMap(Tile[,] map, CaveMapConfig config, Random random)
        {
            var smoothedMap = (Tile[,])map.Clone();
            for (var x = 0; x < config.BitMapWidth; x++)
            {
                for (var y = 0; y < config.BitMapHeight; y++)
                {
                    var (neighborWallTiles, neighborWallType) = GetSurroundingWallCount(x, y, map, random);

                    if (neighborWallTiles >= config.NeighbourWallsNeededToStayWall)
                    {
                        smoothedMap[x, y] = new Tile(neighborWallType);
                    }
                    else
                    {
                        smoothedMap[x, y] = new Tile(TileType.Room);
                    }
                }
            }

            return smoothedMap;
        }

        private (int, TileType) GetSurroundingWallCount(int gridX, int gridY, Tile[,] map, Random random)
        {
            var wallCount = 0;
            var wallTypes = new Dictionary<TileType, int>();
            for (var neighborX = gridX - 1; neighborX <= gridX + 1; neighborX++)
            {
                for (var neighborY = gridY - 1; neighborY <= gridY + 1; neighborY++)
                {
                    if (IsInMapRange(neighborX, neighborY, map))
                    {
                        if ((neighborX == gridX || neighborY == gridY) || !Tile.IsWall(map[neighborX, neighborY].Type))
                        {
                            continue;
                        }

                        wallCount += 1;
                        wallTypes[map[neighborX, neighborY].Type] = wallTypes.GetValueOrDefault(map[neighborX, neighborY].Type) + 1;
                    }
                    else
                    {
                        wallCount++;
                        var tile = Tile.GetRandomWall(random);
                        wallTypes[tile.Type] = wallTypes.GetValueOrDefault(tile.Type) + 1;
                    }

                }
            }

            var mostCommonType = wallCount > 0 ? wallTypes.Aggregate((l, r) => l.Value > r.Value ? l : r).Key : TileType.Room;

            return (wallCount, mostCommonType);
        }

        private void OnDrawGizmosSelected()
        {
            if (_mapToDraw != null)
            {
                DrawMap(_mapToDraw);
            }
        }
    }
}