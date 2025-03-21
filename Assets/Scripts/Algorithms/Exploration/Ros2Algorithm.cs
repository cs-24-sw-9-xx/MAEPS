// Copyright 2022 MAES
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
// Contributors: Malte Z. Andreasen, Philip I. Holler and Magnus K. Jensen
// 
// Original repository: https://github.com/MalteZA/MAES

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Maes.Robot;
using Maes.Robot.Tasks;
using Maes.Utilities;

using RosMessageTypes.Geometry;
using RosMessageTypes.Maes;

using Unity.Robotics.ROSTCPConnector;

using UnityEngine;

namespace Maes.Algorithms.Exploration
{
    internal class Ros2Algorithm : IExplorationAlgorithm
    {
        // Set by SetController
        private Robot2DController _controller = null!;

        // Set by SetController
        private ROSConnection _ros = null!;

        // Set by SetController
        private string _robotRosId = null!; // e.g. robot0

        // Set by SetController
        private string _topicPrefix = null!; // Is set in SetController method, e.g. /robot0

        // Set by SetController
        private Transform _worldPosition = null!; // Used for finding position of relative objects and sending it to ROS

        private const string _stateTopic = "/maes_state";
        private const string _broadcastTopic = "/maes_broadcast";
        private const string _depositTagTopic = "/maes_deposit_tag";
        private const string _cmdVelTopic = "/cmd_vel";

        private int _tick;

        // Used to react to cmlVel from ROS
        private float _rosLinearSpeed;
        private float _rosRotationSpeed;

        // Service calls from ROS uses a callback function. We need to store the results 
        // and act on them in the next logic tick
        private readonly List<string> _msgsToBroadcast = new List<string>();
        private readonly List<string> _envTagsToDeposit = new List<string>();

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
                ReactToCmdVel(_rosLinearSpeed, _rosRotationSpeed);

                if (_envTagsToDeposit.Count != 0)
                {
                    ReactToBroadcastRequests();
                }

                if (_msgsToBroadcast.Count != 0)
                {
                    ReactToDepositTagRequests();
                }

                PublishState();

                _tick++;

                yield return WaitForCondition.WaitForLogicTicks(1);
            }
        }

        private void ReactToDepositTagRequests()
        {
            foreach (var tagMsg in _envTagsToDeposit)
            {
                _controller.DepositTag(tagMsg);
            }
            _envTagsToDeposit.Clear();
        }

        private void ReactToBroadcastRequests()
        {
            foreach (var msg in _msgsToBroadcast)
            {
                _controller.Broadcast(new RosBroadcastMsg(msg, _robotRosId));
            }
            _msgsToBroadcast.Clear();
        }

        private void PublishState()
        {
            var state = new StateMsg();
            var robotPosition = (Vector2)_worldPosition.position;
            var robotRotation = _worldPosition.rotation.eulerAngles.z - 90f;
            // Flip signs like also done in TransformTreePublisher 
            // TODO: Maybe create utility function for transforming coordinates between ROS and Maes ? - Philip
            robotPosition = Geometry.ToRosCoord(robotPosition);
            // ---- tick ---- //
            state.tick = _tick;
            // ---- Status ---- //
            state.status = Enum.GetName(typeof(RobotStatus), _controller.Status);

            // ---- Collision ---- //
            state.colliding = _controller.IsCurrentlyColliding;

            // ---- Incoming broadcast messages ---- //
            var objectsReceived = _controller.ReceiveBroadcast();
            var msgsReceived = objectsReceived.Cast<RosBroadcastMsg>().ToList();
            var broadcastMsgs = msgsReceived.Select(e => new BroadcastMsg(e.msg, e.sender));
            state.incoming_broadcast_msgs = broadcastMsgs.ToArray();

            // ---- Nearby Robots ---- //
            var nearbyRobots = _controller.SenseNearbyRobots();
            // Map to relative positions of other robots
            var otherRobots = nearbyRobots.Select(e => (item: e.Item, e.GetRelativePosition(robotPosition, robotRotation)));
            // Convert to ros messages
            var nearbyRobotMsgs = otherRobots.Select(e =>
                new NearbyRobotMsg(e.item.ToString(), new Vector2DMsg(e.Item2.x, e.Item2.y)));
            state.nearby_robots = nearbyRobotMsgs.ToArray();

            // ---- Nearby environment tags ---- //
            var tags = _controller.ReadNearbyTags();
            var rosTagsWithPos = tags.Select(e => (e.Item.Content, GetRelativePosition(robotPosition, robotRotation, e)));
            var rosTagAsMsgs =
                    rosTagsWithPos.Select(e => new EnvironmentTagMsg(e.Content, new Vector2DMsg(e.Item2.x, e.Item2.y)));
            state.tags_nearby = rosTagAsMsgs.ToArray();

            // ---- Publish to ROS ---- //
            _ros.Publish(_topicPrefix + _stateTopic, state);
        }

        private void ReactToCmdVel(float speedCommandValue, float rotationCommandValue)
        {
            // Debug.Log($"{this._robotRosId} command velocities: [{speedCommandValue}, {rotationCommandValue}]");
            var robotStatus = _controller.Status;

            if (Math.Abs(speedCommandValue) < 0.01f && Math.Abs(rotationCommandValue) > 0.0)
            {
                if (robotStatus != RobotStatus.Idle && !_controller.IsRotatingIndefinitely())
                {
                    // The robot is currently performing another task - Stop that task and continue
                    _controller.StopCurrentTask();
                }
                else
                {
                    var sign = rotationCommandValue > 0 ? -1 : 1;
                    var force = Mathf.Pow(1.3f * rotationCommandValue, 2.0f) * 0.6f;
                    force = Mathf.Min(1f, force); // Ensure maximum force of 1.0 
                    force = sign * force; // Apply direction / sign +-
                    _controller.RotateAtRate(force);
                }
            }
            else if (speedCommandValue > 0)
            {
                if (robotStatus != RobotStatus.Idle && !_controller.IsPerformingDifferentialDriveTask())
                {
                    // The robot must stop current task before starting the desired movement task
                    _controller.StopCurrentTask();
                }
                else
                {
                    // The force applied at each wheel before factoring in rotation
                    var flatWheelForce = speedCommandValue;

                    // The difference in applied force between the right and left wheel. 
                    float rotationSign = rotationCommandValue > 0 ? -1 : 1;
                    var wheelForceDelta = rotationSign * Mathf.Pow(1.3f * rotationCommandValue, 2.0f) * 0.6f;

                    // Calculate the force applied to each wheel and send the values to the controller
                    var leftWheelForce = flatWheelForce + wheelForceDelta / 2f;
                    var rightWheelForce = flatWheelForce - wheelForceDelta / 2f;
                    _controller.SetWheelForceFactors(leftWheelForce, rightWheelForce);
                }
            }
            else if (_controller.Status != RobotStatus.Idle)
            {
                // If cmd_vel does not indicate any desired movement - then stop robot if currently moving 
                // Debug.Log("Stopping movement!");
                _controller.StopCurrentTask();
            }
        }

        private void ReceiveRosCmd(TwistMsg cmdVel)
        {
            _rosLinearSpeed = (float)cmdVel.linear.x;
            _rosRotationSpeed = (float)cmdVel.angular.z;
            // Debug.Log($"Robot {_controller.GetRobotID()}: Received cmdVel twist: {cmdVel.ToString()}");
        }

        public string GetDebugInfo()
        {
            var info = new StringBuilder();

            var robotPosition = (Vector2)(-_worldPosition.position);

            info.AppendLine($"Robot ID: {_robotRosId}");
            info.AppendLine($"Namespace: {_topicPrefix}");
            info.AppendLine($"Position: ({robotPosition.x},{robotPosition.y})");
            info.AppendLine($"Status: {Enum.GetName(typeof(RobotStatus), _controller.Status)}");
            info.AppendLine($"Is Colliding: {_controller.IsCurrentlyColliding}");
            info.AppendLine($"Number of nearby robots: {_controller.SenseNearbyRobots().Length}");
            info.AppendLine($"Number Incoming broadcast msg: {_controller.ReceiveBroadcast().Count}");
            info.AppendLine($"Number of nearby env tags: {_controller.ReadNearbyTags().Count}");
            info.AppendLine($"rosLinearSpeed: {_rosLinearSpeed}");
            info.AppendLine($"rosRotationalSpeed: {_rosRotationSpeed}");

            return info.ToString();
        }

        public void SetController(Robot2DController controller)
        {
            _controller = controller;
            _ros = ROSConnection.GetOrCreateInstance();
            _topicPrefix = $"/robot{_controller.Id}";
            _robotRosId = $"robot{_controller.Id}";

            _worldPosition = GameObject.Find($"robot{_controller.Id}").transform;

            // Register state publisher
            _ros.RegisterPublisher<StateMsg>(_topicPrefix + _stateTopic);

            // Register broadcast and deposit tag services
            _ros.ImplementService<BroadcastToAllRequest, BroadcastToAllResponse>(_topicPrefix + _broadcastTopic, BroadcastMessage);
            _ros.ImplementService<DepositTagRequest, DepositTagResponse>(_topicPrefix + _depositTagTopic, DepositTag);

            // Subscribe to cmdVel from Nav2
            _ros.Subscribe<TwistMsg>(_topicPrefix + _cmdVelTopic, ReceiveRosCmd);
        }

        private DepositTagResponse DepositTag(DepositTagRequest req)
        {
            _controller.DepositTag(req.msg);
            return new DepositTagResponse(true);
        }

        private BroadcastToAllResponse BroadcastMessage(BroadcastToAllRequest req)
        {
            _controller.Broadcast(new RosBroadcastMsg(req.msg, _robotRosId));
            return new BroadcastToAllResponse(true);
        }

        private class RosBroadcastMsg
        {
            public string msg;
            public string sender;

            public RosBroadcastMsg(string msg, string sender)
            {
                this.msg = msg;
                this.sender = sender;
            }
        }

        private Vector2 GetRelativePosition<T>(Vector2 myPosition, float globalAngle, RelativeObject<T> o)
        {
            var x = myPosition.x + (o.Distance * Mathf.Cos(Mathf.Deg2Rad * ((o.RelativeAngle + globalAngle) % 360)));
            var y = myPosition.y + (o.Distance * Mathf.Sin(Mathf.Deg2Rad * ((o.RelativeAngle + globalAngle) % 360)));
            return new Vector2(x, y);
        }

    }
}