using System.Collections.Generic;
using System.Linq;

using Maes.Map;
using Maes.Map.Generators;
using Maes.Utilities;

namespace Maes.UI.Visualizers.Patrolling
{
    public class LineOfSightVertices
    {
        public Dictionary<int, HashSet<int>> VerticesVisibleTiles { get; }
        public HashSet<int> AllVerticesVisibleTiles { get; } = new();
        private readonly SimulationMap<Tile> _simulationMap;
        private readonly PatrollingMap _patrollingMap;

        public LineOfSightVertices(SimulationMap<Tile> simulationMap, PatrollingMap patrollingMap)
        {
            _simulationMap = simulationMap;
            _patrollingMap = patrollingMap;
            VerticesVisibleTiles = _patrollingMap.Vertices.ToDictionary(v => v.Id, _ => new HashSet<int>());
        }

        public void CreateLineOfSightVertices()
        {
            var cellIndexToTriangleIndexes = _simulationMap.CellIndexToTriangleIndexes();

            using var bitmap = MapUtilities.MapToBitMap(_simulationMap);

            foreach (var vertex in _patrollingMap.Vertices)
            {
                var tiles = _patrollingMap.VertexPositions[vertex.Position];

                foreach (var tile in tiles)
                {
                    var index = tile.x + tile.y * bitmap.Width;
                    VerticesVisibleTiles[vertex.Id].UnionWith(cellIndexToTriangleIndexes[index]);
                }

                AllVerticesVisibleTiles.UnionWith(VerticesVisibleTiles[vertex.Id]);
            }
        }
    }
}