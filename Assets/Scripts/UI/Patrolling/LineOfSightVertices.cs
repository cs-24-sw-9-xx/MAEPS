using System.Collections.Generic;
using System.Linq;

using Maes.Map;
using Maes.Map.MapGen;
using Maes.Utilities;

namespace Maes.UI.Patrolling
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
            var cellIndexToTriangleIndexes = CellIndexToTriangleIndexes(_simulationMap);

            var bitmap = MapUtilities.MapToBitMap(_simulationMap);

            var rows = bitmap.GetLength(0);

            foreach (var vertex in _patrollingMap.Vertices)
            {
                var tiles = LineOfSightUtilities.ComputeVisibilityOfPointFastBreakColumn(vertex.Position, bitmap);

                foreach (var tile in tiles)
                {
                    var index = tile.x + tile.y * rows;
                    VerticesVisibleTiles[vertex.Id].UnionWith(cellIndexToTriangleIndexes[index]);
                }

                AllVerticesVisibleTiles.UnionWith(VerticesVisibleTiles[vertex.Id]);
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