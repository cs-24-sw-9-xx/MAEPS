// Copyright 2025 MAEPS
// 
// This file is part of MAEPS
// 
// MAEPS is free software: you can redistribute it and/or modify it under
// the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your option)
// any later version.
// 
// MAEPS is distributed in the hope that it will be useful, but WITHOUT
// ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
// or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General
// Public License for more details.
// 
// You should have received a copy of the GNU General Public License along
// with MAEPS. If not, see http://www.gnu.org/licenses/.
// 
// Contributors: Mads Beyer Mogensen

using Maes.Map.Generators;
using Maes.Utilities;

namespace Tests.PlayModeTests
{
    public static class Utilities
    {
        public static Tile[,] BitmapToTilemap(Bitmap bitmap)
        {
            var tiles = new Tile[bitmap.Width, bitmap.Height];

            for (var x = 0; x < bitmap.Width; x++)
            {
                for (var y = 0; y < bitmap.Height; y++)
                {
                    // A bit weird that the only floor is room?
                    tiles[x, y] = new Tile(bitmap.Contains(x, y) ? TileType.Concrete : TileType.Room);
                }
            }

            return tiles;
        }
    }
}