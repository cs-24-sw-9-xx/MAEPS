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

using System;
using System.Collections.Generic;

using UnityEngine;

namespace Tests.PlayModeTests.Utilities.MapInterpreter
{
    public class TileMapParser
    {
        public TileMapParser(string map, char delimiter = ';')
        {
            // Check if editor is running under windows
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                // Replace all \r\n with \n
                map = map.Replace("\r\n", "\n");
            }

            _lines = map.Split(delimiter, StringSplitOptions.RemoveEmptyEntries);
            Width = _lines[0].Length;
            Height = _lines.Length;

            ValidateMap(_lines);
        }

        private readonly string[] _lines;
        public int Height { get; }
        public int Width { get; }

        public IEnumerable<(char tileChar, int x, int y)> GetTiles()
        {
            for (var y = 0; y < Height; y++)
            {
                for (var x = 0; x < Width; x++)
                {
                    yield return (_lines[y][x], x, Height - 1 - y);
                }
            }
        }

        private static void ValidateMap(string[] mapLines)
        {
            if (mapLines.Length == 0)
            {
                throw new FormatException("Map is empty.");
            }

            var expectedWidth = mapLines[0].Length;
            for (var y = 0; y < mapLines.Length; y++)
            {
                if (mapLines[y].Length != expectedWidth)
                {
                    throw new FormatException($"Line {y + 1} has inconsistent width.");
                }
            }
        }
    }
}