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

using Maes.Utilities;

using UnityEngine;

using static Maes.Map.SlamMap;

namespace Maes.Map.PathFinding
{
    public interface IPathFindingMap
    {
        [ForbiddenKnowledge]
        public bool BrokenCollisionMap { get; }

        [ForbiddenKnowledge]
        public int LastUpdateTick { get; }

        [ForbiddenKnowledge]
        public int Width { get; }
        [ForbiddenKnowledge]
        public int Height { get; }

        [ForbiddenKnowledge]
        public float CellSize { get; }

        public bool IsSolid(Vector2Int coordinate);

        public bool IsOptimisticSolid(Vector2Int coordinate);

        public bool IsUnseenSemiOpen(Vector2Int nextCoordinate, Vector2Int currentCoordinate);

        public bool IsWithinBounds(Vector2Int coordinate);

        public SlamTileStatus GetTileStatus(Vector2Int coordinate, bool optimistic = false);

        public Vector2Int? GetNearestTileFloodFill(Vector2Int targetCoordinate, SlamTileStatus lookupStatus, HashSet<Vector2Int>? excludedTiles = null);

        public Vector2Int GetCurrentPosition(bool dependOnBrokenBehavior = false);

        /// <summary>
        /// This is for debugging purposes only to be able to easily convert coordinates to world units for drawing.
        /// </summary>
        /// <param name="tile"></param>
        /// <returns></returns>
        public Vector3 TileToWorld(Vector2 tile);
    }
}