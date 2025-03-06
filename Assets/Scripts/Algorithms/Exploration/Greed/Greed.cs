// Copyright 2024 MAES
// 
// This file is part of MAES
// 
// MAES is free software: you can redistribute it and/or modify it under
// the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your option)
// any later version.
// 
// MAES is distributed in the hope that it will be useful, but WITHOUT
// ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
// or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General
// Public License for more details.
// 
// You should have received a copy of the GNU General Public License along
// with MAES. If not, see http://www.gnu.org/licenses/.
// 
// Contributors: Rasmus Borrisholt Schmidt, Andreas Sebastian Sørensen, Thor Beregaard
// 
// Original repository: https://github.com/Molitany/MAES

using System;
using System.Collections.Generic;
using System.Linq;

using Maes.Map;
using Maes.Robot;

using UnityEngine;

using static Maes.Map.SlamMap;

namespace Maes.Algorithms.Exploration.Greed
{
    // TODO: Use constructors instead of SetController.
    public class GreedAlgorithm : IExplorationAlgorithm
    {
        // Set by SetController
        private IRobotController _controller = null!;
        // Set by SetController
        private CoarseGrainedMap _map = null!;
        private Vector2Int _position => _map.GetCurrentPosition();
        private AlgorithmState _currentState = AlgorithmState.Idle;

        private Waypoint? _waypoint;
        private int _logicTicks;
        private int _ticksSinceHeartbeat;
        private int _deadlockTimer;
        private Vector2Int _previousPosition;

        private enum AlgorithmState
        {
            Idle,
            ExploreRoom,
            Done
        }

        private struct Waypoint : IEquatable<Waypoint>
        {
            public Vector2Int Destination;
            public WaypointType Type;

            public enum WaypointType
            {
                Greed,
            }

            public Waypoint(Vector2Int destination, WaypointType type)
            {
                Destination = destination;
                Type = type;
            }

            public override bool Equals(object? obj)
            {
                if (obj is not Waypoint other)
                {
                    return false;
                }

                return Equals(other);
            }

            public bool Equals(Waypoint other)
            {
                return Destination.Equals(other.Destination) && Type == other.Type;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Destination, (int)Type);
            }

            public static bool operator ==(Waypoint left, Waypoint right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(Waypoint left, Waypoint right)
            {
                return !left.Equals(right);
            }
        }

        public string GetDebugInfo()
        {
            return $"State: {Enum.GetName(typeof(AlgorithmState), _currentState)}" +
                   $"\nCoarse Map Position: {_map.GetApproximatePosition()}";
        }

        public void SetController(Robot2DController controller)
        {
            _controller = controller;
            _map = _controller.GetSlamMap().GetCoarseMap();
            _previousPosition = _position;
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
                _logicTicks++;
                _ticksSinceHeartbeat++;
                if (_ticksSinceHeartbeat == 10)
                {
                    var ownHeartbeat = new HeartbeatMessage(_controller.GetSlamMap());
                    _ticksSinceHeartbeat = 0;
                    _controller.Broadcast(ownHeartbeat);
                }
                var receivedHeartbeat = new Queue<HeartbeatMessage>(_controller.ReceiveBroadcast().OfType<HeartbeatMessage>());
                if (receivedHeartbeat.Count > 1)
                {
                    var combinedMessage = receivedHeartbeat.Dequeue();
                    foreach (var message in receivedHeartbeat)
                    {
                        combinedMessage = combinedMessage.Combine(message, _logicTicks);
                    }
                }

                if (_controller.IsCurrentlyColliding)
                {
                    if (_controller.GetStatus() != Robot.Tasks.RobotStatus.Idle)
                    {
                        _controller.StopCurrentTask();
                    }
                    else
                    {
                        var openTile = _map.GetNearestTileFloodFill(_position, SlamTileStatus.Open);
                        if (openTile.HasValue)
                        {
                            _controller.MoveTo(openTile.Value);
                        }
                        else
                        {
                            _controller.Move(1, true);
                        }
                    }
                    _waypoint = null;
                    yield return WaitForCondition.WaitForLogicTicks(1);
                    continue;
                    //TODO: full resets
                }

                if (_deadlockTimer >= 5)
                {
                    var waypoint = _waypoint;
                    MoveToNearestUnseen();
                    if (waypoint.HasValue && waypoint.Equals(_waypoint))
                    {
                        MoveToNearestUnseen(new HashSet<Vector2Int> { waypoint.Value.Destination });
                    }
                    _deadlockTimer = 0;
                }

                if (_waypoint.HasValue)
                {
                    var waypoint = _waypoint.Value;
                    if (_map.GetPath(waypoint.Destination, beOptimistic: true) == null)
                    {
                        MoveToNearestUnseen(new() { waypoint.Destination });
                        waypoint = _waypoint.Value;
                    }
                    _controller.PathAndMoveTo(waypoint.Destination);

                    if (IsDestinationReached())
                    {
                        _waypoint = null;
                    }
                    else
                    {
                        if (_logicTicks % 10 == 0)
                        {
                            if (_previousPosition == _position)
                            {
                                _deadlockTimer++;
                            }
                            else
                            {
                                _previousPosition = _position;
                                _deadlockTimer = 0;
                            }
                        }
                        yield return WaitForCondition.WaitForLogicTicks(1);
                        continue;
                    }
                }

                switch (_currentState)
                {
                    case AlgorithmState.Idle:
                        _controller.StartMoving();
                        _currentState = AlgorithmState.ExploreRoom;
                        break;
                    case AlgorithmState.ExploreRoom:
                        if (_controller.GetStatus() == Robot.Tasks.RobotStatus.Idle)
                        {
                            if (!MoveToNearestUnseen())
                            {
                                _currentState = AlgorithmState.Done;
                            }
                        }
                        break;
                    case AlgorithmState.Done:
                        break;
                }
                if (_logicTicks % 10 == 0)
                {
                    if (_previousPosition == _position)
                    {
                        _deadlockTimer++;
                    }
                    else
                    {
                        _deadlockTimer = 0;
                    }
                }
                _previousPosition = _position;
                yield return WaitForCondition.WaitForLogicTicks(1);
            }
        }

        private bool MoveToNearestUnseen(HashSet<Vector2Int>? excludedTiles = null)
        {
            var startCoordinate = _position;
            if (_map.GetTileStatus(startCoordinate) == SlamTileStatus.Solid)
            {
                var nearestOpenTile = _map.GetNearestTileFloodFill(startCoordinate, SlamTileStatus.Open, excludedTiles);
                if (nearestOpenTile.HasValue)
                {
                    startCoordinate = nearestOpenTile.Value;
                }
            }
            var tile = _map.GetNearestTileFloodFill(startCoordinate, SlamTileStatus.Unseen, excludedTiles);
            if (tile.HasValue)
            {
                tile = _map.GetNearestTileFloodFill(tile.Value, SlamTileStatus.Open);
                if (tile.HasValue)
                {
                    _controller.PathAndMoveTo(tile.Value);
                    _waypoint = new Waypoint(tile.Value, Waypoint.WaypointType.Greed);
                    return true;
                }
            }
            return false;
        }


        private bool IsDestinationReached()
        {
            return _waypoint.HasValue && _map.GetTileCenterRelativePosition(_waypoint.Value.Destination).Distance < 0.5f;
        }
    }

    public class HeartbeatMessage
    {
        private readonly SlamMap _map;

        public HeartbeatMessage(SlamMap map)
        {
            _map = map;
        }

        public HeartbeatMessage Combine(HeartbeatMessage heartbeatMessage, int tick)
        {
            List<SlamMap> maps = new() { heartbeatMessage._map, _map };
            SlamMap.Synchronize(maps, tick); //layers of pass by reference, map in controller is updated with the info from message
            return this;
        }

        public HeartbeatMessage Process() //Combine all, then process, but not really anything to process for heartbeat
        {
            return this;
        }
    }
}