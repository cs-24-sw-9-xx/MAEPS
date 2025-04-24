using System;
using System.Collections.Generic;

namespace Tests.EditModeTests.Utilities.StringMapInterpreter
{
    public class StringMapInterpreter
    {
        public StringMapInterpreter(string map)
        {
            _lines = map.Split(';', StringSplitOptions.RemoveEmptyEntries);
            Width = _lines[0].Length;
            Height = _lines.Length;
        }

        private readonly string[] _lines;
        public int Width { get; }
        public int Height { get; }
            
        public IEnumerable<(char, int, int)> GetTiles()
        {
            for (var y = 0; y < Height; y++)
            {
                for (var x = 0; x < Width; x++)
                {
                    yield return (_lines[y][x], x, y);
                }
            }
        }
    }
}