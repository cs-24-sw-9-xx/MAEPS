using Maes.Map;
using Maes.Map.Generators;

using UnityEngine;

namespace Tests.PlayModeTests.Utilities.MapInterpreter.MapBuilder
{
    public abstract class BaseSimulationMapBuilder<TResult>
    {
        protected BaseSimulationMapBuilder(string map, char delimiter = ';')
        {
            _tileMapParser = new TileMapParser(map, delimiter);
            _tiles = new SimulationMapTile<Tile>[_tileMapParser.Width, _tileMapParser.Height];
        }

        private readonly TileMapParser _tileMapParser;
        private readonly SimulationMapTile<Tile>[,] _tiles;

        public TResult Build()
        {
            foreach (var (tileChar, x, y) in _tileMapParser.GetTiles())
            {
                InterpretTile(tileChar, x, y);
            }

            return BuildResult(new SimulationMap<Tile>(_tiles, Vector2.zero));
        }

        protected abstract TResult BuildResult(SimulationMap<Tile> map);

        protected virtual void InterpretTile(char tileChar, int x, int y)
        {
            _tiles[x, y] = new SimulationMapTile<Tile>(() => MapCharToTile(tileChar));
        }

        private static Tile MapCharToTile(char c)
        {
            var type = c switch
            {
                'X' => TileType.Brick,
                _ => TileType.Room
            };

            return new Tile(type);
        }

    }
}