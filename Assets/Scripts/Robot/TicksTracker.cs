using System.Collections.Generic;

using UnityEngine;

namespace Maes.Robot
{
    public class TicksTracker
    {
        private readonly Dictionary<(Vector2Int, Vector2Int), int> _ticksByRoute = new();
        private Vector2Int? _previousTrackedPosition;
        private int _previousTrackedTick;

        public int? GetTicks(Vector2Int from, Vector2Int to)
        {
            if (from == to)
            {
                return 0;
            }
            return _ticksByRoute.TryGetValue((from, to), out var ticks) ? ticks : null;
        }

        public void Visited(Vector2Int position, int atTick)
        {
            if (_previousTrackedPosition.HasValue)
            {
                var ticks = atTick - _previousTrackedTick;
                if (_ticksByRoute.TryGetValue((_previousTrackedPosition.Value, position), out var storedTicks))
                {
                    ticks = (ticks + storedTicks) / 2;
                }

                _ticksByRoute[(_previousTrackedPosition.Value, position)] = ticks;
            }

            _previousTrackedPosition = position;
            _previousTrackedTick = atTick;
        }

        public void Reset()
        {
            _previousTrackedPosition = null;
            _previousTrackedTick = 0;
        }
    }
}