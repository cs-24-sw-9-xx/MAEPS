using System;

using JetBrains.Annotations;

using Maes.Map;
using Maes.Map.Generators;
using Maes.Utilities;

using UnityEngine;

using Random = System.Random;

namespace Tests.EditModeTests
{
    public static class Utilities
    {
        public static SimulationMap<Tile> GenerateSimulationMapFromString(string map)
        {
            var lines = map.Split(';', StringSplitOptions.RemoveEmptyEntries);
            var width = lines[0].Length;
            var height = lines.Length;
            var tiles = new SimulationMapTile<Tile>[width, height];

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var tileChar = lines[y][x];
                    var tile = tileChar == 'X' ? new Tile(TileType.Wall) : new Tile(TileType.Room);
                    tiles[x, y] = new SimulationMapTile<Tile>(() => tile);
                }
            }

            return new SimulationMap<Tile>(tiles, Vector2.zero);
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