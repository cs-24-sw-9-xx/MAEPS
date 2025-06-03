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
// Contributors: Rasmus Borrisholt Schmidt, Andreas Sebastian Sørensen, Thor Beregaard
// 
// Original repository: https://github.com/Molitany/MAES

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Maes.Map.Generators
{
    public enum TileType : byte
    {
        Room,
        Hall,
        Wall,
        Concrete,
        Wood,
        Brick
    }

    [DebuggerDisplay("{Type}")]
    public readonly struct Tile
    {
        public readonly TileType Type;

        public Tile(TileType type)
        {
            Type = type;
        }

        public static Tile GetRandomWall(Random random)
        {
            var typeValues = Enum.GetValues(typeof(TileType));
            var randomWallInt = random.Next((int)TileType.Concrete, typeValues.Length);
            return new Tile((TileType)typeValues.GetValue(randomWallInt));
        }

        public static bool IsWall(TileType tile)
        {
            return (byte)tile >= (byte)TileType.Wall;
        }

        public static TileType[] Walls()
        {
            var wallTypes = new List<TileType>();
            var typeValues = Enum.GetValues(typeof(TileType));
            for (var i = (int)TileType.Concrete; i < typeValues.Length; i++)
            {
                wallTypes.Add((TileType)typeValues.GetValue(i));
            }
            return wallTypes.ToArray();
        }
    }
}