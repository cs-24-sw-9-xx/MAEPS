// Copyright 2022 MAES
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
// Contributors: Malte Z. Andreasen, Philip I. Holler and Magnus K. Jensen
// 
// Original repository: https://github.com/MalteZA/MAES

using System;

using UnityEngine;

namespace Maes.Map.Generators
{
    public readonly struct BuildingMapConfig : IEquatable<BuildingMapConfig>, IMapConfig
    {
        // Bitmap size is always +1 larger in both axis
        // due to the marching squares algorithm using 4 points per square
        public int WidthInTiles { get; }
        public int HeightInTiles { get; }
        public int BitMapWidth { get; }
        public int BitMapHeight { get; }

        public int RandomSeed { get; }

        public float MaxHallInPercent { get; }

        public int HallWidth { get; }
        public float MinRoomSideLength { get; }
        public uint DoorWidth { get; }

        public int DoorPadding { get; }

        // Value in [0,100] determining the likelihood of an office being split
        // Higher value = more but smaller rooms.
        public uint RoomSplitChancePercent { get; }

        public int BorderSize { get; }
        public int WallThickness { get; }

        public bool BrokenCollisionMap { get; }

        internal BuildingMapConfig(MaesYamlConfigLoader.MaesConfigType config, int seed) : this(
            randomSeed: seed, wallThickness: config.Map!.BuildingConfig!.WallThickness, widthInTiles: config.Map.WidthInTiles,
            heightInTiles: config.Map.HeightInTiles,
            maxHallInPercent: config.Map.BuildingConfig.MaxHallInPercent,
            hallWidth: config.Map.BuildingConfig.HallWidth,
            minRoomSideLength: config.Map.BuildingConfig.MinRoomSideLength,
            doorWidth: config.Map.BuildingConfig.DoorWidth,
            doorPadding: config.Map.BuildingConfig.DoorPadding,
            roomSplitChancePercent: config.Map.BuildingConfig.RoomSplitChance,
            borderSize: config.Map.BorderSize
            )
        {
        }

        public BuildingMapConfig(
            int randomSeed, int wallThickness = 1, int widthInTiles = 50, int heightInTiles = 50,
            float maxHallInPercent = 20,
            int hallWidth = 4,
            int minRoomSideLength = 6,
            uint doorWidth = 2,
            int doorPadding = 2,
            uint roomSplitChancePercent = 85,
            int borderSize = 1,
            bool brokenCollisionMap = true)
        {
            if ((2 * doorPadding + doorWidth) > minRoomSideLength)
            {
                throw new ArgumentOutOfRangeException(nameof(doorWidth),
                    "Door width cannot be bigger than the smallest side lenght of rooms plus two times DoorPadding");
            }

            if (roomSplitChancePercent > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(roomSplitChancePercent), "roomSplitChance cannot be greater than 100");
            }

            WidthInTiles = widthInTiles;
            HeightInTiles = heightInTiles;
            BitMapWidth = widthInTiles + 1 - (borderSize * 2);
            BitMapHeight = heightInTiles + 1 - (borderSize * 2);

            RandomSeed = randomSeed;
            WallThickness = wallThickness;
            MaxHallInPercent = maxHallInPercent;
            HallWidth = hallWidth;
            MinRoomSideLength = minRoomSideLength;
            DoorWidth = doorWidth;
            DoorPadding = doorPadding;
            RoomSplitChancePercent = roomSplitChancePercent;
            BorderSize = borderSize;
            BrokenCollisionMap = brokenCollisionMap;
        }

        public bool Equals(BuildingMapConfig other)
        {
            return WidthInTiles == other.WidthInTiles && HeightInTiles == other.HeightInTiles && BitMapWidth == other.BitMapWidth && BitMapHeight == other.BitMapHeight && RandomSeed == other.RandomSeed && MaxHallInPercent.Equals(other.MaxHallInPercent) && HallWidth == other.HallWidth && MinRoomSideLength.Equals(other.MinRoomSideLength) && DoorWidth == other.DoorWidth && DoorPadding == other.DoorPadding && RoomSplitChancePercent == other.RoomSplitChancePercent && BorderSize == other.BorderSize && WallThickness == other.WallThickness;
        }

        public override bool Equals(object? obj)
        {
            return obj is BuildingMapConfig other && Equals(other);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(WidthInTiles);
            hashCode.Add(HeightInTiles);
            hashCode.Add(BitMapWidth);
            hashCode.Add(BitMapHeight);
            hashCode.Add(RandomSeed);
            hashCode.Add(MaxHallInPercent);
            hashCode.Add(HallWidth);
            hashCode.Add(MinRoomSideLength);
            hashCode.Add(DoorWidth);
            hashCode.Add(DoorPadding);
            hashCode.Add(RoomSplitChancePercent);
            hashCode.Add(BorderSize);
            hashCode.Add(WallThickness);
            return hashCode.ToHashCode();
        }

        public readonly SimulationMap<Tile> GenerateMap(GameObject gameObject, float wallHeight = 2)
        {
            var buildingGenerator = gameObject.AddComponent<BuildingGenerator>();
            return buildingGenerator.GenerateBuildingMap(this, wallHeight);
        }

        public static bool operator ==(BuildingMapConfig left, BuildingMapConfig right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BuildingMapConfig left, BuildingMapConfig right)
        {
            return !left.Equals(right);
        }
    }
}