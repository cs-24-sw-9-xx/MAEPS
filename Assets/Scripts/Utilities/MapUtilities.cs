using Maes.Map;
using Maes.Map.MapGen;

namespace Maes.Utilities
{
    public static class MapUtilities
    {
        public static bool[,] MapToBitMap(SimulationMap<Tile> simulationMap)
        {
            var map = new bool[simulationMap.WidthInTiles, simulationMap.HeightInTiles];
            for (var x = 0; x < simulationMap.WidthInTiles; x++)
            {
                for (var y = 0; y < simulationMap.HeightInTiles; y++)
                {
                    var tile = simulationMap.GetTileByLocalCoordinate(x, y);
                    var firstTri = tile.GetTriangles()[0];
                    map[x, y] = Tile.IsWall(firstTri.Type);
                }
            }

            return map;
        }


    }
}