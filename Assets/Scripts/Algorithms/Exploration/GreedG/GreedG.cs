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
using Maes.Utilities;
using UnityEngine;

using static Maes.Map.SlamMap;

namespace Maes.Algorithms.Exploration.GreedG
{

    public class GreedGAlgorithm : IExplorationAlgorithm
    {

        static int id = 1;
        private int Id;

        private IRobotController _controller;
        private CoarseGrainedMap _map;
        //        private Dictionary<Vector2Int, SlamTileStatus> _visibleTiles => _controller.SlamMap.GetVisibleTiles();
        private Vector2Int _position => _map.GetCurrentPosition();
        private AlgorithmState _currentState = AlgorithmState.Idle;

        private Waypoint? _waypoint;

        private Waypoint? _waypointNew;

        private Waypoint? changedWaypoint;
        private int _communicationTicks = 0;
        private int _logicTicks = 0;
        private int _ticksSinceHeartbeat = 0;
        private int _deadlockTimer = 0;
        private Vector2Int _previousPosition;
        private Waypoint _previousWaypoint;

        private bool goingToNewNode;

        private GreedyGraph RootNode;
        private GreedyGraph CurrentNode;

        private GreedyGraph NextNode;

        private NeighbourList neighbourList;

        private ExplorationState explorationState;

        private bool firstFlag;

        private int AlgorithmNumber;

        private enum AlgorithmState
        {
            Idle,
            ExploreRoom,
            Done
        }

        private enum ExplorationState
        {
            AssignedToCurrent,
            AssignedToNext,
            Idle
        }

        private struct Waypoint
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

            public override bool Equals(object obj)
            {
                if (obj is Waypoint other)
                {
                    return Destination == other.Destination
                           && Type == other.Type;
                }
                return false;
            }
        }

        private struct Doorway
        {
            public Vector2Int Center;

            public DoorOrientation Orientation;

            public enum DoorOrientation
            {
                Vertical,
                Horizontal,
            }

            public Doorway(Vector2Int center, DoorOrientation orientation)
            {
                Center = center;
                Orientation = orientation;
            }

            public override bool Equals(object obj)
            {
                if (obj is Doorway other)
                {
                    return Center == other.Center;
                }
                return false;
            }
        }

        private class Neighbour
        {
            public Doorway doorway;

            public GreedyGraph node;

            public Neighbour(Doorway door, GreedyGraph n)
            {
                doorway = door;
                node = n;
            }
        }

        public enum NodeState
        {
            Free = 0,
            Assigned = 1,
            Explored = 2,
        }

        private class GreedyGraph
        {

            public int leftX, rightX, downY, upY;
            public NodeState State;

            public int Cost;

            public int Utility;

            public List<Neighbour> Neighbours;



            public GreedyGraph(NodeState state, int utility)
            {
                State = state;
                Neighbours = new List<Neighbour>();

                Cost = 300;

                Utility = utility;

                leftX = Int32.MinValue;
                rightX = Int32.MinValue;
                downY = Int32.MinValue;
                upY = Int32.MinValue;
            }

            public GreedyGraph(GreedyGraph node)
            {
                State = node.State;

                Neighbours = new List<Neighbour>();

                Cost = node.Cost;

                Utility = node.Utility;

                leftX = node.leftX;
                rightX = node.rightX;
                downY = node.downY;
                upY = node.upY;

                for (int i = 0; i < node.Neighbours.Count; ++i)
                {
                    Neighbours.Add(new Neighbour(node.Neighbours[i].doorway, new GreedyGraph(node.Neighbours[i].node)));
                }

            }

            public void copyNode(GreedyGraph node)
            {

                if (node.State > State)
                {
                    State = node.State;
                }

                if (node.Cost != 300)
                {
                    Cost = node.Cost;
                }

                Utility = node.Utility;

                if (node.leftX != Int32.MinValue)
                {
                    leftX = node.leftX;
                }

                if (node.rightX != Int32.MinValue)
                {
                    rightX = node.rightX;
                }

                if (node.downY != Int32.MinValue)
                {
                    downY = node.downY;
                }

                if (node.upY != Int32.MinValue)
                {
                    upY = node.upY;
                }

                for (int i = 0; i < node.Neighbours.Count; ++i)
                {
                    bool flag = true;
                    for (int j = 0; j < Neighbours.Count; ++j)
                    {
                        if (Neighbours[j].doorway.Equals(node.Neighbours[i].doorway))
                        {
                            Neighbours[j].node.copyNode(node.Neighbours[i].node);
                            flag = false;
                        }
                    }

                    if (flag)
                    {
                        Neighbours.Add(new Neighbour(node.Neighbours[i].doorway, new GreedyGraph(node.Neighbours[i].node)));
                    }

                }

            }

            public void addNeighbour(GreedyGraph neighbour, Doorway door)
            {
                Neighbours.Add(new Neighbour(door, neighbour));
            }

            public void addNeighbour(Neighbour neighbour)
            {
                Neighbours.Add(neighbour);
            }

            public void setLeftX(int x)
            {
                leftX = x;
            }

            public void setRightX(int x)
            {
                rightX = x;
            }

            public void setDownY(int y)
            {
                downY = y;
            }

            public void setUpY(int y)
            {
                upY = y;
            }

            public void setState(NodeState state)
            {
                State = state;
            }

            public void setCost()
            {
                Cost = (int)Math.Sqrt((rightX - leftX) * (upY - downY));
            }

            public NodeState getState()
            {
                return State;
            }

            public void resetNeighbour(GreedyGraph newNode, Doorway door)
            {
                for (int i = 0; i < Neighbours.Count; ++i)
                {
                    if (Neighbours[i].doorway.Equals(door))
                    {
                        //Neighbours[i].node.copyNode(newNode);
                        Neighbours[i].node = newNode;
                    }
                }
            }

            public override bool Equals(object obj)
            {
                if (obj is GreedyGraph other)
                {
                    return leftX == other.leftX
                           && rightX == other.rightX
                           && downY == other.downY
                           && upY == other.upY
                           && State == other.State;
                }
                return false;
            }

            public void DebugNode()
            {
                Debug.Log("up: " + upY);
                Debug.Log("down: " + downY);
                Debug.Log("left: " + leftX);
                Debug.Log("right: " + rightX);
                Debug.Log("State: " + State);
            }

        }

        private class NeighbourList
        {

            public List<Neighbour> List;

            public NeighbourList()
            {
                List = new List<Neighbour>();
            }

        }

        public GreedGAlgorithm(int algorithmnumber)
        {

            neighbourList = new NeighbourList();

            CurrentNode = new GreedyGraph(NodeState.Free, 100);

            RootNode = CurrentNode;

            AlgorithmNumber = algorithmnumber;

            goingToNewNode = false;
            explorationState = ExplorationState.Idle;
            firstFlag = true;

            changedWaypoint = new Waypoint(Vector2Int.up, Waypoint.WaypointType.Greed);


            Id = id;
            id++;
        }

        public string GetDebugInfo()
        {
            return $"State: {Enum.GetName(typeof(AlgorithmState), _currentState)}" +
                   $"\nCoarse Map Position: {_map.GetApproximatePosition()}";
        }

        public void SetController(Robot2DController controller)
        {
            _controller = controller;
            _map = _controller.SlamMap.CoarseMap;
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
                if (_ticksSinceHeartbeat == 50)
                {
                    var ownHeartbeat = new HeartbeatMessage(_controller.SlamMap);
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
                    if (_controller.Status != Robot.Tasks.RobotStatus.Idle)
                        _controller.StopCurrentTask();
                    else
                    {
                        var openTile = _map.GetNearestTileFloodFill(_position, SlamTileStatus.Open);

                        if (openTile.HasValue)
                            _controller.MoveTo(openTile.Value);
                        else
                            _controller.Move(1, true);
                    }
                    _waypoint = null;
                    //Debug.Log("HeartbeatMessage");
                    yield return WaitForCondition.WaitForLogicTicks(1);
                    continue;
                    //TODO: full resets
                }



                if (!goingToNewNode)
                {
                    CheckDoorway();
                    SetDoors();
                    beamDoors();
                    SetWalls();
                }



                foreach (var neighbour in CurrentNode.Neighbours)
                {
                    neighbour.doorway.Center.DrawDebugLineFromRobot(_map, Color.yellow);
                    //Debug.Log(door);
                }


                if (_deadlockTimer >= 5)
                {
                    var waypoint = _waypoint;
                    MoveToNearestUnseen() ;
                    if (waypoint.HasValue && waypoint.Equals(_waypoint))
                    {
                        MoveToNearestUnseen(new HashSet<Vector2Int> { waypoint.Value.Destination });
                    }
                    _deadlockTimer = 0;
                }

                if (explorationState == ExplorationState.AssignedToCurrent)
                {
                    checkWaypoint();
                }
                else if (explorationState == ExplorationState.Idle)
                {
                    if (!setNewDestination())
                    {
                        _waypoint = new Waypoint(_position, Waypoint.WaypointType.Greed); ;
                    }
                }

                //Debug.Log(explorationState);


                if (_waypoint.HasValue)
                {
                    var waypoint = _waypoint.Value;
                    if (_map.GetPath(waypoint.Destination, false, false) == null)
                    {
                        MoveToNearestUnseen(new() { waypoint.Destination });
                        waypoint = _waypoint.Value;
                    }
                    _controller.PathAndMoveTo(waypoint.Destination);

                    if (IsDestinationReached())
                    {
                                                if (explorationState != ExplorationState.Idle)
                                                {
                        _waypoint = null;
                                                }

                    }
                    else
                    {
                        if (_logicTicks % 10 == 0)
                        {
                            if (_previousPosition == _position)
                                _deadlockTimer++;
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



                if (_waypoint == null)
                {

                    if (_waypointNew != null)
                    {
                        _waypoint = _waypointNew;
                        _waypointNew = null;
                    }
                    else
                    {
                        if (goingToNewNode)
                        {

                            CurrentNode = NextNode;
                            goingToNewNode = false;
                            explorationState = ExplorationState.AssignedToCurrent;
                        }


                        if (isCurrentNodeDone())
                        {

                            finalDoorCheck();
                            CurrentNode.setCost();
                            CurrentNode.State = NodeState.Explored;
                            explorationState = ExplorationState.Idle;
                            clearDuplicates();
                            setNeighbourUtilities();
                            setNewDestination();
                            _controller.StopCurrentTask();
                        }
                    }


                }

                if (goingToNewNode)
                {

                    //Debug.Log(NextNode.State);

                    if (NextNode.State == NodeState.Explored)
                    {
                        goingToNewNode = false;
                        _waypoint = null;
                        _waypointNew = null;
                    }


                }

                switch (_currentState)
                {
                    case AlgorithmState.Idle:
                        _controller.StartMoving();
                        _currentState = AlgorithmState.ExploreRoom;
                        break;
                    case AlgorithmState.ExploreRoom:
                        if (_controller.Status == Robot.Tasks.RobotStatus.Idle)
                        {
                            if (goingToNewNode)
                            {
                                yield return WaitForCondition.WaitForLogicTicks(1);
                                continue;
                            }
                            if (MoveToNearestUnseen())
                            {
                                yield return WaitForCondition.WaitForLogicTicks(1);
                                continue;
                            }
                            else _currentState = AlgorithmState.Done;
                        }
                        break;
                    case AlgorithmState.Done:
                        break;
                    default:
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
                if (_waypoint.HasValue)
                        _previousWaypoint = _waypoint.Value;
                _previousPosition = _position;
                yield return WaitForCondition.WaitForLogicTicks(1);
            }
        }

        private bool MoveToNearestUnseen(HashSet<Vector2Int> excludedTiles = null)
        {
            var startCoordinate = _position;
            if (_map.GetTileStatus(startCoordinate) == SlamTileStatus.Solid)
            {
                var NearestOpenTile = _map.GetNearestTileFloodFill(startCoordinate, SlamTileStatus.Open, excludedTiles);
                if (NearestOpenTile.HasValue)
                {
                    startCoordinate = NearestOpenTile.Value;
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


        // Changes waypoint if greed goes outside of room
        private void checkWaypoint()
        {

            if (_waypoint == null)
            {
                return;
            }

            if (CurrentNode.State == NodeState.Explored && goingToNewNode)
            {
                return;
            }

            if (isCurrentNodeDone())
            {

                _waypoint = null;
                return;
            }

            if (_waypoint.Value.Destination == changedWaypoint.Value.Destination)
            {
                return;
            }



            var waypoint = _waypoint;
            if ((waypoint.Value.Destination.x >= CurrentNode.rightX)
                || (waypoint.Value.Destination.x <= CurrentNode.leftX)
                || (waypoint.Value.Destination.y >= CurrentNode.upY)
                || (waypoint.Value.Destination.y <= CurrentNode.downY))
            {

                if (CurrentNode.leftX == Int32.MinValue)
                {
                    Vector2Int node = _position;
                    while (_map.GetTileStatus(node) == SlamTileStatus.Open)
                    {
                        node += Vector2Int.left;
                    }

                    if (_map.GetTileStatus(node) == SlamTileStatus.Unseen)
                    {
                        _waypoint = new Waypoint(node, Waypoint.WaypointType.Greed);
                        changedWaypoint = _waypoint;
                    }

                    return;

                }

                if (CurrentNode.rightX == Int32.MinValue)
                {
                    Vector2Int node = _position;
                    while (_map.GetTileStatus(node) == SlamTileStatus.Open)
                    {
                        node += Vector2Int.right;
                    }

                    if (_map.GetTileStatus(node) == SlamTileStatus.Unseen)
                    {
                        _waypoint = new Waypoint(node, Waypoint.WaypointType.Greed);
                        changedWaypoint = _waypoint;
                    }

                    return;
                }

                if (CurrentNode.downY == Int32.MinValue)
                {
                    Vector2Int node = _position;
                    while (_map.GetTileStatus(node) == SlamTileStatus.Open)
                    {
                        node += Vector2Int.down;
                    }

                    if (_map.GetTileStatus(node) == SlamTileStatus.Unseen)
                    {
                        _waypoint = new Waypoint(node, Waypoint.WaypointType.Greed);
                        changedWaypoint = _waypoint;
                    }

                    return;
                }

                if (CurrentNode.upY == Int32.MinValue)
                {
                    Vector2Int node = _position;
                    while (_map.GetTileStatus(node) == SlamTileStatus.Open)
                    {
                        node += Vector2Int.up;
                    }

                    if (_map.GetTileStatus(node) == SlamTileStatus.Unseen)
                    {
                        _waypoint = new Waypoint(node, Waypoint.WaypointType.Greed);
                        changedWaypoint = _waypoint;
                    }

                    return;
                }

                SetUnseenInNode();

            }
        }

        // Check if current node is fully explored
        private bool isCurrentNodeDone()
        {

            if ((CurrentNode.leftX == Int32.MinValue)
                || (CurrentNode.rightX == Int32.MinValue)
                || (CurrentNode.upY == Int32.MinValue)
                || (CurrentNode.downY == Int32.MinValue))
            {
                return false;
            }

            Vector2Int node = _position;
            node.x = CurrentNode.leftX + 1;
            node.y = CurrentNode.upY - 1;

            for (; node.y > CurrentNode.downY; --node.y)
            {
                node.x = CurrentNode.leftX + 1;

                for (; node.x < CurrentNode.rightX; ++node.x)
                {
                    if (_map.GetTileStatus(node) == SlamTileStatus.Unseen)
                    {
                        return false;
                    }
                }
            }
            return true;

        }

        // Set waypint inside of room if room is not fully explored
        private void SetUnseenInNode()
        {

            if ((CurrentNode.leftX == Int32.MinValue)
                || (CurrentNode.rightX == Int32.MinValue)
                || (CurrentNode.upY == Int32.MinValue)
                || (CurrentNode.downY == Int32.MinValue))
            {

                return;
            }

            Vector2Int node = _position;
            node.x = CurrentNode.leftX;
            node.y = CurrentNode.upY;

            for (; node.y >= CurrentNode.downY; --node.y)
            {
                node.x = CurrentNode.leftX;

                for (; node.x <= CurrentNode.rightX; ++node.x)
                {
                    if (_map.GetTileStatus(node) == SlamTileStatus.Unseen)
                    {
                        _waypoint = new Waypoint(node, Waypoint.WaypointType.Greed);
                        return;
                    }
                }
            }
        }

        // Set destination to new node if current is done
        private bool setNewDestination()
        {

            if (!CurrentNode.Neighbours.Any())
            {
                return false;
            }

            switch (AlgorithmNumber)
            {
                case 0:
                    return setNewDestinationSimple();
                case 1:
                    return setNewDestinationUtil();
                case 2:
                    return setNewDestinationEuclideanSimple();
                case 3:
                    return setNewDestinationEuclideanPath();
                case 4:
                    return setNewDestinationManhattanSimple();
                case 5:
                    return setNewDestinationManhattanPath();
                default:
                    return false;
            }
        }

        private bool setNewDestinationSimple()
        {

            List<GreedyGraph> isChecked = new List<GreedyGraph>();

            List<(GreedyGraph, int)> toCheck = new List<(GreedyGraph, int)>();

            GreedyGraph targetNeighbour = CurrentNode;
            Doorway targetDoorway = CurrentNode.Neighbours[0].doorway;
            int maxUtility = Int32.MinValue;
            int minCost = Int32.MaxValue;

            bool flag = false;

            toCheck.Add((CurrentNode, 0));

            int currCost = Int32.MaxValue;
            int currUtility = Int32.MinValue;


            for (int i = 0; i < toCheck.Count; ++i)
            {
                for (int j = 0; j < toCheck[i].Item1.Neighbours.Count; ++j)
                {
                    currCost = Int32.MaxValue;

                    bool checkedFlag = true;

                    for (int k = 0; k < isChecked.Count; ++k)
                    {
                        if (toCheck[i].Item1.Neighbours[j].node.Equals(isChecked[k]))
                        {
                            checkedFlag = false;
                            break;
                        }
                    }

                    if (checkedFlag)
                    {

                        if (i == 0)
                        {
                            currCost = (int)Vector2.Distance(_position, toCheck[i].Item1.Neighbours[j].doorway.Center);
                        }
                        else
                        {
                            currCost = toCheck[i].Item1.Cost + toCheck[i].Item2;
                        }

                        if (toCheck[i].Item1.Neighbours[j].node.State == NodeState.Free)
                        {
                            flag = true;

                            if (currCost < minCost)
                            {
                                targetNeighbour = toCheck[i].Item1.Neighbours[j].node;
                                targetDoorway = toCheck[i].Item1.Neighbours[j].doorway;
                                minCost = currCost;

                            }

                        }
                        toCheck.Add((toCheck[i].Item1.Neighbours[j].node, currCost));
                    }

                }

                isChecked.Add(toCheck[i].Item1);
            }

            if (flag)
            {
                if (setDestinationWaypoint(targetNeighbour, targetDoorway))
                {
                    targetNeighbour.State = NodeState.Assigned;
                    explorationState = ExplorationState.AssignedToNext;
                    goingToNewNode = true;
                    return true;
                }
            }

            return false;
        }

        private bool setNewDestinationUtil()
        {

            List<GreedyGraph> isChecked = new List<GreedyGraph>();

            List<(GreedyGraph, int)> toCheck = new List<(GreedyGraph, int)>();

            GreedyGraph targetNeighbour = CurrentNode;
            Doorway targetDoorway = CurrentNode.Neighbours[0].doorway;
            int maxUtility = Int32.MinValue;
            int minCost = Int32.MaxValue;

            bool flag = false;

            toCheck.Add((CurrentNode, 0));

            int currCost = Int32.MaxValue;
            int currUtility = Int32.MinValue;


            for (int i = 0; i < toCheck.Count; ++i)
            {
                for (int j = 0; j < toCheck[i].Item1.Neighbours.Count; ++j)
                {
                    currCost = Int32.MaxValue;

                    bool checkedFlag = true;

                    for (int k = 0; k < isChecked.Count; ++k)
                    {
                        if (toCheck[i].Item1.Neighbours[j].node.Equals(isChecked[k]))
                        {
                            checkedFlag = false;
                            break;
                        }
                    }

                    if (checkedFlag)
                    {

                        if (i == 0)
                        {
                            currCost = (int)Vector2.Distance(_position, toCheck[i].Item1.Neighbours[j].doorway.Center);
                        }
                        else
                        {
                            currCost = toCheck[i].Item1.Cost + toCheck[i].Item2;
                        }

                        if (toCheck[i].Item1.Neighbours[j].node.State == NodeState.Free)
                        {
                            flag = true;

                            currUtility = toCheck[i].Item1.Neighbours[j].node.Utility - currCost;

                            if (currUtility > maxUtility)
                            {
                                targetNeighbour = toCheck[i].Item1.Neighbours[j].node;
                                targetDoorway = toCheck[i].Item1.Neighbours[j].doorway;
                                maxUtility = currUtility;
                            }

                        }
                        toCheck.Add((toCheck[i].Item1.Neighbours[j].node, currCost));
                    }/**/


                }

                isChecked.Add(toCheck[i].Item1);
            }

            if (flag)
            {
                if (setDestinationWaypoint(targetNeighbour, targetDoorway))
                {
                    targetNeighbour.State = NodeState.Assigned;
                    explorationState = ExplorationState.AssignedToNext;
                    goingToNewNode = true;
                    return true;
                }
            }

            return false;
        }

        private bool setNewDestinationEuclideanSimple()
        {

            List<GreedyGraph> isChecked = new List<GreedyGraph>();

            List<(GreedyGraph, int)> toCheck = new List<(GreedyGraph, int)>();

            GreedyGraph targetNeighbour = CurrentNode;
            Doorway targetDoorway = CurrentNode.Neighbours[0].doorway;
            int maxUtility = Int32.MinValue;
            int minCost = Int32.MaxValue;

            bool flag = false;

            toCheck.Add((CurrentNode, 0));

            int currCost = Int32.MaxValue;
            int currUtility = Int32.MinValue;


            for (int i = 0; i < toCheck.Count; ++i)
            {
                for (int j = 0; j < toCheck[i].Item1.Neighbours.Count; ++j)
                {
                    currCost = Int32.MaxValue;

                    bool checkedFlag = true;

                    for (int k = 0; k < isChecked.Count; ++k)
                    {
                        if (toCheck[i].Item1.Neighbours[j].node.Equals(isChecked[k]))
                        {
                            checkedFlag = false;
                            break;
                        }
                    }

                    if (checkedFlag)
                    {

                        currCost = (int)Vector2.Distance(_position, toCheck[i].Item1.Neighbours[j].doorway.Center);

                        if (toCheck[i].Item1.Neighbours[j].node.State == NodeState.Free)
                        {
                            flag = true;

                            if (currCost < minCost)
                            {
                                targetNeighbour = toCheck[i].Item1.Neighbours[j].node;
                                targetDoorway = toCheck[i].Item1.Neighbours[j].doorway;
                                minCost = currCost;

                            }

                        }
                        toCheck.Add((toCheck[i].Item1.Neighbours[j].node, currCost));
                    }


                }

                isChecked.Add(toCheck[i].Item1);
            }

            if (flag)
            {
                if (setDestinationWaypoint(targetNeighbour, targetDoorway))
                {
                    targetNeighbour.State = NodeState.Assigned;
                    explorationState = ExplorationState.AssignedToNext;
                    goingToNewNode = true;
                    return true;
                }
            }

            return false;
        }

        private bool setNewDestinationEuclideanPath()
        {

            List<GreedyGraph> isChecked = new List<GreedyGraph>();

            List<(GreedyGraph, int, Vector2Int)> toCheck = new List<(GreedyGraph, int, Vector2Int doorway)>();

            GreedyGraph targetNeighbour = CurrentNode;
            Doorway targetDoorway = CurrentNode.Neighbours[0].doorway;
            int maxUtility = Int32.MinValue;
            int minCost = Int32.MaxValue;

            bool flag = false;

            toCheck.Add((CurrentNode, 0, _position));

            int currCost = Int32.MaxValue;
            int currUtility = Int32.MinValue;


            for (int i = 0; i < toCheck.Count; ++i)
            {
                for (int j = 0; j < toCheck[i].Item1.Neighbours.Count; ++j)
                {
                    currCost = Int32.MaxValue;

                    bool checkedFlag = true;

                    for (int k = 0; k < isChecked.Count; ++k)
                    {
                        if (toCheck[i].Item1.Neighbours[j].node.Equals(isChecked[k]))
                        {
                            checkedFlag = false;
                            break;
                        }
                    }

                    if (checkedFlag)
                    {

                        currCost = toCheck[i].Item2 + (int)Vector2.Distance(toCheck[i].Item3, toCheck[i].Item1.Neighbours[j].doorway.Center);

                        if (toCheck[i].Item1.Neighbours[j].node.State == NodeState.Free)
                        {
                            flag = true;

                            if (currCost < minCost)
                            {
                                targetNeighbour = toCheck[i].Item1.Neighbours[j].node;
                                targetDoorway = toCheck[i].Item1.Neighbours[j].doorway;
                                minCost = currCost;

                            }

                        }
                        toCheck.Add((toCheck[i].Item1.Neighbours[j].node, currCost, toCheck[i].Item1.Neighbours[j].doorway.Center));
                    }


                }

                isChecked.Add(toCheck[i].Item1);
            }

            if (flag)
            {
                if (setDestinationWaypoint(targetNeighbour, targetDoorway))
                {
                    targetNeighbour.State = NodeState.Assigned;
                    explorationState = ExplorationState.AssignedToNext;
                    goingToNewNode = true;
                    return true;
                }
            }

            return false;
        }

        private bool setNewDestinationManhattanSimple()
        {

            List<GreedyGraph> isChecked = new List<GreedyGraph>();

            List<(GreedyGraph, int)> toCheck = new List<(GreedyGraph, int)>();

            GreedyGraph targetNeighbour = CurrentNode;
            Doorway targetDoorway = CurrentNode.Neighbours[0].doorway;
            int maxUtility = Int32.MinValue;
            int minCost = Int32.MaxValue;

            bool flag = false;

            toCheck.Add((CurrentNode, 0));

            int currCost = Int32.MaxValue;
            int currUtility = Int32.MinValue;


            for (int i = 0; i < toCheck.Count; ++i)
            {
                for (int j = 0; j < toCheck[i].Item1.Neighbours.Count; ++j)
                {
                    currCost = Int32.MaxValue;

                    bool checkedFlag = true;

                    for (int k = 0; k < isChecked.Count; ++k)
                    {
                        if (toCheck[i].Item1.Neighbours[j].node.Equals(isChecked[k]))
                        {
                            checkedFlag = false;
                            break;
                        }
                    }

                    if (checkedFlag)
                    {

                        currCost = (int)ManhattanCalc(_position.x, toCheck[i].Item1.Neighbours[j].doorway.Center.x, _position.y, toCheck[i].Item1.Neighbours[j].doorway.Center.y);

                        if (toCheck[i].Item1.Neighbours[j].node.State == NodeState.Free)
                        {
                            flag = true;

                            if (currCost < minCost)
                            {
                                targetNeighbour = toCheck[i].Item1.Neighbours[j].node;
                                targetDoorway = toCheck[i].Item1.Neighbours[j].doorway;
                                minCost = currCost;

                            }

                        }
                        toCheck.Add((toCheck[i].Item1.Neighbours[j].node, currCost));
                    }


                }

                isChecked.Add(toCheck[i].Item1);
            }

            if (flag)
            {
                if (setDestinationWaypoint(targetNeighbour, targetDoorway))
                {
                    targetNeighbour.State = NodeState.Assigned;
                    explorationState = ExplorationState.AssignedToNext;
                    goingToNewNode = true;
                    return true;
                }
            }

            return false;
        }
        private bool setNewDestinationManhattanPath()
        {

            List<GreedyGraph> isChecked = new List<GreedyGraph>();

            List<(GreedyGraph, int, Vector2Int)> toCheck = new List<(GreedyGraph, int, Vector2Int doorway)>();

            GreedyGraph targetNeighbour = CurrentNode;
            Doorway targetDoorway = CurrentNode.Neighbours[0].doorway;
            int maxUtility = Int32.MinValue;
            int minCost = Int32.MaxValue;

            bool flag = false;

            toCheck.Add((CurrentNode, 0, _position));

            int currCost = Int32.MaxValue;
            int currUtility = Int32.MinValue;


            for (int i = 0; i < toCheck.Count; ++i)
            {
                for (int j = 0; j < toCheck[i].Item1.Neighbours.Count; ++j)
                {
                    currCost = Int32.MaxValue;

                    bool checkedFlag = true;

                    for (int k = 0; k < isChecked.Count; ++k)
                    {
                        if (toCheck[i].Item1.Neighbours[j].node.Equals(isChecked[k]))
                        {
                            checkedFlag = false;
                            break;
                        }
                    }

                    if (checkedFlag)
                    {

                        currCost = toCheck[i].Item2 + (int)ManhattanCalc(toCheck[i].Item3.x, toCheck[i].Item1.Neighbours[j].doorway.Center.x, toCheck[i].Item3.y, toCheck[i].Item1.Neighbours[j].doorway.Center.y);

                        if (toCheck[i].Item1.Neighbours[j].node.State == NodeState.Free)
                        {
                            flag = true;

                            if (currCost < minCost)
                            {
                                targetNeighbour = toCheck[i].Item1.Neighbours[j].node;
                                targetDoorway = toCheck[i].Item1.Neighbours[j].doorway;
                                minCost = currCost;

                            }

                        }
                        toCheck.Add((toCheck[i].Item1.Neighbours[j].node, currCost, toCheck[i].Item1.Neighbours[j].doorway.Center));
                    }


                }

                isChecked.Add(toCheck[i].Item1);
            }

            if (flag)
            {
                if (setDestinationWaypoint(targetNeighbour, targetDoorway))
                {
                    targetNeighbour.State = NodeState.Assigned;
                    explorationState = ExplorationState.AssignedToNext;
                    goingToNewNode = true;
                    return true;
                }
            }

            return false;
        }

        private int ManhattanCalc(int x1, int x2, int y1, int y2)
        {
            return Math.Abs(x1 - x2) + Math.Abs(y1 - y2);
        }


        // Set the exact point to enter
        private bool setDestinationWaypoint(GreedyGraph neighbour, Doorway door)
        {

            bool flag = false;

            if (door.Orientation == Doorway.DoorOrientation.Horizontal)
            {

                if (neighbour.upY != Int32.MinValue)
                {
                    _waypointNew = new Waypoint(door.Center + Vector2Int.down + Vector2Int.down, Waypoint.WaypointType.Greed);
                    _waypoint = new Waypoint(door.Center + Vector2Int.up + Vector2Int.up, Waypoint.WaypointType.Greed);
                    flag = true;
                }
                else
                {
                    _waypointNew = new Waypoint(door.Center + Vector2Int.up + Vector2Int.up, Waypoint.WaypointType.Greed);
                    _waypoint = new Waypoint(door.Center + Vector2Int.down + Vector2Int.down, Waypoint.WaypointType.Greed);
                    flag = true;
                }

            }
            else
            {
                if (neighbour.rightX != Int32.MinValue)
                {
                    _waypointNew = new Waypoint(door.Center + Vector2Int.left + Vector2Int.left, Waypoint.WaypointType.Greed);
                    _waypoint = new Waypoint(door.Center + Vector2Int.right + Vector2Int.right, Waypoint.WaypointType.Greed);
                    flag = true;
                }
                else
                {
                    _waypointNew = new Waypoint(door.Center + Vector2Int.right + Vector2Int.right, Waypoint.WaypointType.Greed);
                    _waypoint = new Waypoint(door.Center + Vector2Int.left + Vector2Int.left, Waypoint.WaypointType.Greed);
                    flag = true;
                }
            }

            if (flag)
            {
                NextNode = neighbour;
                return true;
            }

            return false;

        }

        private void clearDuplicates()
        {
            for (int i = 0; i < CurrentNode.Neighbours.Count; ++i)
            {
                CurrentNode.Neighbours[i].node.resetNeighbour(CurrentNode, CurrentNode.Neighbours[i].doorway);
            }
        }

        private void CheckDoorway()
        {
            // Test
            var wallTile = _map.GetNearestTileFloodFill(_position, SlamTileStatus.Solid);

            if (wallTile == null)
            {
                return;
            }

            if ((_map.GetTileStatus((Vector2Int)(wallTile + Vector2Int.left)) == SlamTileStatus.Open) || (_map.GetTileStatus((Vector2Int)(wallTile + Vector2Int.right)) == SlamTileStatus.Open))
            {
                if ((_map.GetTileStatus((Vector2Int)(wallTile + Vector2Int.up)) == SlamTileStatus.Open) || (_map.GetTileStatus((Vector2Int)(wallTile + Vector2Int.down)) == SlamTileStatus.Open))
                {


                    if (_map.GetTileStatus(wallTile.Value + Vector2Int.right) == SlamTileStatus.Open)
                    {
                        AddDoorHorizontal(wallTile.Value + Vector2Int.right);
                    }


                    if (_map.GetTileStatus((wallTile.Value + Vector2Int.left)) == SlamTileStatus.Open)
                    {
                        AddDoorHorizontal(wallTile.Value + Vector2Int.left);
                    }


                    if (_map.GetTileStatus((wallTile.Value + Vector2Int.up)) == SlamTileStatus.Open)
                    {
                        AddDoorVertical(wallTile.Value + Vector2Int.up);
                    }

                    if (_map.GetTileStatus((wallTile.Value + Vector2Int.down)) == SlamTileStatus.Open)
                    {
                        AddDoorVertical(wallTile.Value + Vector2Int.down);
                    }

                }
            }

        }

        private void SetDoors()
        {

            Vector2Int pos = _position;

            if (CurrentNode.leftX != Int32.MinValue)
            {

                pos.y = _position.y;
                pos.x = CurrentNode.leftX;
                if (_map.GetTileStatus(pos) == SlamTileStatus.Open)
                {
                    AddDoorVertical(pos);
                }
            }

            if (CurrentNode.rightX != Int32.MinValue)
            {
                pos.y = _position.y;
                pos.x = CurrentNode.rightX;
                if (_map.GetTileStatus(pos) == SlamTileStatus.Open)
                {
                    AddDoorVertical(pos);
                }
            }

            if (CurrentNode.downY != Int32.MinValue)
            {
                pos.x = _position.x;
                pos.y = CurrentNode.downY;
                if (_map.GetTileStatus(pos) == SlamTileStatus.Open)
                {
                    AddDoorHorizontal(pos);
                }
            }

            if (CurrentNode.upY != Int32.MinValue)
            {
                pos.x = _position.x;
                pos.y = CurrentNode.upY;
                if (_map.GetTileStatus(pos) == SlamTileStatus.Open)
                {
                    AddDoorHorizontal(pos);
                }
            }

        }

        private void beamDoors()
        {
            if (_map.GetTileStatus(_position) == SlamTileStatus.Solid)
            {
                return;
            }

            Vector2Int direction = Vector2Int.left;
            Vector2Int node = _position + direction;

            while (_map.GetTileStatus(node) == SlamTileStatus.Open)
            {
                node += direction;
                AddDoorVertical(node);
            }

            direction = Vector2Int.right;
            node = _position + direction;

            while (_map.GetTileStatus(node) == SlamTileStatus.Open)
            {
                node += direction;
                AddDoorVertical(node);
            }

            direction = Vector2Int.up;
            node = _position + direction;

            while (_map.GetTileStatus(node) == SlamTileStatus.Open)
            {
                node += direction;
                AddDoorHorizontal(node);
            }

            direction = Vector2Int.down;
            node = _position + direction;

            while (_map.GetTileStatus(node) == SlamTileStatus.Open)
            {
                node += direction;
                AddDoorHorizontal(node);
            }
        }

        private void finalDoorCheck()
        {


            Vector2Int node1 = _position;
            Vector2Int node2 = _position;

            node1.x = CurrentNode.leftX;
            node1.y = CurrentNode.upY;

            node2.x = CurrentNode.rightX;
            node2.y = CurrentNode.upY;

            for (; node1.y > CurrentNode.downY; --node1.y, --node2.y)
            {

                if (_map.GetTileStatus(node1) == SlamTileStatus.Open)
                {
                    AddDoorVertical(node1);
                }
                if (_map.GetTileStatus(node2) == SlamTileStatus.Open)
                {
                    AddDoorVertical(node2);
                }

            }

            node1.x = CurrentNode.rightX;
            node1.y = CurrentNode.upY;

            node2.x = CurrentNode.rightX;
            node2.y = CurrentNode.downY;

            for (; node1.x > CurrentNode.leftX; --node1.x, --node2.x)
            {

                if (_map.GetTileStatus(node1) == SlamTileStatus.Open)
                {
                    AddDoorHorizontal(node1);
                }
                if (_map.GetTileStatus(node2) == SlamTileStatus.Open)
                {
                    AddDoorHorizontal(node2);
                }

            }
        }


        // Add Horizontal door to current node
        private void AddDoorHorizontal(Vector2Int pos)
        {

            if (_map.GetTileStatus(pos) == SlamTileStatus.Solid)
            {
                return;
            }

            Vector2Int left = pos;
            Vector2Int right = pos;

            while (_map.GetTileStatus(left) == SlamTileStatus.Open)
            {
                left += Vector2Int.left;
            }

            if (_map.GetTileStatus(left) != SlamTileStatus.Solid)
            {
                return;
            }

            while (_map.GetTileStatus(right) == SlamTileStatus.Open)
            {
                right += Vector2Int.right;
            }

            if (_map.GetTileStatus(right) != SlamTileStatus.Solid)
            {
                return;
            }

            if (right.x - left.x > 4)
            {
                return;
            }

            if ((_map.GetTileStatus(right + Vector2Int.up) != SlamTileStatus.Open) && (_map.GetTileStatus(right + Vector2Int.down) != SlamTileStatus.Open))
            {
                return;
            }

            if ((_map.GetTileStatus(left + Vector2Int.up) != SlamTileStatus.Open) && (_map.GetTileStatus(left + Vector2Int.down) != SlamTileStatus.Open))
            {
                return;
            }

            Vector2Int newDoor = (left + right) / 2;
            bool flag = true;
            foreach (var neighbour in CurrentNode.Neighbours)
            {
                if (Vector2.Distance(newDoor, neighbour.doorway.Center) < 3)
                {
                    flag = false;
                }
                if (CurrentNode.downY > Int32.MinValue)
                {
                    if (CurrentNode.downY > newDoor.y)
                    {
                        flag = false;
                    }
                }
                if (CurrentNode.upY > Int32.MinValue)
                {
                    if (CurrentNode.upY < newDoor.y)
                    {
                        flag = false;
                    }
                }
                if (CurrentNode.rightX > Int32.MinValue)
                {
                    if (CurrentNode.rightX < newDoor.x)
                    {
                        flag = false;
                    }
                }
                if (CurrentNode.leftX > Int32.MinValue)
                {
                    if (CurrentNode.leftX > newDoor.x)
                    {
                        flag = false;
                    }
                }

            }

            if (flag)
            {
                foreach (var neighbour in neighbourList.List)
                {
                    if (Vector2.Distance(newDoor, neighbour.doorway.Center) < 3)
                    {
                        CurrentNode.addNeighbour(neighbour);
                        neighbour.node.resetNeighbour(CurrentNode, neighbour.doorway);
                        flag = false;
                        break;
                    }
                }
            }



            if (flag)
            {
                GreedyGraph neighbour = new GreedyGraph(NodeState.Free, CurrentNode.Utility / 2);
                Doorway newDoorway = new Doorway(newDoor, Doorway.DoorOrientation.Horizontal);

                if (_position.y < newDoor.y)
                {
                    neighbour.setDownY(newDoor.y);
                    CurrentNode.upY = newDoor.y;
                }
                else
                {
                    neighbour.setUpY(newDoor.y);
                    CurrentNode.downY = newDoor.y;
                }

                neighbourList.List.Add(new Neighbour(newDoorway, CurrentNode));
                neighbour.addNeighbour(CurrentNode, newDoorway);
                CurrentNode.addNeighbour(neighbour, newDoorway);
            }

        }


        // Add Vertical door to current node
        private void AddDoorVertical(Vector2Int pos)
        {

            if (_map.GetTileStatus(pos) == SlamTileStatus.Solid)
            {
                return;
            }

            Vector2Int down = pos;
            Vector2Int up = pos;

            while (_map.GetTileStatus(down) == SlamTileStatus.Open)
            {
                down += Vector2Int.down;
            }

            if (_map.GetTileStatus(down) != SlamTileStatus.Solid)
            {
                return;
            }

            while (_map.GetTileStatus(up) == SlamTileStatus.Open)
            {
                up += Vector2Int.up;
            }

            if (_map.GetTileStatus(up) != SlamTileStatus.Solid)
            {
                return;
            }

            if (up.y - down.y > 4)
            {
                return;
            }

            if ((_map.GetTileStatus(up + Vector2Int.left) != SlamTileStatus.Open) && (_map.GetTileStatus(up + Vector2Int.right) != SlamTileStatus.Open))
            {
                return;
            }

            if ((_map.GetTileStatus(down + Vector2Int.left) != SlamTileStatus.Open) && (_map.GetTileStatus(down + Vector2Int.right) != SlamTileStatus.Open))
            {
                return;
            }

            Vector2Int newDoor = (down + up) / 2;
            bool flag = true;


            foreach (var neighbour in CurrentNode.Neighbours)
            {
                if (Vector2.Distance(newDoor, neighbour.doorway.Center) < 3)
                {
                    flag = false;
                }
                if (CurrentNode.downY > Int32.MinValue)
                {
                    if (CurrentNode.downY > newDoor.y)
                    {
                        flag = false;
                    }
                }
                if (CurrentNode.upY > Int32.MinValue)
                {
                    if (CurrentNode.upY < newDoor.y)
                    {
                        flag = false;
                    }
                }
                if (CurrentNode.rightX > Int32.MinValue)
                {
                    if (CurrentNode.rightX < newDoor.x)
                    {
                        flag = false;
                    }
                }
                if (CurrentNode.leftX > Int32.MinValue)
                {
                    if (CurrentNode.leftX > newDoor.x)
                    {
                        flag = false;
                    }
                }
            }

            if (flag)
            {
                foreach (var neighbour in neighbourList.List)
                {
                    if (Vector2.Distance(newDoor, neighbour.doorway.Center) < 3)
                    {
                        CurrentNode.addNeighbour(neighbour);
                        neighbour.node.resetNeighbour(CurrentNode, neighbour.doorway);
                        flag = false;
                    }
                }
            }



            if (flag)
            {
                GreedyGraph neighbour = new GreedyGraph(NodeState.Free, CurrentNode.Utility / 2);
                Doorway newDoorway = new Doorway(newDoor, Doorway.DoorOrientation.Vertical);

                if (_position.x < newDoor.x)
                {
                    neighbour.setLeftX(newDoor.x);
                    CurrentNode.rightX = newDoor.x;
                }
                else
                {
                    neighbour.setRightX(newDoor.x);
                    CurrentNode.leftX = newDoor.x;
                }

                neighbourList.List.Add(new Neighbour(newDoorway, CurrentNode));
                neighbour.addNeighbour(CurrentNode, newDoorway);
                CurrentNode.addNeighbour(neighbour, newDoorway);
            }

        }

        private void SetWalls()
        {
            int wall = Int32.MinValue;

            if (CurrentNode.leftX == Int32.MinValue)
            {
                wall = getWall(Vector2Int.left);
                if (wall > Int32.MinValue)
                {
                    CurrentNode.setLeftX(wall);
                }
            }

            if (CurrentNode.rightX == Int32.MinValue)
            {
                wall = getWall(Vector2Int.right);
                if (wall > Int32.MinValue)
                {
                    CurrentNode.setRightX(wall);
                }
            }

            if (CurrentNode.downY == Int32.MinValue)
            {

                wall = getWall(Vector2Int.down);
                if (wall > Int32.MinValue)
                {
                    CurrentNode.setDownY(wall);
                }
            }

            if (CurrentNode.upY == Int32.MinValue)
            {
                wall = getWall(Vector2Int.up);
                if (wall > Int32.MinValue)
                {
                    CurrentNode.setUpY(wall);
                }
            }
        }

        private int getWall(Vector2Int direction)
        {
            if (_map.GetTileStatus(_position) == SlamTileStatus.Solid)
            {
                return Int32.MinValue;
            }

            Vector2Int node = _position + direction;
            while (_map.GetTileStatus(node) == SlamTileStatus.Open)
            {
                node += direction;
            }

            if (_map.GetTileStatus(node) == SlamTileStatus.Solid)
            {
                node = node * direction;
                int value = node.x + node.y;
                return Math.Abs(value);
            }

            return Int32.MinValue;
        }

        private void checkWall(Doorway doorway)
        {

            if (CurrentNode.leftX == Int32.MinValue)
            {
                if ((doorway.Orientation == Doorway.DoorOrientation.Vertical) && (_position.x > doorway.Center.x))
                {
                    CurrentNode.leftX = doorway.Center.x;
                }
            }

            if (CurrentNode.rightX == Int32.MinValue)
            {
                if ((doorway.Orientation == Doorway.DoorOrientation.Vertical) && (_position.x < doorway.Center.x))
                {
                    CurrentNode.rightX = doorway.Center.x;
                }
            }

            if (CurrentNode.downY == Int32.MinValue)
            {
                if ((doorway.Orientation == Doorway.DoorOrientation.Horizontal) && (_position.y > doorway.Center.y))
                {
                    CurrentNode.downY = doorway.Center.y;
                }
            }

            if (CurrentNode.upY == Int32.MinValue)
            {
                if ((doorway.Orientation == Doorway.DoorOrientation.Horizontal) && (_position.y < doorway.Center.y))
                {
                    CurrentNode.upY = doorway.Center.y;
                }
            }
        }

        private void setNeighbourUtilities()
        {
            int unexplored = 0;

            foreach (var neighbour in CurrentNode.Neighbours)
            {
                if (neighbour.node.State != NodeState.Explored)
                {
                    unexplored++;
                }
            }

            if (unexplored == 0)
            {
                return;
            }

            int minUtility = CurrentNode.Utility / CurrentNode.Neighbours.Count;
            int freeUtility = minUtility * unexplored;

            foreach (var neighbour in CurrentNode.Neighbours)
            {
                if (neighbour.node.State != NodeState.Explored)
                {
                    neighbour.node.Utility = freeUtility;
                }
            }
        }
    }

    public class HeartbeatMessage
    {
        private readonly SlamMap _map;

        public HeartbeatMessage(SlamMap map)
        {
            _map = map;
        }

        public HeartbeatMessage Combine(HeartbeatMessage otherMessage, int tick)
        {
            if (otherMessage is HeartbeatMessage heartbeatMessage)
            {
                List<SlamMap> maps = new() { heartbeatMessage._map, _map };
                SlamMap.Synchronize(maps, tick); //layers of pass by reference, map in controller is updated with the info from message
                return this;
            }
            return null;
        }

        public HeartbeatMessage Process() //Combine all, then process, but not really anything to process for heartbeat
        {
            return this;
        }
    }
}
