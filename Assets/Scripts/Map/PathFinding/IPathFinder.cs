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

using UnityEngine;

using static Maes.Map.SlamMap;

namespace Maes.Map.PathFinding
{
    public interface IPathFinder
    {
        public Vector2Int[]? GetPath<TMap>(Vector2Int startCoordinate, Vector2Int targetCoordinate,
            TMap pathFindingMap, bool beOptimistic = false, bool acceptPartialPaths = false)
            where TMap : IPathFindingMap;

        public Vector2Int[]? GetOptimisticPath<TMap>(Vector2Int startCoordinate, Vector2Int targetCoordinate, TMap pathFindingMap, bool acceptPartialPaths = false)
            where TMap : IPathFindingMap;

        public PathStep[] PathToSteps(Vector2Int[] path);

        public Vector2Int? GetNearestTileFloodFill<TMap>(TMap pathFindingMap, Vector2Int targetCoordinate, SlamTileStatus lookupStatus, HashSet<Vector2Int>? excludedTiles = null)
            where TMap : IPathFindingMap;

        public Vector2Int? IsAnyNeighborStatus<TMap>(Vector2Int targetCoordinate, TMap pathFindingMap, SlamTileStatus status, bool optimistic = false)
            where TMap : IPathFindingMap;

    }
}