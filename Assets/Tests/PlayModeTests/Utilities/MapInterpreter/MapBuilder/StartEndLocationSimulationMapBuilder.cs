using Maes.Map;
using Maes.Map.Generators;

using UnityEngine;

namespace Tests.PlayModeTests.Utilities.MapInterpreter.MapBuilder
{
    public class StartEndLocationSimulationMapBuilder : BaseSimulationMapBuilder<(Vector2 start, Vector2 end, SimulationMap<Tile> map)>
    {
        public StartEndLocationSimulationMapBuilder(string map) : base(map)
        {

        }

        private Vector2 _start = Vector2.zero;
        private Vector2 _end = Vector2.zero;

        protected override (Vector2 start, Vector2 end, SimulationMap<Tile> map) BuildResult(SimulationMap<Tile> map)
        {
            return (_start, _end, map);
        }

        protected override void InterpretTile(char tileChar, int x, int y)
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

            base.InterpretTile(tileChar, x, y);
        }

        private static Vector2 CreateVector2(int x, int y)
        {
            return new Vector2(x, y) + (Vector2.one / 2f);
        }
    }
}