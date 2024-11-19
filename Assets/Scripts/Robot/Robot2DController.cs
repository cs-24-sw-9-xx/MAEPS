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
// Contributors: Rasmus Borrisholt Schmidt, Andreas Sebastian Sørensen, Thor Beregaard, Malte Z. Andreasen, Philip I. Holler and Magnus K. Jensen,
// 
// Original repository: https://github.com/Molitany/MAES

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Maes.Map;
using Maes.Robot.Task;
using Maes.Utilities;

using UnityEngine;

namespace Maes.Robot
{
    public class Robot2DController : IRobotController
    {
        private readonly Rigidbody2D _rigidbody;
        public Transform Transform { get; }
        public Transform LeftWheel { get; }
        public Transform RightWheel { get; }

        private const int RotateForce = 5;
        private const int MoveForce = 15;

        // Used for calculating wheel rotation for animation
        private Vector3? _previousLeftWheelPosition;
        private Vector3? _previousRightWheelPosition;

        private readonly MonaRobot _robot;
        private readonly Transform _robotTransform;
        private RobotStatus _currentStatus = RobotStatus.Idle;
        private ITask? _currentTask;

        // Set by RobotSpawner
        internal CommunicationManager CommunicationManager { get; set; } = null!;

        // Set by RobotSpawner
        public SlamMap SlamMap { get; set; } = null!;

        // Set by RobotSpawner
        public RobotConstraints Constraints { get; set; } = null!;

        private Queue<Vector2Int> _currentPath = new();
        private Vector2Int _currentTarget;

        // Returns the counterclockwise angle in degrees between the forward orientation of the robot and the x-axis
        public float GetForwardAngleRelativeToXAxis()
        {
            return ((Vector2)Transform.up).GetAngleRelativeToX();
        }

        private Vector2 GetRobotDirectionVector()
        {
            var angle = GetForwardAngleRelativeToXAxis();
            return new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
        }


        // Indicates whether the robot has entered a new collision since the previous logic update
        private bool _newCollisionSinceLastUpdate;

        // When the robot enters a collision (such as with a wall) Unity will only notify of the
        // collision upon initial impact. If the robot continues to drive into the wall,
        // no further collision notifications will be received. To counteract this problem, the controller will 
        // reissue the collision notification if the collision flag is not cleared and the robot is following an
        // instruction to move forward. Because the acceleration of the robot is relatively slow, the collision
        // exit may not be triggered until after a few physics updates. This variable determines how many physics
        // updates to wait before re-declaring the collision.
        private const int MovementUpdatesBeforeRedeclaringCollision = 2;
        private int _physicsUpdatesSinceStartingMovement;

        public readonly List<(Vector3, float)> DebugCircle = new();

        public Robot2DController(Rigidbody2D rigidbody, Transform transform, Transform leftWheel, Transform rightWheel,
            MonaRobot robot)
        {
            _rigidbody = rigidbody;
            Transform = transform;
            LeftWheel = leftWheel;
            RightWheel = rightWheel;
            _robot = robot;
            _robotTransform = _robot.transform;
        }

        public MonaRobot GetRobot()
        {
            return _robot;
        }

        public int GetRobotID()
        {
            return _robot.id;
        }

        public void UpdateLogic()
        {
            // Clear the collision flag
            _newCollisionSinceLastUpdate = false;
        }

        public bool HasCollidedSinceLastLogicTick()
        {
            return _newCollisionSinceLastUpdate;
        }

        // Whether the rigidbody is currently colliding with something
        public bool IsCurrentlyColliding { get; private set; }

        public void NotifyCollided()
        {
            _newCollisionSinceLastUpdate = true;
            IsCurrentlyColliding = true;
            StopCurrentTask();
        }

        public void NotifyCollisionExit()
        {
            IsCurrentlyColliding = false;
        }

        public void UpdateMotorPhysics()
        {
            // Calculate movement delta between current and last physics tick
            var leftWheelVelocityVector = LeftWheel.position - _previousLeftWheelPosition ?? Vector3.zero;
            var rightWheelVelocityVector = RightWheel.position - _previousRightWheelPosition ?? Vector3.zero;

            // For each wheel, determine whether it has moved forwards or backwards
            var forward = Transform.forward;
            var leftWheelMoveDirection = Vector3.Dot(forward, leftWheelVelocityVector) < 0 ? -1f : 1f;
            var rightWheelMoveDirection = Vector3.Dot(forward, rightWheelVelocityVector) < 0 ? -1f : 1f;

            // Animate rotating wheels to match movement of the robot
            AnimateWheelRotation(LeftWheel, leftWheelMoveDirection, leftWheelVelocityVector.magnitude);
            AnimateWheelRotation(RightWheel, rightWheelMoveDirection, rightWheelVelocityVector.magnitude);

            _previousLeftWheelPosition = LeftWheel.position;
            _previousRightWheelPosition = RightWheel.position;

            // Update the current status to indicate whether the robot is currently moving, stopping or idle
            if (_currentTask != null)
            {
                // The robot is currently following an assigned task
                _currentStatus = RobotStatus.Moving;
            }
            else if (rightWheelVelocityVector.magnitude > 0.01f || leftWheelVelocityVector.magnitude > 0.01f)
            {
                // The robot is moving but is not following a task, it assumed to be in the process of stopping
                _currentStatus = RobotStatus.Stopping;
            }
            else
            {
                _currentStatus = RobotStatus.Idle;
            }

            var isAttemptingToMoveForwards = _currentTask is MovementTask;
            if (IsCurrentlyColliding && isAttemptingToMoveForwards)
            {
                if (_physicsUpdatesSinceStartingMovement > MovementUpdatesBeforeRedeclaringCollision)
                {
                    NotifyCollided();
                }

                _physicsUpdatesSinceStartingMovement += 1;
            }
            else
            {
                // Reset counter
                _physicsUpdatesSinceStartingMovement = 0;
            }

            // Get directive from current task if present
            var directive = _currentTask?.GetNextDirective();

            if (directive != null)
            {
                ApplyWheelForce(directive.Value);
            }

            // Delete task once completed
            var isCurrentTaskCompleted = _currentTask?.IsCompleted() ?? false;
            if (isCurrentTaskCompleted)
            {
                _currentTask = null;
            }

            if (directive != null)
            {
                ApplyWheelForce(directive.Value);
            }
        }

        // The robot is rotated relative to Unity's coordinate system, so 'up' is actually forward for the robot
        public Vector3 GetForwardDirectionVector()
        {
            return Transform.up;
        }

        // Applies force at the positions of the wheels to create movement using physics simulation
        private void ApplyWheelForce(MovementDirective directive)
        {
            var leftPosition = LeftWheel.position;
            var rightPosition = RightWheel.position;

            var forward = GetForwardDirectionVector();

            // Force changes depending on whether the robot is rotating or accelerating
            var force = MoveForce;
            if (directive.IsRotational())
            {
                force = RotateForce;
            }

            // Apply force at teach wheel
            _rigidbody.AddForceAtPosition(forward * (force * directive.LeftWheelSpeed), leftPosition);
            _rigidbody.AddForceAtPosition(forward * (force * directive.RightWheelSpeed), rightPosition);
        }

        // Rotates the given wheel depending on how far it has moved an in which direction
        private static void AnimateWheelRotation(Transform wheel, float direction, float magnitude)
        {
            // This factor determines how forward movement of the wheel translates into rotation
            const float rotationFactor = 180f;
            wheel.Rotate(new Vector3(rotationFactor * direction * magnitude, 0f, 0f));
        }

        public RobotStatus GetStatus()
        {
            if (_currentStatus == RobotStatus.Idle && _currentTask != null)
            {
                return RobotStatus.Moving;
            }

            return _currentStatus;
        }

        public void Rotate(float degrees)
        {
            if (_currentTask != null)
            {
                StopCurrentTask();
                return;
            }

            AssertRobotIsInIdleState("rotation");

            _currentTask = new FiniteRotationTask(Transform, degrees);
        }

        public void StartRotating(bool counterClockwise = false)
        {
            if (_currentTask != null)
            {
                StopCurrentTask();
                return;
            }

            AssertRobotIsInIdleState("rotation");
            _currentTask = new InfiniteRotationTask(Constraints.RelativeMoveSpeed * (counterClockwise ? -1 : 1));
        }

        public void StartRotatingAroundPoint(Vector2Int point, bool counterClockwise = false)
        {
            AssertRobotIsInIdleState("Rotating around point");
            var coarseLocation = SlamMap.CoarseMap.GetCurrentPosition();
            var radius = Vector2.Distance(Vector2Int.FloorToInt(coarseLocation), point);
            var worldPoint = SlamMap.CoarseMap.TileToWorld(point);
            var distanceBetweenWheels = Vector2.Distance(LeftWheel.position, RightWheel.position);
            DebugCircle.Add((worldPoint, radius - distanceBetweenWheels / 2));
            DebugCircle.Add((worldPoint, radius + distanceBetweenWheels / 2));
            _currentTask = new RotateAroundPointTask(point, radius, Constraints.RelativeMoveSpeed, counterClockwise);
        }


        public void StartMoving(bool reverse = false)
        {
            AssertRobotIsInIdleState("Moving Forwards");
            _currentTask = new MovementTask(Constraints.RelativeMoveSpeed * (reverse ? -1 : 1));
        }

        // Asserts that the current status is idle, and throws an exception if not
        protected void AssertRobotIsInIdleState(string attemptedActionName)
        {
            var currentStatus = GetStatus();
            if (currentStatus != RobotStatus.Idle)
            {
                throw new InvalidOperationException(
                    $"Tried to start action: '{attemptedActionName}' rotation action but current status is: {Enum.GetName(typeof(RobotStatus), currentStatus)}Can only start '{attemptedActionName}' action when current status is Idle");
            }
        }


        public void StopCurrentTask()
        {
            _currentTask = null;
        }

        public void Broadcast(object data)
        {
            CommunicationManager.BroadcastMessage(_robot, data);
        }

        public List<object> ReceiveBroadcast()
        {
            return CommunicationManager.ReadMessages(_robot);
        }

        public IRobotController.DetectedWall? DetectWall(float globalAngle)
        {
#if DEBUG
            if (globalAngle < 0 || globalAngle > 360)
            {
                throw new ArgumentException("Global angle argument must be between 0 and 360." +
                                            $"Given angle was {globalAngle}");
            }
#endif

            var result = CommunicationManager.DetectWall(_robot, globalAngle);
            if (result == null)
            {
                return null;
            }

            var intersection = result.Value.Item1;
            var distance = Vector2.Distance(intersection, _robotTransform.position);
            var intersectingWallAngle = result.Value.Item2;

            // Calculate angle of wall relative to current forward angle of the robot
            var relativeWallAngle = Math.Abs(intersectingWallAngle - GetForwardAngleRelativeToXAxis());

            // Convert to relative wall angle to range 0-90
            relativeWallAngle %= 180;
            if (relativeWallAngle > 90)
            {
                relativeWallAngle = 180 - relativeWallAngle;
            }

            return new IRobotController.DetectedWall(distance, relativeWallAngle);
        }

        public string GetDebugInfo()
        {
            var info = new StringBuilder();

            info.Append("id: ");
            info.AppendLine(_robot.id.ToString());

            info.Append("Current task: ");
            info.AppendLine(_currentTask == null ? "none" : _currentTask.GetType().ToString());

            var position = Transform.position;
            info.Append("World Position: ");
            info.Append(position.x.ToString("#.0"));
            info.Append(", ");
            info.AppendLine(position.y.ToString("#.0"));

            info.Append("Slam tile: ");
            info.AppendLine(SlamMap.GetCurrentPosition().ToString());

            info.Append("Coarse tile: ");
            info.AppendLine(SlamMap.CoarseMap.GetApproximatePosition().ToString());

            info.Append("Is colliding: ");
            info.AppendLine(IsCurrentlyColliding.ToString());

            return info.ToString();
        }

        public void Move(float distanceInMeters, bool reverse = false)
        {
            AssertRobotIsInIdleState("Move forwards");
            _currentTask = new FiniteMovementTask(Transform, distanceInMeters, Constraints.RelativeMoveSpeed, reverse);
        }

        /// <summary>
        /// Paths and moves to the tile along the path
        /// Uses and moves along coarse tiles, handling the path by itself
        /// Must be called continuously untill the final target is reached
        /// If there is already a path, does not recompute
        /// </summary>
        /// <param name="tile">COARSEGRAINED tile as final target</param>
        public void PathAndMoveTo(Vector2Int tile)
        {
            if (GetStatus() != RobotStatus.Idle)
            {
                return;
            }

            if (_currentPath.Count > 0 && _currentPath.Last() != tile)
            {
                _currentPath.Clear();
            }

            if (_currentPath.Count == 0)
            {
                var robotCurrentPosition = Vector2Int.FloorToInt(SlamMap.CoarseMap.GetApproximatePosition());
                if (robotCurrentPosition == tile)
                {
                    return;
                }

                var pathList = SlamMap.CoarseMap.GetPath(tile, false);
                if (pathList == null)
                {
                    return;
                }

                _currentPath = new Queue<Vector2Int>(pathList);
                _currentTarget = _currentPath.Dequeue();
            }
            if (SlamMap.CoarseMap.GetTileStatus(_currentTarget) == SlamMap.SlamTileStatus.Solid)
            {
                _currentTarget = _currentPath.Dequeue();
            }

            var relativePosition = SlamMap.CoarseMap.GetTileCenterRelativePosition(_currentTarget);
            if (relativePosition.Distance < 0.5f)
            {
                if (_currentPath.Count == 0)
                {
                    return;
                }

                _currentTarget = _currentPath.Dequeue();
                relativePosition = SlamMap.CoarseMap.GetTileCenterRelativePosition(_currentTarget);
            }
            #region DrawPath
#if DEBUG
            Debug.DrawLine(SlamMap.CoarseMap.TileToWorld(Vector2Int.FloorToInt(SlamMap.CoarseMap.GetApproximatePosition())), SlamMap.CoarseMap.TileToWorld(_currentTarget), Color.cyan, 2);
            for (var i = 0; i < _currentPath.Count - 1; i++)
            {
                var pathSteps = _currentPath.ToArray();
                if (i == 0)
                {
                    Debug.DrawLine(SlamMap.CoarseMap.TileToWorld(_currentTarget), SlamMap.CoarseMap.TileToWorld(pathSteps[i]), Color.cyan, 2);
                }

                Debug.DrawLine(SlamMap.CoarseMap.TileToWorld(pathSteps[i]), SlamMap.CoarseMap.TileToWorld(pathSteps[i + 1]), Color.cyan, 2);
            }
#endif
            #endregion
            if (Math.Abs(relativePosition.RelativeAngle) > 1.5f)
            {
                Rotate(relativePosition.RelativeAngle);
            }
            else if (relativePosition.Distance > 0.5f)
            {
                Move(relativePosition.Distance);
            }
        }

        /// <summary>
        /// Estimates the time of arrival for the robot to reach the specified destination.
        /// Uses the path from PathAndMoveTo and the robots max speed (RobotConstraints.RelativeMoveSpeed) to calculate the ETA.
        /// </summary>
        /// <param name="target">the target that the path should end at.</param>
        /// <param name="acceptPartialPaths">if <b>true</b>, returns the distance of the path getting the closest to the target, if no full path can be found.</param>
        /// <param name="beOptimistic">if <b>true</b>, treats unseen tiles as open in the path finding algorithm. Treats unseen tiles as solid otherwise.</param>
        public int? EstimateTimeToTarget(Vector2Int target, bool acceptPartialPaths = false, bool beOptimistic = false)
        {
            // An estimation for the distance it takes the robot to reach terminal speed.
            const float distForMaxSpeed = 2.5f;
            var distance = EstimateDistanceToTarget(target);
            if (distance == null)
            {
                return null;
            }
            var dist = distance.Value;
            var startDist = Math.Min(dist, distForMaxSpeed);
            // If the distance is small, it's characterized by a quadratic function.
            var startTime = (int)Math.Floor(Math.Pow(CorrectForRelativeMoveSpeed(startDist, Constraints.RelativeMoveSpeed), 0.85));
            if (dist <= distForMaxSpeed)
            {
                return startTime;
            }
            else
            {
                // If the distance is long, the robot reaches terminal speed, and is characterized as a liniar function.
                dist -= distForMaxSpeed;
                return (int)Math.Ceiling(CorrectForRelativeMoveSpeed(dist, Constraints.RelativeMoveSpeed)) + startTime;
            }

            static float CorrectForRelativeMoveSpeed(float distance, float relativeMoveSpeed)
            {
                // These constants are fitted not calculated.
                return distance * 3.2f / (0.21f + (relativeMoveSpeed / 3.0f));
            }
        }

        /// <summary>
        /// Estimates the distance for robot to reach the specified destination.
        /// Uses the path from PathAndMoveTo to calculate distance.
        /// </summary>
        /// <param name="target">the target that the path should end at.</param>
        /// <param name="acceptPartialPaths">if <b>true</b>, returns the distance of the path getting the closest to the target, if no full path can be found.</param>
        /// <param name="beOptimistic">if <b>true</b>, treats unseen tiles as open in the path finding algorithm. Treats unseen tiles as solid otherwise.</param>
        public float? EstimateDistanceToTarget(Vector2Int target, bool acceptPartialPaths = false, bool beOptimistic = false)
        {
            if (SlamMap.CoarseMap.GetTileCenterRelativePosition(target).Distance < 0.5f)
            {
                return 0f;
            }

            var pathList = SlamMap.CoarseMap.GetPath(target, acceptPartialPaths, beOptimistic);
            if (pathList == null)
            {
                return null;
            }

            var distance = 0f;
            for (var i = 0; i < pathList.Count() - 1; i++)
            {
                // Get current point and next point
                var point1 = pathList[i];
                var point2 = pathList[i + 1];

                // Calculate the Euclidean distance between the two points
                distance += Vector2.Distance(point1, point2);
            }
            return distance;
        }

        /// <summary>
        /// Rotates and moves directly to target unless already moving or already on target
        /// </summary>
        /// <param name="target">COARSEGRAINED tile to move to</param>
        public void MoveTo(Vector2Int target)
        {
            var relativePosition = SlamMap.CoarseMap.GetTileCenterRelativePosition(target);
            if (GetStatus() != RobotStatus.Idle || relativePosition.Distance < 0.5f)
            {
                return;
            }

            if (Math.Abs(relativePosition.RelativeAngle) > 0.5f)
            {
                Rotate(relativePosition.RelativeAngle);
            }
            else
            {
                Move(relativePosition.Distance);
            }
        }


        public float GetGlobalAngle()
        {
            return GetForwardAngleRelativeToXAxis();
        }

        // Deposits an environment tag at the current position of the robot
        public void DepositTag(string content)
        {
            CommunicationManager.DepositTag(_robot, content);
        }

        // Returns a list of all environment tags that are within sensor range
        public List<RelativeObject<EnvironmentTag>> ReadNearbyTags()
        {
            var tags = CommunicationManager.ReadNearbyTags(_robot);
            return tags.Select(tag => ToRelativePosition(tag.MapPosition, tag)).ToList();
        }

        private RelativeObject<T> ToRelativePosition<T>(Vector2 tagPosition, T item)
        {
            var robotPosition = (Vector2)_robotTransform.position;
            var distance = Vector2.Distance(robotPosition, tagPosition);
            var angle = Vector2.SignedAngle(GetRobotDirectionVector(), tagPosition - robotPosition);
            return new RelativeObject<T>(distance, angle, item);
        }

        public SensedObject<int>[] SenseNearbyRobots()
        {
            return CommunicationManager.SenseNearbyRobots(_robot.id)
                .Select(e => new SensedObject<int>(
                    e.Distance,
                    Vector2.SignedAngle(_robot.transform.up,
                                                new Vector2(Mathf.Cos(e.Angle * Mathf.Deg2Rad),
                                                            Mathf.Sin(e.Angle * Mathf.Deg2Rad))),
                    e.Item))
                .ToArray();
        }

        public SlamMap GetSlamMap()
        {
            return SlamMap;
        }

        public bool IsRotating()
        {
            return _currentTask is FiniteRotationTask || _currentTask is InfiniteRotationTask;
        }

        public bool IsPerformingDifferentialDriveTask()
        {
            return _currentTask is InfiniteDifferentialMovementTask;
        }

        public bool IsRotatingIndefinitely()
        {
            return _currentTask is InfiniteRotationTask;
        }

        // This method requires the robot to currently be idle or already be performing an infinite rotation 
        public void RotateAtRate(float forceMultiplier)
        {
            if (forceMultiplier < -1.0f || forceMultiplier > 1.0f)
            {
                throw new ArgumentException($"Force multiplier must be in range [-1.0, 1.0]. Given value: {forceMultiplier}");
            }

            if (_currentTask is InfiniteRotationTask currentRotationTask)
            {
                // Adjust existing rotation task
                currentRotationTask.ForceMultiplier = Constraints.RelativeMoveSpeed * forceMultiplier;
            }
            else
            {
                // Create new rotation task
                AssertRobotIsInIdleState("infinite rotation");
                _currentTask = new InfiniteRotationTask(Constraints.RelativeMoveSpeed * forceMultiplier);
            }
        }

        // This method requires the robot to either be idle or already be performing an infinite movement
        public void MoveAtRate(float forceMultiplier)
        {
            if (forceMultiplier < -1.0f || forceMultiplier > 1.0f)
            {
                throw new ArgumentException(
                    $"Force multiplier must be in range [-1.0, 1.0]. Given value: {forceMultiplier}");
            }

            if (_currentTask is MovementTask currentMovementTask)
            {
                // Adjust existing movement task
                currentMovementTask.ForceMultiplier = Constraints.RelativeMoveSpeed * forceMultiplier;
            }
            else
            {
                // Create new movement task
                AssertRobotIsInIdleState("Infinite movement");
                _currentTask = new MovementTask(Constraints.RelativeMoveSpeed * forceMultiplier);
            }
        }

        // This method allows for differential drive (each wheel is controlled separately)
        public void SetWheelForceFactors(float leftWheelForce, float rightWheelForce)
        {
            // Apply force multiplier from robot constraints (this value varies based on robot size)
            leftWheelForce *= Constraints.RelativeMoveSpeed;
            rightWheelForce *= Constraints.RelativeMoveSpeed;

            if (_currentTask is InfiniteDifferentialMovementTask existingTask)
            {
                // Update the existing differential movement task
                existingTask.UpdateWheelForces(leftWheelForce, rightWheelForce);
            }
            else
            {
                // The robot must be in idle state to start this task
                AssertRobotIsInIdleState("Differential movement");
                _currentTask = new InfiniteDifferentialMovementTask(leftWheelForce, rightWheelForce);
            }
        }
    }
}