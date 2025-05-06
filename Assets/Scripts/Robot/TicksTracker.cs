using System.Collections.Generic;

using Maes.Map;

using UnityEngine;

namespace Maes.Robot
{
    public class TicksTracker
    {
        public TicksTracker(IRobotController controller)
        {
            _controller = controller;
        }

        private readonly Dictionary<(int fromVertexId, int toVertexId), (int? actualTicks, int overEstimatedTicks)> _ticksByRoute = new();
        private Vertex? _previousVertex;
        private int _previousCapturedTick;
        private readonly IRobotController _controller;

        public int GetTicks(Vertex from, Vertex to)
        {
            if (from.Id == to.Id)
            {
                return 0;
            }

            var path = (from.Id, to.Id);
            if (_ticksByRoute.TryGetValue(path, out var value))
            {
                return value.actualTicks ?? value.overEstimatedTicks;
            }

            var overEstimatedTicks = OverEstimatedTicks(from.Position, to.Position);
            _ticksByRoute[path] = (null, overEstimatedTicks);
            return overEstimatedTicks;
        }

        public void Visited(Vertex position, int atTick)
        {
            // if we don't have a previous position, we can't set the ticks for the route
            if (_previousVertex == null)
            {
                return;
            }

            // if the previous position is the same as the current one, we don't need to set the ticks
            if (_previousVertex.Id == position.Id)
            {
                return;
            }

            var travelTimeTicks = atTick - _previousCapturedTick;

            var path = (_previousVertex.Id, position.Id);
            if (_ticksByRoute.TryGetValue(path, out var value))
            {
                var (actualTicks, overEstimatedTicks) = value;
                if (actualTicks != null)
                {
                    actualTicks = (travelTimeTicks + actualTicks) / 2;
                }
                else
                {
                    actualTicks = travelTimeTicks;
                }

                value = (actualTicks, overEstimatedTicks);
            }
            else
            {
                value = (travelTimeTicks, OverEstimatedTicks(_previousVertex.Position, position.Position));
            }

            _ticksByRoute[path] = EnsureActualNotGreaterThanOverEstimated(value.actualTicks!.Value, value.overEstimatedTicks);

            _previousVertex = position;
            _previousCapturedTick = atTick;
        }

        private static (int?, int) EnsureActualNotGreaterThanOverEstimated(int actualTicks, int overEstimatedTicks)
        {
            if (actualTicks > overEstimatedTicks)
            {
                actualTicks = overEstimatedTicks;
            }

            return (actualTicks, overEstimatedTicks);
        }

        private int OverEstimatedTicks(Vector2Int from, Vector2Int to)
        {
            return _controller.TravelEstimator.OverEstimateTime(from, to) ?? int.MaxValue;
        }
    }
}