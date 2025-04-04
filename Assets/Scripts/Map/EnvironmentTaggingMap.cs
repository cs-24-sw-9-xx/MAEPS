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
using System.Collections.Generic;
using System.Linq;

using Maes.Map.Generators;

using UnityEngine;

namespace Maes.Map
{
    /// This type holds all environment tags deposited by robots in the simulated environment 
    /// This map is represented as a 2D array of tiles to make lookup faster
    internal sealed class EnvironmentTaggingMap
    {
        // Each tile in the map is a list of tags that are positioned within the bounds of that tile
        private readonly List<EnvironmentTag>[,] _tagLists;

        private readonly int _widthInTiles, _heightInTiles;
        private readonly Vector2 _offset;

        public EnvironmentTaggingMap(SimulationMap<Tile> collisionMap)
        {
            _widthInTiles = collisionMap.WidthInTiles;
            _heightInTiles = collisionMap.HeightInTiles;
            _offset = collisionMap.ScaledOffset;
            _tagLists = new List<EnvironmentTag>[_widthInTiles, _heightInTiles];
            for (var x = 0; x < _widthInTiles; x++)
            {
                for (var y = 0; y < _heightInTiles; y++)
                {
                    _tagLists[x, y] = new List<EnvironmentTag>();
                }
            }
        }

        public EnvironmentTag AddTag(Vector2 worldPosition, EnvironmentTag tag)
        {
            var gridCoordinate = ToLocalMapCoordinate(worldPosition);
            _tagLists[gridCoordinate.x, gridCoordinate.y].Add(tag);
            return tag;
        }

        public List<EnvironmentTag> GetTagsNear(Vector2 centerWorldPosition, float radius)
        {
            var nearbyTags = new List<EnvironmentTag>();

            var gridPosition = ToLocalMapCoordinate(centerWorldPosition);
            var maxTileRadius = (int)Math.Ceiling(radius);
            // Find bounding box of cells to check
            var minX = Math.Max(gridPosition.x - maxTileRadius, 0);
            var maxX = Math.Min(gridPosition.x + maxTileRadius, _widthInTiles);
            var minY = Math.Max(gridPosition.y - maxTileRadius, 0);
            var maxY = Math.Min(gridPosition.y + maxTileRadius, _heightInTiles);

            for (var x = minX; x < maxX; x++)
            {
                for (var y = minY; y < maxY; y++)
                {
                    // Add all tags from this tile that are within the given radius of the center 
                    nearbyTags.AddRange(_tagLists[x, y]
                        .Where(tag => Vector2.Distance(centerWorldPosition, tag.WorldPosition) <= radius));
                }
            }

            return nearbyTags;
        }

        // Takes a world coordinates and removes the offset and scale to translate it to a local map coordinate
        private Vector2Int ToLocalMapCoordinate(Vector2 worldCoordinate)
        {
            var localFloatCoordinate = worldCoordinate - _offset;
            var localCoordinate = new Vector2Int((int)localFloatCoordinate.x, (int)localFloatCoordinate.y);
            if (!IsWithinLocalMapBounds(localCoordinate))
            {
                throw new ArgumentException("The given coordinate " + localCoordinate
                                                                    + "(World coordinate:" + worldCoordinate + " )"
                                                                    + " is not within map bounds: {" + _widthInTiles +
                                                                    ", " + _heightInTiles + "}");
            }

            return localCoordinate;
        }

        // Checks that the given coordinate is within the local map bounds
        private bool IsWithinLocalMapBounds(Vector2Int localCoordinates)
        {
            return localCoordinates.x >= 0
                   && localCoordinates.x < _widthInTiles
                   && localCoordinates.y >= 0
                   && localCoordinates.y < _heightInTiles;
        }

        public interface ITag
        {
            public void DrawTag(Vector3 position);
        }

    }
}