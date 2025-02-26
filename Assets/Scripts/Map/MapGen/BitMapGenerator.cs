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

using UnityEngine;

using Random = System.Random;

namespace Maes.Map.MapGen
{
    public class BitMapGenerator : MapGenerator
    {
        // Set by CreateMapFromBitMap
        private Tile[,] _bitmap = null!;
        private float _wallHeight;
        private int _borderSize;

        /// <summary>
        /// Method for creating a map from a 2D array of Tiles.
        /// </summary>
        public SimulationMap<Tile> CreateMapFromBitMap(Tile[,] bitmap, int seed, float wallHeight = 2.0f, int borderSize = 1, bool brokenCollisionMap = true)
        {
            _bitmap = bitmap;
            _wallHeight = wallHeight;
            _borderSize = borderSize;

            _borderSize = Math.Max(2, borderSize);

            var random = new Random(seed);

            // Clear and destroy objects from previous map
            ClearMap();

            // Add border around map
            var borderedMap = CreateBorderedMap(_bitmap, _bitmap.GetLength(0), _bitmap.GetLength(1), _borderSize, random);

            // Get rooms needed for mesh creation
            var (survivingRooms, cleanedMap) = RemoveRoomsAndWallsBelowThreshold(0, 0, borderedMap, random);

            // The rooms should now reflect their relative shifted positions after adding borders round map.
            survivingRooms.ForEach(r => r.OffsetCoordsBy(_borderSize, _borderSize));

            _mapToDraw = cleanedMap;

            // Create mesh
            var meshGen = GetComponent<MeshGenerator>();
            var collisionMap = meshGen.GenerateMesh((Tile[,])cleanedMap.Clone(), _wallHeight,
                true, survivingRooms, brokenCollisionMap: brokenCollisionMap);

            foreach (var room in survivingRooms)
            {
                room.Dispose();
            }

            // Rotate to fit 2D view
            _plane.rotation = Quaternion.AngleAxis(-90, Vector3.right);
            ResizePlaneToFitMap(_bitmap.GetLength(1), _bitmap.GetLength(0));
            MovePlaneAndWallRoofToFitWallHeight(_wallHeight);

            return collisionMap;
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