
using System;

namespace Maes.Map.Generators
{
/// <summary>
/// Provides static methods for generating common map layouts used in multi-agent patrolling research.
/// Each method returns a 2D array of Tiles representing a specific map layout.
/// </summary>
public static class CommonMaps
{
        private static Tile[,] GenerateBitmap(string bitmapString)
        {
            var lines = bitmapString.Split(';', StringSplitOptions.RemoveEmptyEntries);
            var width = lines[0].Length;
            var height = lines.Length;
            
            // Validate that all lines have the same length
            for (var i = 1; i < lines.Length; i++)
            {
                if (lines[i].Length != width)
                {
                    throw new ArgumentException($"Line {i} has length {lines[i].Length}, expected {width}");
                }
            }
            
            var tiles = new Tile[width, height];

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    // Reverse y index to flip vertically (making the bitmap read from bottom to top)
                    var tileChar = lines[height - 1 - y][x];
                    var tile = tileChar == 'X' ? TileType.Wall : TileType.Room;
                    tiles[x, y] = new Tile(tile);
                }
            }

            return tiles;
        }

        public static Tile[,] GridMap()
        {
            const string bitmapString = "" +
                                        "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX;" +
                                        "XXXX                                       XXXXXXX;" +
                                        "XXXX                                       XXXXXXX;" +
                                        "XXXX                                        XXXXXX;" +
                                        "XXXX   XXX   XXX   XXX   XXX   XXX   XXX    XXXXXX;" +
                                        "XXXX   XXX   XXX   XXX   XXX   XXX   XXX     XXXXX;" +
                                        "XXXX   XXX   XXX   XXX   XXX   XXX   XXX     XXXXX;" +
                                        "XXXX                                         XXXXX;" +
                                        "XXXX                                         XXXXX;" +
                                        "XXXX                                          XXXX;" +
                                        "XXXX   XXX   XXX   XXX   XXX   XXX   XXX   X  XXXX;" +
                                        "XXXX   XXX   XXX   XXX   XXX   XXX   XXX      XXXX;" +
                                        "XXXX   XXX   XXX   XXX   XXX   XXX   XXX       XXX;" +
                                        "XXXX                                           XXX;" +
                                        "XXXX                                       X    XX;" +
                                        "XXXX                                            XX;" +
                                        "XXXX   XXX   XXX   XXX   XXX   XXX   XXX    X   XX;" +
                                        "XXXX   XXX   XXX   XXX   XXX   XXX   XXX        XX;" +
                                        "XXXX   XXX   XXX   XXX   XXX   XXX   XXX   X     X;" +
                                        "XXXX                                        X    X;" +
                                        "XXXX                                             X;" +
                                        "XXXX                                       XX    X;" +
                                        "XXXX   XXX   XXX   XXX   XXX   XXX   XXX  XXX    X;" +
                                        "XXXX   XXX   XXX   XXX   XXX   XXX   XXX  X     XX;" +
                                        "XXXX   XXX   XXX   XXX   XXX   XXX   XXX  X     XX;" +
                                        "XXXX                                       X    XX;" +
                                        "XXXX                                      XX    XX;" +
                                        "XXXX                                      X    XXX;" +
                                        "XXXX   XXX   XXX   XXX   XXX   XXX   XXX       XXX;" +
                                        "XXXX   XXX   XXX   XXX   XXX   XXX   XXX      XXXX;" +
                                        "XXXX   XXX   XXX   XXX   XXX   XXX   XXX   X  XXXX;" +
                                        "XXXX                                      X   XXXX;" +
                                        "XXXX                                      X  XXXXX;" +
                                        "XXXX                                         XXXXX;" +
                                        "XXXX   XXX   XXX   XXX   XXX   XXX   XXX     XXXXX;" +
                                        "XXXX   XXX   XXX   XXX   XXX   XXX   XXX     XXXXX;" +
                                        "XXXX   XXX   XXX   XXX   XXX   XXX   XXX    XXXXXX;" +
                                        "XXXX                                        XXXXXX;" +
                                        "XXXX                                       XXXXXXX;" +
                                        "XXXX                                       XXXXXXX;" +
                                        "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX;";
            return GenerateBitmap(bitmapString);

        }
        public static Tile[,] CorridorMap()
        {
            const string bitmapString = "" +
                                        "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX;" +
                                        "XXX        XX        XX                       XXXX;" +
                                        "XXX        XX        XX                       XXXX;" +
                                        "XXX        XX        XX                       XXXX;" +
                                        "XXX   XX   XX   XX   XX   XXXXXXXXXXXXXXXXXXXXXXXX;" +
                                        "XXX   XX   XX   XX   XX   XXXXXXXXXXXXXXXXXXXXXXXX;" +
                                        "XXX   XX   XX   XX   XX   XXXXXXXXXXXXXXXXXXXXXXXX;" +
                                        "XXX   XX   XX   XX   XX   XXXXXXXXXXXXXXXXXXXXXXXX;" +
                                        "XXX   XX   XX   XX   XX   XXXXXXXXXXXXXXXXXXXXXXXX;" +
                                        "XXX   XX   XX   XX   XX   XXXXXXXXXXXXXXXXXXXXXXXX;" +
                                        "XXX   XX   XX   XX   XX   XXXXXXXXXXXXXXXXXXXXXXXX;" +
                                        "XXX   XX   XX   XX   XX   XXXXXXXXXXXXXXXXXXXXXXXX;" +
                                        "XXX   XX   XX   XX   XX   XXXXXXXXXXXXXXXXXXXXXXXX;" +
                                        "XXX   XX   XX   XX   XX   XXXXXXXXXXXXXXXXXXXXXXXX;" +
                                        "XXX   XX   XX   XX   XX   XXXXXXXXXXXXXXXXXXXXXXXX;" +
                                        "XXX   XX   XX   XX   XX   XXXXXXXXXXXXXXXXXXXXXXXX;" +
                                        "XXX   XX   XX   XX   XX   XXXXXXXXXXXXXXXXXXXXXXXX;" +
                                        "XXX   XX   XX   XX   XX   XXXXXXXXXXXXXXXXXXXXXXXX;" +
                                        "XXX   XX   XX   XX   XX   XXXXXXXXXXXXXXXXXXXXXXXX;" +
                                        "XXX   XX   XX   XX   XX   XXXXXXXXXXXXXXXXXXXXXXXX;" +
                                        "XXX   XX   XX   XX   XX   XXXXXXXXXXXXXXXXXXXXXXXX;" +
                                        "XXX   XX   XX   XX   XX   XXXXXXXXXXXXXXXXXXXXXXXX;" +
                                        "XXX   XX   XX   XX   XX   XXXXXXXXXXXXXXXXXXXXXXXX;" +
                                        "XXX   XX   XX   XX   XX   XXXXXXXXXXXXXXXXXXXXXXXX;" +
                                        "XXX   XX   XX   XX   XX   XXXXXXXXXXXXXXXXXXXXXXXX;" +
                                        "XXX   XX   XX   XX   XX   XXXXXXXXXXXXXXXXXXXXXXXX;" +
                                        "XXX   XX   XX   XX   XX   XXXXXXXXXXXXXXXXXXXXXXXX;" +
                                        "XXX   XX   XX   XX   XX   XXXXXXXXXXXXXXXXXXXXXXXX;" +
                                        "XXX   XX   XX   XX   XX   XXXXXXXXXXXXXXXXXXXXXXXX;" +
                                        "XXX   XX   XX   XX   XX   XXXXXXXXXXXXXXXXXXXXXXXX;" +
                                        "XXX   XX   XX   XX   XX   XXXXXXXXXXXXXXXXXXXXXXXX;" +
                                        "XXX   XX   XX   XX   XX   XXXXXXXXXXXXXXXXXXXXXXXX;" +
                                        "XXX   XX   XX   XX   XX   XXXXXXXXXXXXXXXXXXXXXXXX;" +
                                        "XXX   XX        XX        XXXXXXXXXXXXXXXXXXXXXXXX;" +
                                        "XXX   XX        XX        XXXXXXXXXXXXXXXXXXXXXXXX;" +
                                        "XXX   XX        XX        XXXXXXXXXXXXXXXXXXXXXXXX;" +
                                        "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX;" +
                                        "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX;" +
                                        "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX;" +
                                        "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX;" +
                                        "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX;";
            return GenerateBitmap(bitmapString);

        }

        public static Tile[,] IslandsMap()
        {
            const string bitmapString = "" +
                                        "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX;" +
                                        "X         XXXXXXX              XXXXXXX          XX;" +
                                        "X          XXXX                     X           XX;" +
                                        "X    X                 XX                  X    XX;" +
                                        "X  X   X          XX  XXXX               X   X  XX;" +
                                        "X    X       XXXXXXX  XXXX  XXXXX          X    XX;" +
                                        "X    X        XXXXXX   XX   XXXXXXXXX      X    XX;" +
                                        "X              XXXXX        XXXXXXXXXX          XX;" +
                                        "X    X    X    XXXXXXXXXXXXXXXXXXXXXXXX    X    XX;" +
                                        "XX       XXX    XXXXXXXXXXXXXXXXXXXXXXXX   X   XXX;" +
                                        "XXX     XXXXX    XXXXXXXXXXXXXXXXXXXXXXXX      XXX;" +
                                        "XXX    XXXXXXX    XXXXXXXXXXXXXXXXXXXXXXXX   XXXXX;" +
                                        "XX    XXXXXXXX     XXXXXXXXXXXXXXXXXXXXXXX   XXXXX;" +
                                        "XX    XXXXXXXXX     XXXXXXXXXXXXXXXXXXXXXX   XXXXX;" +
                                        "X    XXXXXXXXXXX             XXXXXXXXXXXXX   XXXXX;" +
                                        "X       XXXXXXXXX             XXXXXXXXXXXX       X;" +
                                        "X       XXXXXXXXXX     X       XXXXXXXXXXX       X;" +
                                        "X   X   XXXXXXXXXX  XX   XX     XXXXXXXXXX   X   X;" +
                                        "X  XXX  XXXXXXXXXX     X    X    XXXXXXXXX  XXX  X;" +
                                        "X   X   XXXXXXXXXX     X          XXXXXXXX   X   X;" +
                                        "X       XXXXXXXXXX                 XXXXXXX       X;" +
                                        "X       XXXXXXXXXX            X      XXXXX       X;" +
                                        "XXXXX   XXXXXXXXXXXXXXXXXXXXXXXX     XXXXX   XXXXX;" +
                                        "XXXXX   XXXXXXXXXXXXXXXXXXXXXXXXX     XXXX   XXXXX;" +
                                        "XXXX    XXXXXXXXXXXXXXXXXXXXXXXXXX      XX   XXXXX;" +
                                        "XXXX    XXXXXXXXXXXXXXXXXXXXXXXXXXXX         XXXXX;" +
                                        "XXXX    XXXXXXXXXXXXXXXXXXXXXXXXXXXXX        XXXXX;" +
                                        "XXXX   XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX       XXXXX;" +
                                        "XXXX   XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX      XXXXX;" +
                                        "XXX     XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX      XXXX;" +
                                        "XX   X   XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX   X   XXX;" +
                                        "X    X     XXXXXXXX          XXXXXXXXX     X    XX;" +
                                        "X          XXXXX                XXXXX           XX;" +
                                        "X    X                 XX                  X    XX;" +
                                        "X  X   X XX           XXXX             X X   X  XX;" +
                                        "X    X         XXXXX  XXXX  XXXXXXX        X    XX;" +
                                        "X    X      XXXXXXXX   XX   XXXXXXXXX      X    XX;" +
                                        "X         XXXXXXXXXX        XXXXXXXXXX          XX;" +
                                        "X         XXXXXXXXXX        XXXXXXXXXXX         XX;" +
                                        "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX;" +
                                        "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX;";
            return GenerateBitmap(bitmapString);

        }

        public static Tile[,] MapAMap()
        {
            const string bitmapString = "" +
                                        "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX;" +
                                        "X                                                  X;" +
                                        "X                                                  X;" +
                                        "X                        XXX                       X;" +
                                        "X   XXX   XXX          XXXXXXX                     X;" +
                                        "X   XXX   XXX         XXXXXXXXX                    X;" +
                                        "X   XXX   XXX         XXXXXXXXX                    X;" +
                                        "X   XXX   XXX          XXXXXXX                     X;" +
                                        "X   XXX   XXX            XXX          XXXXX   XXXXXX;" +
                                        "X   XXX   XXX                         XXXXX   XXXXXX;" +
                                        "X                                                  X;" +
                                        "X                                                  X;" +
                                        "X       XX                                         X;" +
                                        "X      XXXX                XXX                     X;" +
                                        "X     XXXXXX             XXXXXXX               XXXXX;" +
                                        "X      XXXX           XXXXXXXXXXXXX            XXXXX;" +
                                        "X       XX                                     XXXXX;" +
                                        "X                                              XXXXX;" +
                                        "X                                                  X;" +
                                        "X   XXXXXXX                                        X;" +
                                        "X   XXXXXXX                                        X;" +
                                        "X       XXXXXX                             XXXXX   X;" +
                                        "X       XXXXXX                             XXXXX   X;" +
                                        "X   XXXXXXX                                        X;" +
                                        "X   XXXXXXX                               XXXXXX   X;" +
                                        "X                        X                 XXXXX   X;" +
                                        "X                       XXX                XXXXX   X;" +
                                        "X                      XXXXX                XXXX   X;" +
                                        "X   XXXXXX            XXXXXXX                 XX   X;" +
                                        "X   XXXXX            XXXXXXXXX                     X;" +
                                        "X   XXXX                                           X;" +
                                        "X   XXX                                            X;" +
                                        "X   XX                                             X;" +
                                        "X   X           XXXXXX         XXX                 X;" +
                                        "X               XXXXXX       XXXXXXX               X;" +
                                        "X               XXXXXX      XXXXXXXXXXX            X;" +
                                        "X               XXXXXX     XXXXXXXXXXXXX           X;" +
                                        "X          X              XXXXXXXXXXXXXXXXX        X;" +
                                        "X          XXX            XXXXXXXXXXXXXXXXX        X;" +
                                        "X          XXXXXXXXXXX                             X;" +
                                        "X                                                  X;" +
                                        "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX;";
            return GenerateBitmap(bitmapString);

        }

        public static Tile[,] MapBMap()
        {
            const string bitmapString = "" +
                                        "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX;" +
                                        "X                X                    XXXXXX       X;" +
                                        "X                X                     XXXXX       X;" +
                                        "X                X       XXX            XXXX       X;" +
                                        "X   XXX   XXX    X     XXXXXXX    XXX    XXX       X;" +
                                        "X   XXX   XXX    X    XXXXXXXXX   XXXX    XX       X;" +
                                        "X   XXX   XXX    X    XXXXXXXXX   XXXXXXXXXX       X;" +
                                        "X   XXX   XXX    X     XXXXXXX    X                X;" +
                                        "X   XXX   XXX    X       XXX      X   XXXXX   XXXXXX;" +
                                        "X   XXX   XXX    X                X   XXXXX   XXXXXX;" +
                                        "X                X                X                X;" +
                                        "X                X                XXXXXXX          X;" +
                                        "X       XX       X                      X          X;" +
                                        "X      XXXX      X         XXX          X          X;" +
                                        "X     XXXXXX     X       XXXXXXX        X      XXXXX;" +
                                        "X      XXXX      X    XXXXXXXXXXXXX     X      XXXXX;" +
                                        "X       XX       X                      X      XXXXX;" +
                                        "X                X                      X      XXXXX;" +
                                        "X                X                      X          X;" +
                                        "X   XXXXXXX      XXXXXX     XXXXXXXXXXXXX          X;" +
                                        "X   XXXXXXX                                        X;" +
                                        "X       XXXXXX                             XXXXX   X;" +
                                        "X       XXXXXX                             XXXXX   X;" +
                                        "X   XXXXXXX       XXXXX     XXXXXXXX               X;" +
                                        "X   XXXXXXX      X                  X     XXXXXX   X;" +
                                        "X               X        X           X     XXXXX   X;" +
                                        "X              X        XXX           X    XXXXX   X;" +
                                        "X             X        XXXXX           X    XXXX   X;" +
                                        "X   XXXXXX   X        XXXXXXX           X     XX   X;" +
                                        "X   XXXXX   X        XXXXXXXXX           X         X;" +
                                        "X   XXXX   X                              X        X;" +
                                        "X   XXX   X                                X       X;" +
                                        "X   XX   X                                  X      X;" +
                                        "X   X   X       XXXXXX         XXX          XXXXXXXX;" +
                                        "X      X        XXXXXX       XXXXXXX               X;" +
                                        "X     X         XXXXXX      XXXXXXXXXXX            X;" +
                                        "X    X          XXXXXX     XXXXXXXXXXXXX           X;" +
                                        "X   X      X              XXXXXXXXXXXXXXXXX        X;" +
                                        "X  X       XXX            XXXXXXXXXXXXXXXXX        X;" +
                                        "X X        XXXXXXXXXXX                             X;" +
                                        "XX                                                 X;" +
                                        "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX;";
            return GenerateBitmap(bitmapString);

        }

        public static Tile[,] CircularMap()
        {
            const string bitmapString = "" +
                                        "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX;" +
                                        "XXXXXXXXXXXXXXXXXX                             XXX;" +
                                        "XXXXXXXXXXXXXX                                 XXX;" +
                                        "XXXXXXXXXX               X                      XX;" +
                                        "XXXXXX               XXXXXXX    XXXXXXXXXXXX    XX;" +
                                        "XXXX             XXXXXXXXXXXXXXXXXXXXXXXXXXX    XX;" +
                                        "XXX          XXXXXXXXXXXXXXXXXXXXXXXXXXXXXX     XX;" +
                                        "XX       XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX    XXX;" +
                                        "XX     XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX    XXX;" +
                                        "XX   XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX     XX;" +
                                        "XX   XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX    XX;" +
                                        "XX   XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX    XX;" +
                                        "XX   XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX    XX;" +
                                        "XX   XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX    XX;" +
                                        "XX   XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX    XX;" +
                                        "XX   XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX    XX;" +
                                        "XX   XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX    X;" +
                                        "XX    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX    X;" +
                                        "XX    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX   X;" +
                                        "XXX    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX   X;" +
                                        "XXX     XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX   X;" +
                                        "XXXX    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX   X;" +
                                        "XXXXX   XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX   X;" +
                                        "XXXX    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX    X;" +
                                        "XXXX    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX    X;" +
                                        "XXXX    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX     X;" +
                                        "XXX    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX    XX;" +
                                        "XXX    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX     XX;" +
                                        "XXX    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX    XXX;" +
                                        "XXX   XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX     XXX;" +
                                        "XXX   XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX     XXXX;" +
                                        "XXX   XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX     XXXX;" +
                                        "XXX    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX     XXXXX;" +
                                        "XXX    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX    XXXXXX;" +
                                        "XXXX    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX     XXXXXX;" +
                                        "XXXX     XXXXXXXXXXXXXXXXXXXXXXXXXXXX      XXXXXXX;" +
                                        "XXXXX      XXXXXXXXXXXXXXXXXXXXXXX         XXXXXXX;" +
                                        "XXXXXX       XXXXXXXXXXXXXXXXXX           XXXXXXXX;" +
                                        "XXXXXXX                                 XXXXXXXXXX;" +
                                        "XXXXXXXX                             XXXXXXXXXXXXX;" +
                                        "XXXXXXXXXX                        XXXXXXXXXXXXXXXX;" +
                                        "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX;";
            return GenerateBitmap(bitmapString);

        }
    }
}