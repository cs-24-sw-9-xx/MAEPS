using System.Text;

using Maes.Algorithms;
using Maes.Map;
using Maes.Robot;

using UnityEngine;

namespace Maes.ExplorationAlgorithm.FollowWaypoints
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

        public void UpdateLogic()
        {
            if (_currentWaypointIndex >= Waypoints.Length)
            {
                _controller.StopCurrentTask();
                return;
            }

            if (IsDestinationReached())
            {
                _currentWaypointIndex++;
                if (_currentWaypointIndex >= Waypoints.Length)
                {
                    _controller.StopCurrentTask();
                    return;
                }
            }
            _controller.PathAndMoveTo(Waypoints[_currentWaypointIndex].Destination);
        }

        public void SetController(Robot2DController controller)
        {
            _controller = controller;
            _map = _controller.GetSlamMap().GetCoarseMap();
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
                    .Append(_controller.GetStatus())
                    .ToString();
        }

        private bool IsDestinationReached()
        {
            return _map.GetTileCenterRelativePosition(Waypoints[_currentWaypointIndex].Destination).Distance < 0.5f;
        }
    }
}