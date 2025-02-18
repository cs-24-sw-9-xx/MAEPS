using Maes.Map;
using Maes.Map.MapGen;

namespace Maes.Utilities
{
    public static class MapUtilities
    {
        public static Bitmap MapToBitMap(SimulationMap<Tile> simulationMap)
        {
            var map = new Bitmap(0, 0, simulationMap.WidthInTiles, simulationMap.HeightInTiles);
            for (var x = 0; x < simulationMap.WidthInTiles; x++)
            {
                for (var y = 0; y < simulationMap.HeightInTiles; y++)
                {
                    var tile = simulationMap.GetTileByLocalCoordinate(x, y);
                    var firstTri = tile.GetTriangles()[0];
                    if (Tile.IsWall(firstTri.Type))
                    {
                        map.Set(x, y);
                    }
                }
            }

            return map;
        }


    }
}