// Copyright 2025 MAEPS
// 
// This file is part of MAEPS
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
// Contributors: 
// Rasmus Borrisholt Schmidt, 
// Andreas Sebastian Sørensen, 
// Thor Beregaard, 
// Malte Z. Andreasen, 
// Philip I. Holler,
// Magnus K. Jensen, 
//
// In 2025:
// Casper Nyvang Sørensen,
// Christian Ziegler Sejersen,
// Jakob Meyer Olsen,
// 
// Original repository: https://github.com/Molitany/MAES

using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using JetBrains.Annotations;

using Maes.Map;
using Maes.Map.Generators;
using Maes.Statistics.Trackers;
using Maes.UI;
using Maes.Utilities;

using UnityEngine;

using Vector2 = UnityEngine.Vector2;

namespace Maes.Robot
{
    public readonly struct SensedObject<T>
    {
        public readonly float Distance;
        public readonly float Angle;
        public readonly T RobotId;

        public SensedObject(float distance, float angle, T robotId)
        {
            Distance = distance;
            Angle = angle;
            RobotId = robotId;
        }

        public Vector2 GetRelativePosition(Vector2 myPosition, float globalAngle)
        {
            var angle = Mathf.Deg2Rad * ((Angle - globalAngle) % 360);
            var x = myPosition.x + (Distance * Mathf.Cos(angle));
            var y = myPosition.y + (Distance * Mathf.Sin(angle));
            return new Vector2(x, y);
        }
    }

    // Messages sent through this class will be subject to communication range and line of sight.
    // Communication is non-instantaneous. Messages will be received by other robots after one logic tick. 
    public sealed class CommunicationManager : ISimulationUnit
    {
        private readonly RobotConstraints _robotConstraints;
        private readonly DebuggingVisualizer _visualizer;

        // Messages that will be sent during the next logic update
        private readonly List<Message> _queuedMessages = new();

        // Messages that were sent last tick and can now be read 
        private readonly List<Message> _readableMessages = new();

        private RayTracingMap<Tile>? _rayTracingMap;

        // It is only used for DetectWall, so compute it lazily.
        private RayTracingMap<Tile> RayTracingMap
        {
            get
            {
                if (_rayTracingMap == null)
                {
                    _rayTracingMap = new RayTracingMap<Tile>(_tileMap);
                }

                return _rayTracingMap;
            }
        }

        // Set by SetRobotReferences
        private IReadOnlyList<MonaRobot> _robots = null!;


        private EnvironmentTaggingMap? _environmentTaggingMap;

        // Map for storing and retrieving all tags deposited by robots
        // It is only used for environment tag based algorithms, so compute it lazily.
        private EnvironmentTaggingMap EnvironmentTaggingMap
        {
            get
            {
                if (_environmentTaggingMap == null)
                {
                    _environmentTaggingMap = new EnvironmentTaggingMap(_tileMap);
                }

                return _environmentTaggingMap;
            }
        }

        private readonly SimulationMap<Tile> _tileMap;

        private int _localTickCounter;

        private bool _adjacencyMatrixComputed = false;
        private readonly Dictionary<(int, int), CommunicationInfo> _adjacencyMatrix = new();

        private bool _communicationGroupsComputed = false;
        private readonly HashSet<HashSet<int>> _communicationGroups = new();

        private float _robotRelativeSize;
        private readonly Vector2 _offset;

        private int _receivedMessagesThisTick;
        private int _receivedMessagesLastTick;

        private int _sentMessagesThisTick;
        private int _sentMessagesLastTick;

        private readonly HashSet<MonaRobot> _robotsCalledReadMessagesThisTick = new();

        public readonly CommunicationTracker CommunicationTracker;
        private readonly Dictionary<TileType, float> _attenuationDict;

        private readonly struct Message
        {
            public readonly object Contents;
            public readonly MonaRobot Sender;
            public readonly Vector2 BroadcastCenter;

            public Message(object contents, MonaRobot sender, Vector2 broadcastCenter)
            {
                Contents = contents;
                Sender = sender;
                BroadcastCenter = broadcastCenter;
            }
        }

        public readonly struct CommunicationInfo
        {
            public readonly float Distance;
            public readonly float Angle;
            public readonly float WallCellsDistance;
            public readonly float RegularCellsDistance;
            public readonly bool TransmissionSuccessful;
            public readonly float SignalStrength;

            public CommunicationInfo(float distance, float angle, float wallCellDistance, float regularCellDistance, bool transmissionSuccess, float signalStrength)
            {
                Distance = distance;
                Angle = angle;
                WallCellsDistance = wallCellDistance;
                RegularCellsDistance = regularCellDistance;
                TransmissionSuccessful = transmissionSuccess;
                SignalStrength = signalStrength;
            }

        }

        public CommunicationManager(SimulationMap<Tile> collisionMap, RobotConstraints robotConstraints,
            DebuggingVisualizer visualizer)
        {
            _robotConstraints = robotConstraints;
            _visualizer = visualizer;
            _tileMap = collisionMap;
            CommunicationTracker = new CommunicationTracker();
            _offset = collisionMap.ScaledOffset;
            _attenuationDict = _robotConstraints.AttenuationDictionary[_robotConstraints.Frequency];
        }

        public void SetRobotRelativeSize(float robotRelativeSize)
        {
            _robotRelativeSize = robotRelativeSize;
        }

        // Adds a message to the broadcast queue
        [ForbiddenKnowledge]
        public void BroadcastMessage(MonaRobot sender, in object messageContents)
        {
            _queuedMessages.Add(new Message(messageContents, sender, sender.transform.position));
            _sentMessagesThisTick++;
        }

        // Returns a list of messages sent by other robots
        public List<object> ReadMessages(MonaRobot receiver)
        {
            PopulateAdjacencyMatrix();
            var messages = new List<object>();
            foreach (var message in _readableMessages)
            {
                // The robot will not receive its own messages
                if (message.Sender.id == receiver.id)
                {
                    continue;
                }

                if (!_adjacencyMatrix!.TryGetValue((message.Sender.id, receiver.id), out var communicationTrace))
                {
                    continue;
                }

                // If the transmission probability is above the specified threshold then the message will be sent
                // otherwise it is discarded
                if (communicationTrace.TransmissionSuccessful)
                {
                    messages.Add(message.Contents);
                    if (GlobalSettings.DrawCommunication)
                    {
                        _visualizer.AddCommunicationTrail(message.Sender, receiver);
                    }
                }
            }

            if (_robotsCalledReadMessagesThisTick.Add(receiver))
            {
                _receivedMessagesThisTick += messages.Count;
            }

            return messages;
        }

        private CommunicationInfo CreateCommunicationInfo(float angle, float wallCellsDistance, float regularCellsDistance, float signalStrength)
        {
            var totalDistance = regularCellsDistance + wallCellsDistance;
            var transmissionSuccessful = _robotConstraints
                .IsTransmissionSuccessful(totalDistance, wallCellsDistance);
            if (_robotConstraints.MaterialCommunication)
            {
                transmissionSuccessful = _robotConstraints.ReceiverSensitivity <= signalStrength;
            }
            return new CommunicationInfo(totalDistance, angle, wallCellsDistance, regularCellsDistance, transmissionSuccessful, signalStrength);
        }

        public void LogicUpdate()
        {
            // Move messages sent last tick into readable messages
            _readableMessages.Clear();
            _readableMessages.AddRange(_queuedMessages);
            _queuedMessages.Clear();
            _localTickCounter++;

            _adjacencyMatrix.Clear();
            _adjacencyMatrixComputed = false;

            _communicationGroups.Clear();
            _communicationGroupsComputed = false;

            _receivedMessagesLastTick = _receivedMessagesThisTick;
            _receivedMessagesThisTick = 0;

            _sentMessagesLastTick = _sentMessagesThisTick;
            _sentMessagesThisTick = 0;

            _robotsCalledReadMessagesThisTick.Clear();


            if (GlobalSettings.PopulateAdjacencyAndComGroupsEveryTick)
            {
                PopulateAdjacencyMatrix();
                PopulateCommunicationGroups();
            }

            if (_robotConstraints.AutomaticallyUpdateSlam // Are we using slam?
                && _robotConstraints.DistributeSlam // Are we distributing slam?
                && _localTickCounter % _robotConstraints.SlamSynchronizeIntervalInTicks == 0)
            {
                SynchronizeSlamMaps(_localTickCounter);
            }

            if (GlobalSettings.ShouldWriteCsvResults && _localTickCounter % GlobalSettings.TicksPerStatsSnapShot == 0)
            {
                PopulateCommunicationGroups();

                CommunicationTracker.CreateSnapshot(_localTickCounter, _receivedMessagesLastTick, _sentMessagesLastTick, _communicationGroups);
            }
        }

        private void SynchronizeSlamMaps(int tick)
        {
            PopulateCommunicationGroups();

            foreach (var group in _communicationGroups)
            {
                var slamMaps = group
                    .Select(id => _robots.Single(r => r.id == id))
                    .Select(r => r.Controller.SlamMap)
                    .ToList();

                SlamMap.Synchronize(slamMaps, tick);
            }
        }

        public void PhysicsUpdate()
        {
            // No physics update needed
        }

        private void PopulateAdjacencyMatrix()
        {
            if (_adjacencyMatrixComputed)
            {
                return;
            }

            _adjacencyMatrixComputed = true;

            for (var i = 0; i < _robots.Count; i++)
            {
                for (var j = i + 1; j < _robots.Count; j++)
                {
                    var r1 = _robots[i];
                    var r2 = _robots[j];
                    var communication = CommunicationBetweenPoints((Vector2)r1.transform.position - _offset, (Vector2)r2.transform.position - _offset);

                    var reverseCommunication = new CommunicationInfo(communication.Distance,
                        (communication.Angle + 180f) % 360f, communication.WallCellsDistance,
                        communication.RegularCellsDistance, communication.TransmissionSuccessful,
                        communication.SignalStrength);

                    _adjacencyMatrix[(r1.id, r2.id)] = communication;
                    _adjacencyMatrix[(r2.id, r1.id)] = reverseCommunication;
                }
            }
        }

        private void PopulateCommunicationGroups()
        {
            if (_communicationGroupsComputed)
            {
                return;
            }

            _communicationGroupsComputed = true;

            PopulateAdjacencyMatrix();

            for (var i = 0; i < _robots.Count; i++)
            {
                var r1 = _robots[i];
                if (!_communicationGroups.Any(g => g.Contains(r1.id)))
                {
                    _communicationGroups.Add(GetCommunicationGroup(r1.id));
                }
            }
        }

        private HashSet<int> GetCommunicationGroup(int robotId)
        {
            var keys = new Queue<int>();
            keys.Enqueue(robotId);
            var resultSet = new HashSet<int> { robotId };

            while (keys.Count > 0)
            {
                var currentKey = keys.Dequeue();

                foreach (var (key, value) in _adjacencyMatrix)
                {
                    if (key.Item1 != currentKey || !value.TransmissionSuccessful || resultSet.Contains(key.Item2))
                    {
                        continue;
                    }

                    keys.Enqueue(key.Item2);
                    resultSet.Add(key.Item2);
                }
            }

            return resultSet;
        }

        public void DepositTag(MonaRobot robot, string content)
        {
            var tag = EnvironmentTaggingMap.AddTag(robot.transform.position, new EnvironmentTag(robot.id, robot.ClaimTag(), content));
            _visualizer.AddEnvironmentTag(tag);
        }

        public List<EnvironmentTag> ReadNearbyTags(MonaRobot robot)
        {
            var tags = EnvironmentTaggingMap.GetTagsNear(robot.transform.position,
                _robotConstraints.EnvironmentTagReadRange);

            return tags;
        }

        public List<SensedObject<int>> SenseNearbyRobots(int id)
        {
            PopulateAdjacencyMatrix();
            var sensedObjects = new List<SensedObject<int>>();

            foreach (var robot in _robots)
            {
                if (robot.id == id)
                {
                    continue;
                }
                var comInfo = _adjacencyMatrix![(id, robot.id)];

                if (!comInfo.TransmissionSuccessful || comInfo.Distance > _robotConstraints.SenseNearbyAgentsRange)
                {
                    continue;
                }

                sensedObjects.Add(new SensedObject<int>(comInfo.Distance, comInfo.Angle, robot.id));
            }

            return sensedObjects;
        }

        public void SetRobotReferences(IReadOnlyList<MonaRobot> robots)
        {
            _robots = robots;
        }


        // Attempts to detect a wall in the given direction. If present, it will return the intersection point and the
        // global angle (relative to x-axis) in degrees of the intersecting line
        public (Vector2, float)? DetectWall(MonaRobot robot, float globalAngle)
        {
            var range = _robotConstraints.EnvironmentTagReadRange;
            // Perform 3 parallel traces from the robot to determine if
            // a wall will be encountered if the robot moves straight ahead

            var robotPosition = robot.transform.position;

            // Perform trace from the center of the robot
            var result1 = RayTracingMap.FindIntersection(robotPosition, globalAngle, range, (_, tile) => !Tile.IsWall(tile.Type));
            var distance1 = result1 == null ? float.MaxValue : Vector2.Distance(robotPosition, result1.Value.Item1);
            var robotSize = _robotRelativeSize;

            // Perform trace from the left side perimeter of the robot
            var offsetLeft = Geometry.VectorFromDegreesAndMagnitude((globalAngle + 90) % 360, robotSize / 2f);
            var result2 = RayTracingMap.FindIntersection((Vector2)robot.transform.position + offsetLeft, globalAngle, range, (_, tile) => !Tile.IsWall(tile.Type));
            var distance2 = result2 == null ? float.MaxValue : Vector2.Distance(robotPosition, result2.Value.Item1);

            // Finally perform trace from the right side perimeter of the robot
            var offsetRight = Geometry.VectorFromDegreesAndMagnitude((globalAngle + 270) % 360, robotSize / 2f);
            var result3 = RayTracingMap.FindIntersection((Vector2)robot.transform.position + offsetRight, globalAngle, range, (_, tile) => !Tile.IsWall(tile.Type));
            var distance3 = result3 == null ? float.MaxValue : Vector2.Distance(robotPosition, result3.Value.Item1);

            // Return the detected wall that is closest to the robot
            var closestWall = result1;
            var closestWallDistance = distance1;

            if (distance2 < closestWallDistance)
            {
                closestWall = result2;
                closestWallDistance = distance2;
            }

            if (distance3 < closestWallDistance)
            {
                closestWall = result3;
            }

            return closestWall;
        }

        public Dictionary<Vector2Int, Bitmap> CalculateZones(IReadOnlyList<Vertex> vertices)
        {
            Dictionary<Vector2Int, Bitmap> vertexPositionsMultiThread = new(vertices.Count);
            Parallel.ForEach(vertices, vertex =>
                {
                    var bitmap = CalculateCommunicationZone(vertex.Position);
                    lock (vertexPositionsMultiThread)
                    {
                        vertexPositionsMultiThread.Add(vertex.Position, bitmap);
                    }
                }
            );
            return vertexPositionsMultiThread;
        }


        [MustDisposeResource]
        public Bitmap CalculateCommunicationZone(Vector2Int position)
        {
            var width = _tileMap.WidthInTiles;
            var height = _tileMap.HeightInTiles;
            var bitmap = new Bitmap(width, height);

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    var communicationInfo = CommunicationBetweenPoints(new Vector2(position.x, position.y), new Vector2(x + 0.5f, y + 0.5f));
                    if (communicationInfo.SignalStrength >= _robotConstraints.ReceiverSensitivity)
                    {
                        bitmap.Set(x, y);
                    }
                }
            }

            return bitmap;
        }

        // This method is an implementation of Siddon's algorithm which can be found in the following paper:
        // Siddon, R. L. (1985). Fast calculation of the exact radiological path for a three‐dimensional CT array
        // https://doi.org/10.1118/1.595715
        public CommunicationInfo CommunicationBetweenPoints(Vector2 start, Vector2 end)
        {
            var x1 = start.x;
            var y1 = start.y;
            var x2 = end.x;
            var y2 = end.y;
            var xDiff = x2 - x1;
            var yDiff = y2 - y1;
            var lineLength = Mathf.Sqrt(xDiff * xDiff + yDiff * yDiff);

            var signalStrength = _robotConstraints.TransmitPower;

            if (lineLength == 0)
            {
                return CreateCommunicationInfo(0, 0, 0, signalStrength);
            }

            // --- Prepare intersection alphas ---
            var maxSteps = Mathf.CeilToInt(Mathf.Abs(x2 - x1)) + Mathf.CeilToInt(Mathf.Abs(y2 - y1)) + 2;
            var alphas = ArrayPool<float>.Shared.Rent(maxSteps);
            var index = 0;
            alphas[index++] = 0; // Start

            var xStart = Mathf.FloorToInt(x1);
            var xEnd = Mathf.FloorToInt(x2);
            var yStart = Mathf.FloorToInt(y1);
            var yEnd = Mathf.FloorToInt(y2);

            var xStep = xDiff > 0 ? 1 : -1;
            var yStep = yDiff > 0 ? 1 : -1;

            var invXDiff = 1.0f / xDiff;
            var invYDiff = 1.0f / yDiff;

            var xi = xStart + (xDiff > 0 ? 1 : 0);
            var yi = yStart + (yDiff > 0 ? 1 : 0);

            // Merge X and Y alphas in sorted order
            var xCount = Mathf.Abs(xEnd - xStart);
            var yCount = Mathf.Abs(yEnd - yStart);

            var nextAlphaX = xCount > 0 ? (xi - x1) * invXDiff : float.PositiveInfinity;
            var nextAlphaY = yCount > 0 ? (yi - y1) * invYDiff : float.PositiveInfinity;

            // Add alphas from X and Y grid intersections (in sorted order)
            while (xCount > 0 || yCount > 0)
            {
                if (nextAlphaX < nextAlphaY)
                {
                    if (nextAlphaX is > 0f and < 1f)
                    {
                        alphas[index++] = nextAlphaX;
                    }

                    xi += xStep;
                    xCount--;
                    nextAlphaX = (xi - x1) * invXDiff;
                }
                else
                {
                    if (nextAlphaY is > 0f and < 1f)
                    {
                        alphas[index++] = nextAlphaY;
                    }

                    yi += yStep;
                    yCount--;
                    nextAlphaY = (yi - y1) * invYDiff;
                }
            }

            // Result containers
            var wallTileDistance = 0f;
            var otherTileDistance = 0f;

            alphas[index++] = 1f; // End 

            // Process each segment
            for (var i = 1; i < index; i++)
            {
                var aPrev = alphas[i - 1];
                var aCurrent = alphas[i];
                var aMid = 0.5f * (aPrev + aCurrent);
                var midX = x1 + aMid * xDiff;
                var midY = y1 + aMid * yDiff;
                var segmentLength = (aCurrent - aPrev) * lineLength;

                var tile = _tileMap.GetTileByLocalCoordinate(Mathf.FloorToInt(midX), Mathf.FloorToInt(midY));
                var tileType = tile.GetTriangles()[0].Type;

                if (tileType >= TileType.Wall)
                {
                    wallTileDistance += segmentLength;
                }
                else
                {
                    otherTileDistance += segmentLength;
                }

                if (_robotConstraints.MaterialCommunication)
                {
                    signalStrength -= 4 * segmentLength * _attenuationDict[tileType];
                }
            }
            ArrayPool<float>.Shared.Return(alphas, clearArray: false);

            var angle = Vector2.Angle(Vector2.right, end - start);
            return CreateCommunicationInfo(angle, wallTileDistance, otherTileDistance, signalStrength);
        }

    }
}