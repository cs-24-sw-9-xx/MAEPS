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
// Contributors 2025: 
// Casper Nyvang Sørensen,
// Christian Ziegler Sejersen,
// Jakob Meyer Olsen

using System.Collections.Generic;
using System.Linq;

using Maes.Map;
using Maes.Map.Generators;
using Maes.Robot;
using Maes.Utilities;

namespace Maes.UI.Visualizers.Patrolling
{
    public sealed class CommunicationZoneVertices
    {
        public Dictionary<int, HashSet<int>> CommunicationZoneTiles { get; }
        public HashSet<int> AllCommunicationZoneTiles { get; } = new();

        public CommunicationZoneVertices(SimulationMap<Tile> simulationMap, PatrollingMap patrollingMap, CommunicationManager communicationManager)
        {
            CommunicationZoneTiles = patrollingMap.Vertices.ToDictionary(v => v.Id, _ => new HashSet<int>());

            var positions = patrollingMap.Vertices.Select(v => v.Position).ToList();
            var communicationZones = communicationManager.CalculateZones(positions, simulationMap.WidthInTiles, simulationMap.HeightInTiles);
            var cellIndexToTriangleIndexes = simulationMap.CellIndexToTriangleIndexes();
            using var bitmap = MapUtilities.MapToBitMap(simulationMap);
            foreach (var vertex in patrollingMap.Vertices)
            {
                var tiles = communicationZones[vertex.Position];
                if (tiles == null)
                {
                    continue;
                }
                foreach (var tile in tiles)
                {
                    var index = tile.x + tile.y * bitmap.Width;
                    CommunicationZoneTiles[vertex.Id].UnionWith(cellIndexToTriangleIndexes[index]);
                }
                AllCommunicationZoneTiles.UnionWith(CommunicationZoneTiles[vertex.Id]);
            }
        }
    }
}