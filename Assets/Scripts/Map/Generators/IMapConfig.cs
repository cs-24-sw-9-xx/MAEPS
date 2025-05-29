
using UnityEngine;

namespace Maes.Map.Generators
{
    public interface IMapConfig
    {
        public abstract SimulationMap<Tile> GenerateMap(GameObject gameObject, float wallHeight = 2.0f);
        public int WidthInTiles { get; }
        public int HeightInTiles { get; }
    }
}