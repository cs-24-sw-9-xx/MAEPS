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
        public Dictionary<int, Bitmap> CommunicationZoneTiles { get; }
        public Bitmap AllCommunicationZoneTiles { get; }

        public CommunicationZoneVertices(SimulationMap<Tile> simulationMap, PatrollingMap patrollingMap, CommunicationManager communicationManager)
        {
            CommunicationZoneTiles = new Dictionary<int, Bitmap>();
            var vertecies = patrollingMap.Vertices;
            var communicationZones = communicationManager.CalculateZones(vertecies.Select(v => v.Position).ToList());
            foreach (var vertex in vertecies)
            {
                CommunicationZoneTiles[vertex.Id] = communicationZones[vertex.Position];
            }

            AllCommunicationZoneTiles = new Bitmap(0, 0, simulationMap.WidthInTiles, simulationMap.HeightInTiles);
            foreach (var (id, zone) in communicationZones)
            {
                AllCommunicationZoneTiles.Union(zone);
            }
        }
    }
}