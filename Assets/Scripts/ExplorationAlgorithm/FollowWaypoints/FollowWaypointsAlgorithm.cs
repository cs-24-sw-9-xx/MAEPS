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
        private readonly Waypoint[] _waypoints = {
            new(new Vector2Int(5,5)),
            new(new Vector2Int(10,10)),
            new(new Vector2Int(20,20)),
            new(new Vector2Int(30,30))
        };
        private int _currentWaypointIndex;

        private struct Waypoint
        {
            public readonly Vector2Int Destination;

            public Waypoint(Vector2Int destination)
            {
                Destination = destination;
            }
        }

        public void UpdateLogic()
        {
            if (_currentWaypointIndex >= _waypoints.Length)
            {
                _controller.StopCurrentTask();
                return;
            }
            
            if (IsDestinationReached())
            {
                _currentWaypointIndex++;
                if (_currentWaypointIndex >= _waypoints.Length)
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