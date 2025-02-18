using System.Collections;
using System.Collections.Generic;

using Maes.Map;
using Maes.Map.MapGen;

using UnityEngine;

namespace Maes.Utilities
{
    public static class MapUtilities
    {
        public static BitMap2D MapToBitMap(SimulationMap<Tile> simulationMap)
        {
            var map = new BitMap2D(simulationMap.WidthInTiles, simulationMap.HeightInTiles);
            for (var height = 0; height < simulationMap.HeightInTiles; height++)
            {
                for (var width = 0; width < simulationMap.WidthInTiles; width++)
                {
                    var tile = simulationMap.GetTileByLocalCoordinate(width, height);
                    var firstTri = tile.GetTriangles()[0];
                    map[height, width] = Tile.IsWall(firstTri.Type);
                }
            }

            return map;
        }
    }
}