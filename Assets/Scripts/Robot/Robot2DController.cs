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
using Maes.Robot.Tasks;
using Maes.Utilities;

using UnityEngine;

namespace Maes.Robot
{
    public sealed class Robot2DController : IRobotController
    {
        public int Id => _robot.id;

        public int AssignedPartition
        {
            get => _robot.AssignedPartition;
            set => _robot.AssignedPartition = value;
        }

        public Color32 Color => _robot.Color;

        public RobotStatus Status
        {
            get
            {
                if (_currentStatus == RobotStatus.Idle && _currentTask != null)
                {
                    return RobotStatus.Moving;
                }

                return _currentStatus;
            }
        }


        private readonly Rigidbody2D _rigidbody;

        [ForbiddenKnowledge]
        public Transform Transform { get; }

        [ForbiddenKnowledge]
        public Transform LeftWheel { get; }

        [ForbiddenKnowledge]
        public Transform RightWheel { get; }

        [ForbiddenKnowledge]
        public float TotalDistanceTraveled { get; private set; }

        public float GlobalAngle => GetForwardAngleRelativeToXAxis();

        private Vector2 _previousPosition;

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
        [ForbiddenKnowledge]
        internal CommunicationManager CommunicationManager { get; set; } = null!;

        // Set by RobotSpawner
        public SlamMap SlamMap { get; set; } = null!;

        // Set by RobotSpawner
        [ForbiddenKnowledge]
        public RobotConstraints Constraints { get; set; } = null!;

        // Set by RobotSpawner
        public TravelEstimator TravelEstimator { get; set; } = null!;

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

        [ForbiddenKnowledge]
        public Robot2DController(Rigidbody2D rigidbody, Transform transform, Transform leftWheel, Transform rightWheel,
            MonaRobot robot)
        {
            _rigidbody = rigidbody;
            Transform = transform;
            LeftWheel = leftWheel;
            RightWheel = rightWheel;
            _robot = robot;
            _robotTransform = _robot.transform;
            _previousPosition = _rigidbody.position;
        }

        [ForbiddenKnowledge]
        public void UpdateLogic()
        {
            // Clear the collision flag
            HasCollidedSinceLastLogicTick = false;
            CalculateTravelDistance();
        }

        public bool HasCollidedSinceLastLogicTick { get; private set; }

        // Whether the rigidbody is currently colliding with something
        public bool IsCurrentlyColliding { get; private set; }

        [ForbiddenKnowledge]
        public void NotifyCollided()
        {
            HasCollidedSinceLastLogicTick = true;
            IsCurrentlyColliding = true;
            StopCurrentTask();
        }

        [ForbiddenKnowledge]
        public void NotifyCollisionExit()
        {
            IsCurrentlyColliding = false;
        }

        [ForbiddenKnowledge]
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
            var isCurrentTaskCompleted = _currentTask?.IsCompleted ?? false;
            if (isCurrentTaskCompleted)
            {
                _currentTask = null;
            }

            if (directive != null)
            {
                ApplyWheelForce(directive.Value);
            }
        }

        private void CalculateTravelDistance()
        {
            var currentPosition = _rigidbody.position;
            var distance = Vector2.Distance(_previousPosition, currentPosition);
            if (distance < 0.1f)
            {
                return;
            }
            TotalDistanceTraveled += distance;
            _previousPosition = currentPosition;
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
        private void AssertRobotIsInIdleState(string attemptedActionName)
        {
            var currentStatus = Status;
            if (currentStatus != RobotStatus.Idle)
            {
                throw new InvalidOperationException(
                    $"Tried to start action: '{attemptedActionName}' rotation action but current status is: {Enum.GetName(typeof(RobotStatus), currentStatus)}Can only start '{attemptedActionName}' action when current status is Idle");
            }
        }


        public void StopCurrentTask()
        {
            _currentTask = null;
            _currentPath.Clear();
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

        private readonly StringBuilder _debugStringBuilder = new();

        [ForbiddenKnowledge]
        public string GetDebugInfo()
        {
            _debugStringBuilder.Clear();

            _debugStringBuilder.AppendFormat("id: {0}\n", _robot.id);
            _debugStringBuilder.AppendFormat("Current task: {0}\n", _currentTask?.GetType().Name ?? "none");

            var position = Transform.position;
            _debugStringBuilder.AppendFormat("World Position: {0:#.0}, {1:#.0}\n", position.x, position.y);

            _debugStringBuilder.AppendFormat("Slam tile: {0}\n", SlamMap.GetCurrentPosition());

            _debugStringBuilder.AppendFormat("Coarse tile: {0}\n", SlamMap.CoarseMap.GetApproximatePosition());

            _debugStringBuilder.AppendFormat("Is colliding: {0}\n", IsCurrentlyColliding);

            _debugStringBuilder.AppendFormat("Partition: {0}\n", AssignedPartition);

            return _debugStringBuilder.ToString();
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
        /// <param name="dependOnBrokenBehaviour"></param>
        public void PathAndMoveTo(Vector2Int tile, bool dependOnBrokenBehaviour = true)
        {
            var closeness = dependOnBrokenBehaviour ? 0.5f : 0.25f;

            if (Status != RobotStatus.Idle)
            {
                return;
            }

            if (_currentPath.Count > 0 && _currentPath.Last() != tile)
            {
                _currentPath.Clear();
            }

            if (_currentPath.Count == 0)
            {
                var approximatePosition = SlamMap.CoarseMap.GetApproximatePosition();
                var robotCurrentPosition = dependOnBrokenBehaviour ? Vector2Int.FloorToInt(approximatePosition) : Vector2Int.RoundToInt(approximatePosition);
                if (robotCurrentPosition == tile)
                {
                    return;
                }

                var pathList = SlamMap.CoarseMap.GetPath(tile, beOptimistic: true, dependOnBrokenBehavior: dependOnBrokenBehaviour);
                if (pathList == null)
                {
                    return;
                }

                _currentPath = new Queue<Vector2Int>(pathList);
                _currentTarget = _currentPath.Dequeue();
            }
            if (SlamMap.CoarseMap.GetTileStatus(_currentTarget, optimistic: true) == SlamMap.SlamTileStatus.Solid)
            {
                _currentTarget = _currentPath.Dequeue();
            }

            var relativePosition = SlamMap.CoarseMap.GetTileCenterRelativePosition(_currentTarget, dependOnBrokenBehaviour: dependOnBrokenBehaviour);
            if (relativePosition.Distance < closeness)
            {
                if (_currentPath.Count == 0)
                {
                    return;
                }

                _currentTarget = _currentPath.Dequeue();
                relativePosition = SlamMap.CoarseMap.GetTileCenterRelativePosition(_currentTarget, dependOnBrokenBehaviour: dependOnBrokenBehaviour);
            }
            if (Math.Abs(relativePosition.RelativeAngle) > 1.5f)
            {
                Rotate(relativePosition.RelativeAngle);
            }
            else if (relativePosition.Distance > closeness)
            {
                Move(relativePosition.Distance);
            }
        }

        /// <inheritdoc/>
        public int? EstimateTimeToTarget(Vector2Int target, bool acceptPartialPaths = false, bool beOptimistic = true, bool dependOnBrokenBehaviour = true)
        {
            var approxPosition = SlamMap.CoarseMap.GetApproximatePosition();
            var position = Vector2Int.FloorToInt(approxPosition);
            return TravelEstimator.EstimateTime(position, target, acceptPartialPaths, beOptimistic, dependOnBrokenBehaviour);
        }

        /// <inheritdoc/>
        public int? OverEstimateTimeToTarget(Vector2Int target, bool acceptPartialPaths = false, bool beOptimistic = true, bool dependOnBrokenBehaviour = true)
        {
            var approxPosition = SlamMap.CoarseMap.GetApproximatePosition();
            var position = Vector2Int.FloorToInt(approxPosition);
            return TravelEstimator.OverEstimateTime(position, target, acceptPartialPaths, beOptimistic, dependOnBrokenBehaviour);
        }

        /// <inheritdoc/>
        public float? EstimateDistanceToTarget(Vector2Int target, bool acceptPartialPaths = false, bool beOptimistic = true, bool dependOnBrokenBehaviour = true)
        {
            var approxPosition = SlamMap.CoarseMap.GetApproximatePosition();
            var position = Vector2Int.FloorToInt(approxPosition);
            return TravelEstimator.EstimateDistance(position, target, acceptPartialPaths, beOptimistic, dependOnBrokenBehaviour);
        }

        /// <summary>
        /// Rotates and moves directly to target unless already moving or already on target
        /// </summary>
        /// <param name="target">COARSEGRAINED tile to move to</param>
        public void MoveTo(Vector2Int target)
        {
            var relativePosition = SlamMap.CoarseMap.GetTileCenterRelativePosition(target);
            if (Status != RobotStatus.Idle || relativePosition.Distance < 0.5f)
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
            return CommunicationManager.SenseNearbyRobots(_robot.id).ToArray();
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