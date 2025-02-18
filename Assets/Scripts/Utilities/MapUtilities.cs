using Maes.Map;
using Maes.Map.MapGen;
using Maes.Map.MapPatrollingGen;

namespace Maes.Utilities
{
    public static class MapUtilities
    {
        public static BitMap2D MapToBitMap(SimulationMap<Tile> simulationMap)
        {
            var map = new BitMap2D(simulationMap.WidthInTiles, simulationMap.HeightInTiles);
            for (var y = 0; y < simulationMap.HeightInTiles; y++)
            {
                for (var x = 0; x < simulationMap.WidthInTiles; x++)
                {
                    var tile = simulationMap.GetTileByLocalCoordinate(x, y);
                    var firstTri = tile.GetTriangles()[0];
                    map[y, x] = Tile.IsWall(firstTri.Type);
                }
            }

            return map;
        }
    }
}