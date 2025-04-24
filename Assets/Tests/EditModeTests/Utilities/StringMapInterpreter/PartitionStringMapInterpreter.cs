using System.Collections.Generic;
using System.Linq;

using JetBrains.Annotations;

using Maes.Map;
using Maes.Map.Generators;
using Maes.Map.Generators.Patrolling.Partitioning;

using UnityEngine;

namespace Tests.EditModeTests.Utilities.StringMapInterpreter
{
    public class PartitionStringMapInterpreter
    {
        public PartitionStringMapInterpreter(string map)
        {
            _stringMapInterpreter = new StringMapInterpreter(map);
            SimulationMapTile = new SimulationMapTile<Tile>[_stringMapInterpreter.Width, _stringMapInterpreter.Height];
        }

        private SimulationMapTile<Tile>[,] SimulationMapTile { get; }
        private readonly StringMapInterpreter _stringMapInterpreter;
        private readonly VertexInterpreter _vertexInterpreter = new();
            
        public Vector2 Start { get; private set; }
        public Vector2 End { get; private set; }
        public List<Vertex> Vertices => _vertexInterpreter.Vertices;
        public Dictionary<int, HashSet<Vertex>> VertexPositionsByPartitionId => _vertexInterpreter.VertexPositionsByPartitionId;
        public Dictionary<int, HashSet<int>> MeetingRobotsByMeetingPointId { get; } = new();
        public SimulationMap<Tile> SimulationMap => new(SimulationMapTile, Vector2.zero);
            
        public void Interpret()
        {
            foreach (var (tileChar, x, y) in _stringMapInterpreter.GetTiles())
            {
                Tile tile;
                switch (tileChar)
                {
                    case 'S' or 's':
                        Start = CreateVector2(x, y);
                        tile = tileChar == 'S' ? new Tile(TileType.Wall) : new Tile(TileType.Room);
                        _vertexInterpreter.NoWaypoint();
                        break;
                    case 'E' or 'e':
                        End = CreateVector2(x, y);
                        tile = tileChar == 'E' ? new Tile(TileType.Wall) : new Tile(TileType.Room);
                        _vertexInterpreter.NoWaypoint();
                        break;
                    default:
                        {
                            if (int.TryParse(tileChar.ToString(), out var partitionId))
                            {
                                _vertexInterpreter.CreateWaypointAndAddToPartition(x, y, partitionId);
                                tile = new Tile(TileType.Room);
                            }
                            else
                            {
                                _vertexInterpreter.NoWaypoint();
                                tile = tileChar == 'X' ? new Tile(TileType.Wall) : new Tile(TileType.Room);
                            }
                            break;
                        }
                }
                    
                SimulationMapTile[x, y] = new SimulationMapTile<Tile>(() => tile);
            }

            FindMeetingPoints();
        }

        private Vector2 CreateVector2(int x, int y)
        {
            return new Vector2(x, y) + Vector2.one / 2f;
        }
            
        private void FindMeetingPoints()
        {
            foreach (var ((partitionId1, vertices1), (partitionId2, vertices2)) in _vertexInterpreter.VertexPositionsByPartitionId.Combinations())
            {
                foreach (var vertexId in vertices1.Select(v => v.Id).Intersect(vertices2.Select(v => v.Id)) )
                {
                    if (!MeetingRobotsByMeetingPointId.ContainsKey(vertexId))
                    {
                        MeetingRobotsByMeetingPointId[vertexId] = new HashSet<int>();
                    }
                    MeetingRobotsByMeetingPointId[vertexId].Add(partitionId1);
                    MeetingRobotsByMeetingPointId[vertexId].Add(partitionId2);
                }
            }
        }

        private class VertexInterpreter
        {
            public List<Vertex> Vertices { get; } = new List<Vertex>();
            public Dictionary<int, HashSet<Vertex>> VertexPositionsByPartitionId { get; } = new();
                
            [CanBeNull] private Vertex _visitedVertex;
                
            public void CreateWaypointAndAddToPartition(int x, int y, int partitionId)
            {
                if (_visitedVertex == null)
                {
                    var newVertex = new Vertex(Vertices.Count, new Vector2Int(x, y));
                    Vertices.Add(newVertex);
                    _visitedVertex = newVertex;
                }
                    
                AddVertexToPartition(partitionId, _visitedVertex);
            }
                
            public void NoWaypoint()
            {
                _visitedVertex = null;
            }
                
            private void AddVertexToPartition(int partitionId, Vertex vertex)
            {
                if (!VertexPositionsByPartitionId.ContainsKey(partitionId))
                {
                    VertexPositionsByPartitionId[partitionId] = new HashSet<Vertex>();
                }
                    
                VertexPositionsByPartitionId[partitionId].Add(vertex);
            }
        }
    }
}