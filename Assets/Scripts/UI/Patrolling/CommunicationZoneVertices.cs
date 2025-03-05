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
using Maes.Map.MapGen;
using Maes.Robot;
using Maes.Utilities;

using UnityEngine;

namespace Maes.UI.Patrolling
{
    public class CommunicationZoneVertices
    {
        public Dictionary<int, HashSet<int>> CommunicationZoneTiles { get; }
        public HashSet<int> AllCommunicationZoneTiles { get; } = new();
        private readonly SimulationMap<Tile> _simulationMap;
        private readonly PatrollingMap _patrollingMap;
        private readonly CommunicationManager _communicationManager;

        public CommunicationZoneVertices(SimulationMap<Tile> simulationMap, PatrollingMap patrollingMap, CommunicationManager communicationManager)
        {
            _simulationMap = simulationMap;
            _patrollingMap = patrollingMap;
            CommunicationZoneTiles = _patrollingMap.Vertices.ToDictionary(v => v.Id, _ => new HashSet<int>());
            _communicationManager = communicationManager;
        }

        public void CreateComunicationZoneTiles()
        {
            var positions = new List<Vector2Int>();
            foreach (var vertex in _patrollingMap.Vertices)
            {
                positions.Add(vertex.Position);
            }
            var communicationZones = _communicationManager.CalculateCommunicationZone(positions, _simulationMap.WidthInTiles, _simulationMap.HeightInTiles);
            var cellIndexToTriangleIndexes = CellIndexToTriangleIndexes(_simulationMap);
            using var bitmap = MapUtilities.MapToBitMap(_simulationMap);

            foreach (var vertex in _patrollingMap.Vertices)
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

        private static List<List<int>> CellIndexToTriangleIndexes(SimulationMap<Tile> simulationMap)
        {
            var cellIndexTriangleIndexes = new List<List<int>>();

            var list = new List<int>();
            foreach (var (index, _) in simulationMap)
            {
                list.Add(index);
                if ((index + 1) % 8 == 0)
                {
                    cellIndexTriangleIndexes.Add(list);
                    list = new List<int>();
                }
            }

            return cellIndexTriangleIndexes;
        }

    }
}