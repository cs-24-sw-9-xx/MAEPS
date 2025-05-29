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

using System.Collections.Generic;
using System.Linq;

using Maes.Utilities;

using UnityEngine;

using Random = System.Random;


namespace Maes.Map.Generators
{
    public abstract class MapGenerator : MonoBehaviour
    {
        // Set by Awake
        protected Transform _plane = null!;
        protected Transform _innerWalls2D = null!;
        protected Transform _innerWalls3D = null!;
        protected Transform _wallRoof = null!;
        private MeshGenerator _meshGenerator = null!;

        // Variable used for drawing gizmos on selection for debugging.
        protected Tile[,]? _mapToDraw = null;

        public void Awake()
        {
            _plane = transform.Find("CaveFloor").GetComponent<Transform>();
            _innerWalls2D = transform.Find("InnerWalls2D").GetComponent<Transform>();
            _innerWalls3D = transform.Find("InnerWalls3D").GetComponent<Transform>();
            _wallRoof = transform.Find("WallRoof").GetComponent<Transform>();
            _meshGenerator = GetComponent<MeshGenerator>();
        }

        protected void MovePlaneAndWallRoofToFitWallHeight(float wallHeight)
        {
            var newPosition = _wallRoof.position;
            newPosition.z = -wallHeight;
            _wallRoof.position = newPosition;

            newPosition = _innerWalls2D.position;
            newPosition.z = -wallHeight;
            _innerWalls2D.position = newPosition;
        }

        protected void ResizePlaneToFitMap(int bitMapHeight, int bitMapWidth, float padding = 0.1f)
        {
            // Resize plane below cave to fit size
            _plane.localScale = new Vector3(((bitMapWidth) / 10f) + padding,
                1,
                (bitMapHeight / 10f) + padding);
        }

        protected void ClearMap()
        {
            _meshGenerator.ClearMesh();
        }

        protected static Tile[,] CreateBorderedMap(Tile[,] map, int width, int height, int borderSize, Random random)
        {
            var borderedMap = new Tile[width + (borderSize * 2), height + (borderSize * 2)];
            var tile = Tile.GetRandomWall(random);
            for (var x = 0; x < borderedMap.GetLength(0); x++)
            {
                for (var y = 0; y < borderedMap.GetLength(1); y++)
                {
                    if (x > borderSize - 1 && x < width + borderSize && y > borderSize - 1 && y < height + borderSize)
                    {
                        borderedMap[x, y] = map[x - borderSize, y - borderSize];
                    }
                    else
                    {
                        borderedMap[x, y] = tile;
                    }
                }
            }

            return borderedMap;
        }
        protected List<List<Vector2Int>> GetRegions(Tile[,] map, params TileType[] tileTypes)
        {
            var regions = new List<List<Vector2Int>>();
            // Flags if a given coordinate has already been accounted for
            // 1 = yes, 0 = no
            using var mapFlags = new Bitmap(map.GetLength(0), map.GetLength(1));

            for (var x = 0; x < map.GetLength(0); x++)
            {
                for (var y = 0; y < map.GetLength(1); y++)
                {
                    if (mapFlags.Contains(x, y) || !tileTypes.Contains(map[x, y].Type))
                    {
                        continue;
                    }

                    var newRegion = GetRegionTiles(x, y, map);
                    regions.Add(newRegion);

                    foreach (var tile in newRegion)
                    {
                        mapFlags.Set(tile.x, tile.y);
                    }
                }
            }

            return regions;
        }

        // A flood-full algorithm for finding all tiles in the region
        // For example if it starts at some point, that is an empty room tile
        // if will return all room tiles connected (in this region).
        // This is a similar algorithm to the one used in MS Paint for filling.
        protected List<Vector2Int> GetRegionTiles(int startX, int startY, Tile[,] map)
        {
            var tiles = new List<Vector2Int>();
            using var mapFlags = new Bitmap(map.GetLength(0), map.GetLength(1));
            var tileType = map[startX, startY].Type;

            var queue = new Queue<Vector2Int>();
            queue.Enqueue(new Vector2Int(startX, startY));
            mapFlags.Set(startX, startY);

            while (queue.Count > 0)
            {
                var tile = queue.Dequeue();
                tiles.Add(tile);

                for (var x = tile.x - 1; x <= tile.x + 1; x++)
                {
                    for (var y = tile.y - 1; y <= tile.y + 1; y++)
                    {
                        if (!IsInMapRange(x, y, map) || (y != tile.y && x != tile.x))
                        {
                            continue;
                        }

                        if (mapFlags.Contains(x, y) || map[x, y].Type != tileType)
                        {
                            continue;
                        }

                        mapFlags.Set(x, y);
                        queue.Enqueue(new Vector2Int(x, y));
                    }
                }
            }

            return tiles;
        }
        protected static bool IsInMapRange(int x, int y, Tile[,] map)
        {
            return x >= 0 && x < map.GetLength(0) && y >= 0 && y < map.GetLength(1);
        }

        protected (List<Room> surviningRooms, Tile[,] map) RemoveRoomsAndWallsBelowThreshold(int wallThreshold, int roomThreshold,
            Tile[,] map, Random random)
        {
            var cleanedMap = (Tile[,])map.Clone();
            var wallRegions = GetRegions(cleanedMap, Tile.Walls());

            foreach (var wallRegion in wallRegions)
            {
                if (wallRegion.Count >= wallThreshold)
                {
                    continue;
                }

                foreach (var tile in wallRegion)
                {
                    cleanedMap[tile.x, tile.y] = new Tile(TileType.Room);
                }
            }

            var roomRegions = GetRegions(cleanedMap, TileType.Room);
            var survivingRooms = new List<Room>();

            foreach (var roomRegion in roomRegions)
            {
                if (roomRegion.Count < roomThreshold)
                {
                    var tileType = Tile.GetRandomWall(random);
                    foreach (var tile in roomRegion)
                    {
                        cleanedMap[tile.x, tile.y] = tileType;
                    }
                }
                else
                {
                    survivingRooms.Add(new Room(roomRegion, cleanedMap));
                }
            }

            return (survivingRooms, cleanedMap);
        }

        // Draw the gizmo of the map for debugging purposes.
        protected void DrawMap(Tile[,] map)
        {
            if (_mapToDraw == null)
            {
                return;
            }

            var width = map.GetLength(0);
            var height = map.GetLength(1);

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    Gizmos.color = map[x, y].Type switch
                    {
                        TileType.Wall => Color.black,
                        TileType.Room => Color.white,
                        TileType.Hall => Color.gray,
                        TileType.Concrete => Color.yellow,
                        TileType.Wood => Color.green,
                        TileType.Brick => Color.red,
                        _ => Color.blue
                    };

                    var pos = new Vector3((-width / 2) + x + .5f, (-height / 2) + y + .5f, 0);
                    Gizmos.DrawCube(pos, Vector3.one);
                }
            }
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