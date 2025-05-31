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
// Henrik van Peet
// Mads Beyer Mogensen
// Puvikaran Santhirasegaram

using System.Collections.Generic;

using Maes.Map;
using Maes.Map.Generators;
using Maes.Robot;
using Maes.Utilities;

namespace Maes.UI.Visualizers.Patrolling
{
    public sealed class CommunicationZoneVertices
    {
        private readonly SimulationMap<Tile> _simulationMap;
        private readonly PatrollingMap _patrollingMap;
        private readonly CommunicationManager _communicationManager;

        private Dictionary<int, Bitmap>? _communicationZoneTiles;

        public IReadOnlyDictionary<int, Bitmap> CommunicationZoneTiles
        {
            get
            {
                if (_communicationZoneTiles == null)
                {
                    PopulateCommunicationZones();
                }

                return _communicationZoneTiles!;
            }
        }

        private Bitmap? _allCommunicationZoneTiles;

        public Bitmap AllCommunicationZoneTiles
        {
            get
            {
                if (_allCommunicationZoneTiles == null)
                {
                    PopulateCommunicationZones();
                }

                return _allCommunicationZoneTiles!;
            }
        }

        public CommunicationZoneVertices(SimulationMap<Tile> simulationMap, PatrollingMap patrollingMap, CommunicationManager communicationManager)
        {
            _simulationMap = simulationMap;
            _patrollingMap = patrollingMap;
            _communicationManager = communicationManager;
        }

        private void PopulateCommunicationZones()
        {
            _communicationZoneTiles = new Dictionary<int, Bitmap>();
            var vertices = _patrollingMap.Vertices;
            var communicationZones = _communicationManager.CalculateZones(vertices);
            foreach (var vertex in vertices)
            {
                _communicationZoneTiles[vertex.Id] = communicationZones[vertex.Position];
            }

            _allCommunicationZoneTiles = new Bitmap(_simulationMap.WidthInTiles, _simulationMap.HeightInTiles);
            foreach (var (id, zone) in communicationZones)
            {
                _allCommunicationZoneTiles.Union(zone);
            }
        }
    }
}