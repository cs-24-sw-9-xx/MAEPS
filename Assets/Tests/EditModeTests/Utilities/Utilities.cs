using System;
using System.Collections.Generic;

using JetBrains.Annotations;

using Maes.Map;
using Maes.Map.Generators;
using Maes.Utilities;

using UnityEngine;

using Random = System.Random;

namespace Tests.EditModeTests.Utilities
{
    public static class Utilities
    {
        public static ((Vector2 start, Vector2 end), SimulationMap<Tile> map) GenerateSimulationMapFromString(string map)
        {
            var lines = map.Split(';', StringSplitOptions.RemoveEmptyEntries);
            var width = lines[0].Length;
            var height = lines.Length;
            var tiles = new SimulationMapTile<Tile>[width, height];

            var start = Vector2.zero;
            var end = Vector2.zero;


            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var tileChar = lines[y][x];
                    if (tileChar is 'S' or 's')
                    {
                        start = new Vector2(x, y);
                        tileChar = tileChar == 'S' ? 'X' : ' ';
                    }
                    else if (tileChar is 'E' or 'e')
                    {
                        end = new Vector2(x, y);
                        tileChar = tileChar == 'E' ? 'X' : ' ';
                    }

                    var tile = tileChar == 'X' ? new Tile(TileType.Wall) : new Tile(TileType.Room);
                    tiles[x, y] = new SimulationMapTile<Tile>(() => tile);
                }
            }

            return ((start + Vector2.one / 2f, end + Vector2.one / 2f), new SimulationMap<Tile>(tiles, Vector2.zero));
        }

        
        
        

        public static ((Vector2 start, Vector2 end), SimulationMap<Tile> map) GenerateSimulationMapWithMeetingPointsFromString(string map)
        {
            var lines = map.Split(';', StringSplitOptions.RemoveEmptyEntries);
            var width = lines[0].Length;
            var height = lines.Length;
            var tiles = new SimulationMapTile<Tile>[width, height];

            var start = Vector2.zero;
            var end = Vector2.zero;

            var vertexPositionsByPartitionId = new Dictionary<int, HashSet<Vector2>>();

            Vector2? vertexPosition;

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var tileChar = lines[y][x];
                    
                    switch (tileChar)
                    {
                        case 'S' or 's':
                            start = new Vector2(x, y);
                            tileChar = tileChar == 'S' ? 'X' : ' ';
                            break;
                        case 'E' or 'e':
                            end = new Vector2(x, y);
                            tileChar = tileChar == 'E' ? 'X' : ' ';
                            break;
                        default:
                            if (int.TryParse(tileChar.ToString(), out var partitionId))
                            {
                                
                            }
                            
                            
                            
                            break;
                    }

                    var tile = tileChar == 'X' ? new Tile(TileType.Wall) : new Tile(TileType.Room);
                    tiles[x, y] = new SimulationMapTile<Tile>(() => tile);
                }
            }

            return ((start + Vector2.one / 2f, end + Vector2.one / 2f), new SimulationMap<Tile>(tiles, Vector2.zero));
        }



        [MustDisposeResource]
        public static Bitmap CreateRandomBitmap(int width, int height, int seed)
        {
            var bitmap = new Bitmap(0, 0, width, height);

            var random = new Random(seed);

            for (var x = 0; x < bitmap.Width; x++)
            {
                for (var y = 0; y < bitmap.Height; y++)
                {
                    if (random.Next(2) == 1)
                    {
                        bitmap.Set(x, y);
                    }
                }
            }

            return bitmap;
        }

        [MustDisposeResource]
        public static Bitmap BitmapFromString(string map)
        {
            var split = map.Split(';', StringSplitOptions.RemoveEmptyEntries);
            var height = split.Length;

            var width = split[0].Length;

            var bitmap = new Bitmap(0, 0, width, height);

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    if (split[y][x] == 'X')
                    {
                        bitmap.Set(x, y);
                    }
                }
            }

            return bitmap;
        }

        [MustDisposeResource]
        public static Bitmap CreateEmptyBitmap(int width, int height)
        {
            var bitmap = new Bitmap(0, 0, width, height);

            // Top and bottom walls
            for (var x = 0; x < width; x++)
            {
                bitmap.Set(x, 0);
                bitmap.Set(x, height - 1);
            }

            // Left and right walls
            for (var y = 0; y < height; y++)
            {
                bitmap.Set(0, y);
                bitmap.Set(width - 1, y);
            }

            return bitmap;
        }
    }
}