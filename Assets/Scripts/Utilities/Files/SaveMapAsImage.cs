using System.Collections.Generic;

using UnityEngine;

namespace Maes.Utilities.Files
{
    public static class SaveAsImage
    {
        public static void SaveVisibleTiles(HashSet<Vector2Int> pointsOfInterest, Vector2Int startPoint, bool optimized, bool[,] map)
        {
            var texture = new Texture2D(map.GetLength(0), map.GetLength(1));
            for (var x = 0; x < map.GetLength(0); x++)
            {
                for (var y = 0; y < map.GetLength(1); y++)
                {
                    if (map[x, y])
                    {
                        texture.SetPixel(x, y, Color.blue);
                    }
                    else
                    {

                        texture.SetPixel(x, y, Color.white);
                    }
                }
            }

            foreach (var point in pointsOfInterest)
            {
                texture.SetPixel(point.x, point.y, Color.black);
            }

            texture.SetPixel(startPoint.x, startPoint.y, Color.red);
            texture.Apply();
            var bytes = texture.EncodeToPNG();
            var filename = optimized ? "optimized.png" : "origin.png";
            System.IO.File.WriteAllBytes(filename, bytes);
        }
    }
}