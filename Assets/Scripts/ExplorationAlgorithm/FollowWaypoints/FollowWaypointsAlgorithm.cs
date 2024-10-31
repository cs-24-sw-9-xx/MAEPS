using System.Collections.Generic;
using Maes.ExplorationAlgorithm;
using Maes.Map;
using Maes.Robot;
using UnityEngine;

namespace MAES.ExplorationAlgorithm.FollowWaypoints
{
    public class FollowWaypointsAlgorithm : IExplorationAlgorithm
    {
        private Robot2DController _controller;
        private CoarseGrainedMap _map;
        private readonly List<Waypoint> _waypoints = new()
        {
            new Waypoint(new Vector2Int(5,5)),
            new Waypoint(new Vector2Int(10,10)),
            new Waypoint(new Vector2Int(20,20)),
            new Waypoint(new Vector2Int(30,30))
        };
        private int _currentWaypointIndex = 0;

        private struct Waypoint
        {
            public Vector2Int Destination;

            public Waypoint(Vector2Int destination)
            {
                Destination = destination;
            }
        }

        public void UpdateLogic()
        {
            if (_currentWaypointIndex >= _waypoints.Count)
            {
                _controller.StopCurrentTask();
                return;
            }

            if (IsDestinationReached())
            {
                _currentWaypointIndex++;
                if (_currentWaypointIndex >= _waypoints.Count)
                {
                    _controller.StopCurrentTask();
                    return;
                }
            }
            _controller.PathAndMoveTo(_waypoints[_currentWaypointIndex].Destination);
        }

        public void SetController(Robot2DController controller)
        {
            _controller = controller;
            _map = _controller.GetSlamMap().GetCoarseMap();
        }

        public string GetDebugInfo()
        {
            return $"currentWaypointIndex: {_currentWaypointIndex}" +
                   $"\nCoarse Map Position: {_map.GetApproximatePosition()}" +
                   $"\nCurrent Tile: {_map.GetCurrentTile()}" +
                   $"\nCurrent position: {_map.GetCurrentPosition()}" +
                   $"\nDestination: {_waypoints[_currentWaypointIndex].Destination}" +
                   $"\nStatus: {_controller.GetStatus()}";
        }

        private bool IsDestinationReached()
        {
            return _map.GetTileCenterRelativePosition(_waypoints[_currentWaypointIndex].Destination).Distance < 0.5f;
        }
    }
}