
using System;

using UnityEngine;
using UnityEngine.WSA;

namespace Maes.Map.Generators
{
    public class CommonMaps
    {
        private static SimulationMap<Tile> GenerateSimulationMapFromBitmap(string bitmapString)
        {
            var lines = bitmapString.Split(';', StringSplitOptions.RemoveEmptyEntries);
            var width = lines[0].Length;
            var height = lines.Length;
            var tiles = new SimulationMapTile<Tile>[width, height];

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var tileChar = lines[y][x];
                    var tile = tileChar == 'X' ? new Tile(TileType.Wall) : new Tile(TileType.Room);
                    tiles[x, y] = new SimulationMapTile<Tile>(() => tile);
                }
            }

            return new SimulationMap<Tile>(tiles, Vector2.zero);
        }
        
        public SimulationMap<Tile> BlankMap()
        {
            const string bitmapString = "" +
                                        "                                                    ;" +
                                        " XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX ;" +
                                        " X                                                X ;" +
                                        " X                                                X ;" +
                                        " X                                                X ;" +
                                        " X                                                X ;" +
                                        " X                                                X ;" +
                                        " X                                                X ;" +
                                        " X                                                X ;" +
                                        " X                                                X ;" +
                                        " X                                                X ;" +
                                        " X                                                X ;" +
                                        " X                                                X ;" +
                                        " X                                                X ;" +
                                        " X                                                X ;" +
                                        " X                                                X ;" +
                                        " X                                                X ;" +
                                        " X                                                X ;" +
                                        " X                                                X ;" +
                                        " X                                                X ;" +
                                        " X                                                X ;" +
                                        " X                                                X ;" +
                                        " X                                                X ;" +
                                        " X                                                X ;" +
                                        " X                                                X ;" +
                                        " X                                                X ;" +
                                        " X                                                X ;" +
                                        " X                                                X ;" +
                                        " X                                                X ;" +
                                        " X                                                X ;" +
                                        " X                                                X ;" +
                                        " X                                                X ;" +
                                        " X                                                X ;" +
                                        " X                                                X ;" +
                                        " X                                                X ;" +
                                        " X                                                X ;" +
                                        " X                                                X ;" +
                                        " X                                                X ;" +
                                        " X                                                X ;" +
                                        " X                                                X ;" +
                                        " X                                                X ;" +
                                        " XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX ;" +
                                        "                                                     ";
            return GenerateSimulationMapFromBitmap(bitmapString);

        }

        public SimulationMap<Tile> GridMap()
        {
            const string bitmapString = "" +
                                        "                                                    ;" +
                                        " XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX ;" +
                                        " XXXX                                       XXXXXXX ;" +
                                        " XXXX                                       XXXXXXX ;" +
                                        " XXXX                                        XXXXXX ;" +
                                        " XXXX   XXX   XXX   XXX   XXX   XXX   XXX    XXXXXX ;" +
                                        " XXXX   XXX   XXX   XXX   XXX   XXX   XXX     XXXXX ;" +
                                        " XXXX   XXX   XXX   XXX   XXX   XXX   XXX     XXXXX ;" +
                                        " XXXX                                         XXXXX ;" +
                                        " XXXX                                         XXXXX ;" +
                                        " XXXX                                          XXXX ;" +
                                        " XXXX   XXX   XXX   XXX   XXX   XXX   XXX   X  XXXX ;" +
                                        " XXXX   XXX   XXX   XXX   XXX   XXX   XXX      XXXX ;" +
                                        " XXXX   XXX   XXX   XXX   XXX   XXX   XXX       XXX ;" +
                                        " XXXX                                           XXX ;" +
                                        " XXXX                                       X    XX ;" +
                                        " XXXX                                            XX ;" +
                                        " XXXX   XXX   XXX   XXX   XXX   XXX   XXX    X   XX ;" +
                                        " XXXX   XXX   XXX   XXX   XXX   XXX   XXX        XX ;" +
                                        " XXXX   XXX   XXX   XXX   XXX   XXX   XXX   X     X ;" +
                                        " XXXX                                        X    X ;" +
                                        " XXXX                                             X ;" +
                                        " XXXX                                       XX    X ;" +
                                        " XXXX   XXX   XXX   XXX   XXX   XXX   XXX  XXX    X ;" +
                                        " XXXX   XXX   XXX   XXX   XXX   XXX   XXX  X     XX ;" +
                                        " XXXX   XXX   XXX   XXX   XXX   XXX   XXX  X     XX ;" +
                                        " XXXX                                       X    XX ;" +
                                        " XXXX                                      XX    XX ;" +
                                        " XXXX                                      X    XXX ;" +
                                        " XXXX   XXX   XXX   XXX   XXX   XXX   XXX       XXX ;" +
                                        " XXXX   XXX   XXX   XXX   XXX   XXX   XXX      XXXX ;" +
                                        " XXXX   XXX   XXX   XXX   XXX   XXX   XXX   X  XXXX ;" +
                                        " XXXX                                      X   XXXX ;" +
                                        " XXXX                                      X  XXXXX ;" +
                                        " XXXX                                         XXXXX ;" +
                                        " XXXX   XXX   XXX   XXX   XXX   XXX   XXX     XXXXX ;" +
                                        " XXXX   XXX   XXX   XXX   XXX   XXX   XXX     XXXXX ;" +
                                        " XXXX   XXX   XXX   XXX   XXX   XXX   XXX    XXXXXX ;" +
                                        " XXXX                                        XXXXXX ;" +
                                        " XXXX                                       XXXXXXX ;" +
                                        " XXXX                                       XXXXXXX ;" +
                                        " XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX ;" +
                                        "                                                     ";
            return GenerateSimulationMapFromBitmap(bitmapString);

        }
        public SimulationMap<Tile> CorridorMap()
        {
            const string bitmapString = "" +
                                        "                                                    ;" +
                                        " XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX ;" +
                                        " XXX        XX        XX                       XXXX ;" +
                                        " XXX        XX        XX                       XXXX ;" +
                                        " XXX        XX        XX                       XXXX ;" +
                                        " XXX   XX   XX   XX   XX   XXXXXXXXXXXXXXXXXXXXXXXX ;" +
                                        " XXX   XX   XX   XX   XX   XXXXXXXXXXXXXXXXXXXXXXXX ;" +
                                        " XXX   XX   XX   XX   XX   XXXXXXXXXXXXXXXXXXXXXXXX ;" +
                                        " XXX   XX   XX   XX   XX   XXXXXXXXXXXXXXXXXXXXXXXX ;" +
                                        " XXX   XX   XX   XX   XX   XXXXXXXXXXXXXXXXXXXXXXXX ;" +
                                        " XXX   XX   XX   XX   XX   XXXXXXXXXXXXXXXXXXXXXXXX ;" +
                                        " XXX   XX   XX   XX   XX   XXXXXXXXXXXXXXXXXXXXXXXX ;" +
                                        " XXX   XX   XX   XX   XX   XXXXXXXXXXXXXXXXXXXXXXXX ;" +
                                        " XXX   XX   XX   XX   XX   XXXXXXXXXXXXXXXXXXXXXXXX ;" +
                                        " XXX   XX   XX   XX   XX   XXXXXXXXXXXXXXXXXXXXXXXX ;" +
                                        " XXX   XX   XX   XX   XX   XXXXXXXXXXXXXXXXXXXXXXXX ;" +
                                        " XXX   XX   XX   XX   XX   XXXXXXXXXXXXXXXXXXXXXXXX ;" +
                                        " XXX   XX   XX   XX   XX   XXXXXXXXXXXXXXXXXXXXXXXX ;" +
                                        " XXX   XX   XX   XX   XX   XXXXXXXXXXXXXXXXXXXXXXXX ;" +
                                        " XXX   XX   XX   XX   XX   XXXXXXXXXXXXXXXXXXXXXXXX ;" +
                                        " XXX   XX   XX   XX   XX   XXXXXXXXXXXXXXXXXXXXXXXX ;" +
                                        " XXX   XX   XX   XX   XX   XXXXXXXXXXXXXXXXXXXXXXXX ;" +
                                        " XXX   XX   XX   XX   XX   XXXXXXXXXXXXXXXXXXXXXXXX ;" +
                                        " XXX   XX   XX   XX   XX   XXXXXXXXXXXXXXXXXXXXXXXX ;" +
                                        " XXX   XX   XX   XX   XX   XXXXXXXXXXXXXXXXXXXXXXXX ;" +
                                        " XXX   XX   XX   XX   XX   XXXXXXXXXXXXXXXXXXXXXXXX ;" +
                                        " XXX   XX   XX   XX   XX   XXXXXXXXXXXXXXXXXXXXXXXX ;" +
                                        " XXX   XX   XX   XX   XX   XXXXXXXXXXXXXXXXXXXXXXXX ;" +
                                        " XXX   XX   XX   XX   XX   XXXXXXXXXXXXXXXXXXXXXXXX ;" +
                                        " XXX   XX   XX   XX   XX   XXXXXXXXXXXXXXXXXXXXXXXX ;" +
                                        " XXX   XX   XX   XX   XX   XXXXXXXXXXXXXXXXXXXXXXXX ;" +
                                        " XXX   XX   XX   XX   XX   XXXXXXXXXXXXXXXXXXXXXXXX ;" +
                                        " XXX   XX   XX   XX   XX   XXXXXXXXXXXXXXXXXXXXXXXX ;"+
                                        " XXX   XX   XX   XX   XX   XXXXXXXXXXXXXXXXXXXXXXXX ;" +
                                        " XXX                       XXXXXXXXXXXXXXXXXXXXXXXX ;" +
                                        " XXX                       XXXXXXXXXXXXXXXXXXXXXXXX ;" +
                                        " XXX                       XXXXXXXXXXXXXXXXXXXXXXXX ;" +
                                        " XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX ;" +
                                        " XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX ;" +
                                        " XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX ;" +
                                        " XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX ;" +
                                        " XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX ;" +
                                        "                                                     ";
            return GenerateSimulationMapFromBitmap(bitmapString);

        }
        
        public SimulationMap<Tile> IslandsMap()
        {
            const string bitmapString = "" +
                                        "                                                    ;" +
                                        " XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX ;" +
                                        " X         XXXXXXX              XXXXXXX          XX ;" +
                                        " X          XXXX                     X           XX ;" +
                                        " X    X                 XX                  X    XX ;" +
                                        " X  X   X          XX  XXXX               X   X  XX ;" +
                                        " X    X       XXXXXXX  XXXX  XXXXX          X    XX ;" +
                                        " X    X        XXXXXX   XX   XXXXXXXXX      X    XX ;" +
                                        " X              XXXXX        XXXXXXXXXX          XX ;" +
                                        " X    X    X    XXXXXXXXXXXXXXXXXXXXXXXX    X    XX ;" +
                                        " XX       XXX    XXXXXXXXXXXXXXXXXXXXXXXX   X   XXX ;" +
                                        " XXX     XXXXX    XXXXXXXXXXXXXXXXXXXXXXXX      XXX ;" +
                                        " XXX    XXXXXXX    XXXXXXXXXXXXXXXXXXXXXXXX   XXXXX ;" +
                                        " XX    XXXXXXXX     XXXXXXXXXXXXXXXXXXXXXXX   XXXXX ;" +
                                        " XX    XXXXXXXXX     XXXXXXXXXXXXXXXXXXXXXX   XXXXX ;" +
                                        " X    XXXXXXXXXXX             XXXXXXXXXXXXX   XXXXX ;" +
                                        " X       XXXXXXXXX             XXXXXXXXXXXX       X ;" +
                                        " X       XXXXXXXXXX     X       XXXXXXXXXXX       X ;" +
                                        " X   X   XXXXXXXXXX  XX   XX     XXXXXXXXXX   X   X ;" +
                                        " X  XXX  XXXXXXXXXX     X    X    XXXXXXXXX  XXX  X ;" +
                                        " X   X   XXXXXXXXXX     X          XXXXXXXX   X   X ;" +
                                        " X       XXXXXXXXXX                 XXXXXXX       X ;" +
                                        " X       XXXXXXXXXX            X      XXXXX       X ;" +
                                        " XXXXX   XXXXXXXXXXXXXXXXXXXXXXXX     XXXXX   XXXXX ;" +
                                        " XXXXX   XXXXXXXXXXXXXXXXXXXXXXXXX     XXXX   XXXXX ;" +
                                        " XXXX    XXXXXXXXXXXXXXXXXXXXXXXXXX      XX   XXXXX ;" +
                                        " XXXX    XXXXXXXXXXXXXXXXXXXXXXXXXXXX         XXXXX ;" +
                                        " XXXX    XXXXXXXXXXXXXXXXXXXXXXXXXXXXX        XXXXX ;" +
                                        " XXXX   XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX       XXXXX ;" +
                                        " XXXX   XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX      XXXXX ;" +
                                        " XXX     XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX      XXXX ;" +
                                        " XX   X   XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX   X   XXX ;" +
                                        " X    X     XXXXXXXX          XXXXXXXXX     X    XX ;" +
                                        " X          XXXXX                XXXXX           XX ;" +
                                        " X    X                 XX                  X    XX ;" +
                                        " X  X   X XX           XXXX             X X   X  XX ;" +
                                        " X    X         XXXXX  XXXX  XXXXXXX        X    XX ;" +
                                        " X    X      XXXXXXXX   XX   XXXXXXXXX      X    XX ;" +
                                        " X         XXXXXXXXXX        XXXXXXXXXX          XX ;" +
                                        " X         XXXXXXXXXX        XXXXXXXXXXX         XX ;" +
                                        " XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX ;" +
                                        " XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX ;" +
                                        "                                                     ";
            return GenerateSimulationMapFromBitmap(bitmapString);

        }
    }
}