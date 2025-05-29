
using UnityEngine;

namespace Maes.Map.Generators
{
    public readonly struct BitmapConfig : IMapConfig
    {
        public BitmapConfig(Tile[,] bitmap, int seed, int borderSize = 1, bool brokenCollisionMap = true)
        {
            Bitmap = bitmap;
            Seed = seed;
            BorderSize = borderSize;
            BrokenCollisionMap = brokenCollisionMap;
        }

        public Tile[,] Bitmap { get; }
        public int Seed { get; }
        public int BorderSize { get; }
        public bool BrokenCollisionMap { get; }

        public readonly SimulationMap<Tile> GenerateMap(GameObject gameObject, float wallHeight = 2)
        {
            var bitMapGenerator = gameObject.AddComponent<BitMapGenerator>();
            return bitMapGenerator.CreateMapFromBitMap(Bitmap, Seed, wallHeight, BorderSize, BrokenCollisionMap);
        }
    }
}