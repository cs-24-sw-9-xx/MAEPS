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
// Contributors: 
// Henrik van Peet,
// Mads Beyer Mogensen,
// Puvikaran Santhirasegaram

using System.Collections.Generic;

using JetBrains.Annotations;

using Maes.Map;
using Maes.Map.Generators;
using Maes.Map.Generators.Patrolling.Waypoints.Connectors;
using Maes.Utilities;

using UnityEngine;

namespace Tests.EditModeTests.Utilities.MapInterpreter.MapBuilder
{
    public delegate void ConnectVerticesDelegate(IReadOnlyCollection<Vertex> vertices);
    public class PartitionSimulationMapBuilder : BaseSimulationMapBuilder<(SimulationMap<Tile> map, PatrollingMap patrollingMap, Dictionary<int, HashSet<Vertex>> verticesByPartitionId)>
    {
        public PartitionSimulationMapBuilder(string map, ConnectVerticesDelegate connectVerticesDelegate) : base(map)
        {
            _connectVerticesDelegate = connectVerticesDelegate;
        }

        private readonly ConnectVerticesDelegate _connectVerticesDelegate;

        private readonly VertexInterpreter _vertexInterpreter = new();
        protected override (SimulationMap<Tile> map, PatrollingMap patrollingMap, Dictionary<int, HashSet<Vertex>> verticesByPartitionId) BuildResult(SimulationMap<Tile> map)
        {
            var vertices = _vertexInterpreter.Vertices;

            // Connect the vertices using reverse nearest neighbors
            using var bitmap = MapUtilities.MapToBitMap(map);
            ReverseNearestNeighborWaypointConnector.ConnectVertices(vertices, bitmap);

            var patrollingMap = new PatrollingMap(vertices, map);
            return (map, patrollingMap, _vertexInterpreter.VerticesByPartitionId);
        }

        protected override void InterpretTile(char tileChar, int x, int y)
        {
            base.InterpretTile(tileChar, x, y);

            if (int.TryParse(tileChar.ToString(), out var partitionId))
            {
                _vertexInterpreter.CreateOrAssignVertexToPartition(x, y, partitionId);
            }
            else
            {
                _vertexInterpreter.EndVertexAssignment();
            }
        }

        private class VertexInterpreter
        {
            public List<Vertex> Vertices { get; } = new();
            public Dictionary<int, HashSet<Vertex>> VerticesByPartitionId { get; } = new();

            [CanBeNull] private Vertex _activeVertex;

            public void CreateOrAssignVertexToPartition(int x, int y, int partitionId)
            {
                if (_activeVertex == null)
                {
                    var newVertex = new Vertex(Vertices.Count, new Vector2Int(x, y));
                    Vertices.Add(newVertex);
                    _activeVertex = newVertex;
                }

                AddVertexToPartition(partitionId, _activeVertex);
            }

            public void EndVertexAssignment()
            {
                _activeVertex = null;
            }

            private void AddVertexToPartition(int partitionId, Vertex vertex)
            {
                if (!VerticesByPartitionId.ContainsKey(partitionId))
                {
                    VerticesByPartitionId[partitionId] = new HashSet<Vertex>();
                }

                VerticesByPartitionId[partitionId].Add(vertex);
            }
        }
    }
}