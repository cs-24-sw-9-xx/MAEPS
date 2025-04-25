// Copyright 2025 MAEPS
// 
// This file is part of MAEPS
// 
// MAEPS is free software: you can redistribute it and/or modify it under
// the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your option)
// any later version.
// 
// MAEPS is distributed in the hope that it will be useful, but WITHOUT
// ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
// or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General
// Public License for more details.
// 
// You should have received a copy of the GNU General Public License along
// with MAEPS. If not, see http://www.gnu.org/licenses/.
// 
// Contributors: 
// Henrik van Peet,
// Mads Beyer Mogensen,
// Puvikaran Santhirasegaram
// Casper Nyvang Sørensen,
// Christian Ziegler Sejersen,
// Jakob Meyer Olsen

using Maes.Map;
using Maes.Map.Generators;

using UnityEngine;

namespace Tests.EditModeTests.Utilities.MapInterpreter
{
    public class SimulationMapBuilder
    {
        public SimulationMapBuilder(string map)
        {
            _tileMapParser = new TileMapParser(map);
            _tiles = new SimulationMapTile<Tile>[_tileMapParser.Width, _tileMapParser.Height];
        }

        private readonly TileMapParser _tileMapParser;
        private readonly SimulationMapTile<Tile>[,] _tiles;

        private Vector2 _start = Vector2.zero;
        private Vector2 _end = Vector2.zero;

        public ((Vector2 start, Vector2 end), SimulationMap<Tile> map) BuildMap()
        {
            foreach (var (tileChar, x, y) in _tileMapParser.GetTiles())
            {
                InterpretTile(tileChar, x, y);
            }

            return ((_start, _end), new SimulationMap<Tile>(_tiles, Vector2.zero));
        }

        protected virtual void InterpretTile(char tileChar, int x, int y)
        {
            switch (tileChar)
            {
                case 'S' or 's':
                    _start = CreateVector2(x, y);
                    break;
                case 'E' or 'e':
                    _end = CreateVector2(x, y);
                    break;
            }

            tileChar = tileChar switch
            {
                'S' => 'X',
                's' => ' ',
                'E' => 'X',
                'e' => ' ',
                _ => tileChar
            };

            _tiles[x, y] = new SimulationMapTile<Tile>(() => MapCharToTile(tileChar));
        }

        private static Vector2 CreateVector2(int x, int y)
        {
            return new Vector2(x, y) + Vector2.one / 2f;
        }

        private static Tile MapCharToTile(char c)
        {
            var type = c switch
            {
                'X' => TileType.Wall,
                _ => TileType.Room
            };

            return new Tile(type);
        }

    }
}