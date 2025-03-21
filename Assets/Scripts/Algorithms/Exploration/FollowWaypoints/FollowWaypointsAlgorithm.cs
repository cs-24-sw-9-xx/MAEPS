using System.Collections.Generic;
using System.Text;

using Maes.Map;
using Maes.Robot;

using UnityEngine;

namespace Maes.Algorithms.Exploration.FollowWaypoints
{
    public class FollowWaypointsAlgorithm : IExplorationAlgorithm
    {
        // Set by SetController
        private Robot2DController _controller = null!;
        // Set by SetController
        private CoarseGrainedMap _map = null!;
        private static readonly Waypoint[] Waypoints = {
            new(new Vector2Int(5,5)),
            new(new Vector2Int(10,10)),
            new(new Vector2Int(20,20)),
            new(new Vector2Int(30,30))
        };
        private int _currentWaypointIndex;

        private readonly struct Waypoint
        {
            public readonly Vector2Int Destination;

            public Waypoint(Vector2Int destination)
            {
                Destination = destination;
            }
        }

        public IEnumerable<WaitForCondition> PreUpdateLogic()
        {
            while (true)
            {
                yield return WaitForCondition.ContinueUpdateLogic();
            }
        }

        public IEnumerable<WaitForCondition> UpdateLogic()
        {
            while (true)
            {
                if (_currentWaypointIndex >= Waypoints.Length)
                {
                    _controller.StopCurrentTask();
                    yield return WaitForCondition.WaitForLogicTicks(1);
                    continue;
                }

                if (IsDestinationReached())
                {
                    _currentWaypointIndex++;
                    if (_currentWaypointIndex >= Waypoints.Length)
                    {
                        _controller.StopCurrentTask();
                        yield return WaitForCondition.WaitForLogicTicks(1);
                        continue;
                    }
                }

                _controller.PathAndMoveTo(Waypoints[_currentWaypointIndex].Destination);
                yield return WaitForCondition.WaitForLogicTicks(1);
            }
        }

        public void SetController(Robot2DController controller)
        {
            _controller = controller;
            _map = _controller.SlamMap.CoarseMap;
        }

        public string GetDebugInfo()
        {
            return
                new StringBuilder().Append("currentWaypointIndex: ")
                    .Append(_currentWaypointIndex)
                    .Append("\nCoarse Map Position: ")
                    .Append(_map.GetApproximatePosition())
                    .Append("\nCurrent Tile: ")
                    .Append(_map.GetCurrentTile())
                    .Append("\nCurrent position: ")
                    .Append(_map.GetCurrentPosition())
                    .Append("\nDestination: ")
                    .Append(Waypoints[_currentWaypointIndex].Destination)
                    .Append("\nStatus: ")
                    .Append(_controller.Status)
                    .ToString();
        }

        private bool IsDestinationReached()
        {
            return _map.GetTileCenterRelativePosition(Waypoints[_currentWaypointIndex].Destination).Distance < 0.5f;
        }
    }
}