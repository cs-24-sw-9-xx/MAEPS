using System;
using System.Collections.Generic;
using System.Text;

using Maes.Robot;
using Maes.Robot.Task;

using UnityEngine;

namespace Maes.Algorithms.Exploration.WallFollower
{
    public class WallFollowerAlgorithm : IExplorationAlgorithm
    {
        // Set by SetController
        private Robot2DController _controller = null!;

        private bool _hasTurnedLeft;

        private const float GridSpacing = 1.8f;

        private float Angle => _controller.GetGlobalAngle();

        private bool _forwardWall;
        private bool _leftWall;

        private bool _rightWall;
        private bool _behindWall;

        private Direction _direction = Direction.North;

        private float TargetAngle => DirectionToAngle(_direction);

        private bool _collided;

        private const float North = 90.0f;
        private const float East = 0.0f;
        private const float South = 270.0f;
        private const float West = 180.0f;

        private Vector2Int _targetPosition;

        private enum Direction
        {
            North,
            East,
            South,
            West,
            End
        }


        public string GetDebugInfo()
        {
            return
                new StringBuilder().Append("Status: ")
                    .Append(_controller.GetStatus())
                    .Append("\nHasTurnedLeft: ")
                    .Append(_hasTurnedLeft)
                    .Append("\nWalls: ")
                    .Append(_forwardWall ? 'F' : '_')
                    .Append(_leftWall ? 'L' : '_')
                    .Append(_behindWall ? 'B' : '_')
                    .Append(_rightWall ? 'R' : '_')
                    .Append("\nAngle: ")
                    .Append(Angle)
                    .Append(" Target: ")
                    .Append(TargetAngle)
                    .Append("\nCollided: ")
                    .Append(_collided)
                    .Append("\nPos: ")
                    .Append(_controller.GetSlamMap().GetCoarseMap().GetCurrentPosition())
                    .Append("\"")
                    .ToString();
        }

        public void SetController(Robot2DController controller)
        {
            _controller = controller;
        }

        public IEnumerable<WaitForCondition> PreUpdateLogic()
        {
            _targetPosition = _controller.GetSlamMap().GetCoarseMap().GetCurrentPosition();

            while (true)
            {
                const float epsilon = 0.5f;

                while (_targetPosition != _controller.GetSlamMap().GetCoarseMap().GetCurrentPosition())
                {
                    _controller.MoveTo(_targetPosition);
                    yield return WaitForCondition.WaitForRobotStatus(RobotStatus.Idle);
                }

                for (var testAngle = WrapAngle(Angle - TargetAngle); testAngle is < -epsilon or > epsilon; testAngle = WrapAngle(Angle - TargetAngle))
                {
                    var rotateAngle = -WrapAngle(Angle - TargetAngle);
                    _controller.Rotate(rotateAngle);
                    yield return WaitForCondition.WaitForRobotStatus(RobotStatus.Idle);
                }

                yield return WaitForCondition.ContinueUpdateLogic();
            }
        }

        public IEnumerable<WaitForCondition> UpdateLogic()
        {
            _targetPosition = _controller.GetSlamMap().GetCoarseMap().GetCurrentPosition();

            // Before anything else find and hug the west wall
            while (true)
            {
                // Turn west
                _direction = Direction.West;

                // Go to the wall
                if (IsWallForward())
                {
                    // Look north we are ready to go :)
                    _direction = Direction.North;
                    yield return WaitForCondition.WaitForLogicTicks(1);
                    break;
                }
                else
                {
                    GoForward();
                    yield return WaitForCondition.WaitForLogicTicks(1);
                }
            }

            while (true)
            {
                if (_controller.IsCurrentlyColliding && !_collided)
                {
                    _collided = true;
                    _targetPosition -= DirectionToPosition(_direction);
                    _controller.StopCurrentTask();
                }

                _leftWall = IsWallLeft();
                _forwardWall = IsWallForward();
                _behindWall = IsWallBehind();
                _rightWall = IsWallRight();


                // Implements the Wall Follower 1 pseudocode from https://blogs.ntu.edu.sg/scemdp-201718s1-g14/exploration-algorithm/

                if (_hasTurnedLeft && (!_forwardWall && !_collided))
                {
                    GoForward();
                    _hasTurnedLeft = false;
                    _collided = false;
                    yield return WaitForCondition.WaitForLogicTicks(1);
                    continue;
                }

                if (!IsWallLeft())
                {
                    TurnLeft();
                    _hasTurnedLeft = true;
                    _collided = false;
                    yield return WaitForCondition.WaitForLogicTicks(1);
                    continue;
                }

                if ((!_forwardWall && !_collided))
                {
                    GoForward();
                    _hasTurnedLeft = false;
                    _collided = false;
                    yield return WaitForCondition.WaitForLogicTicks(1);
                    continue;
                }

                TurnRight();
                _hasTurnedLeft = false;
                _collided = false;

                yield return WaitForCondition.WaitForLogicTicks(1);
            }

            void TurnLeft()
            {
                Debug.Log("TurnLeft");

                _direction = (Direction)Mod((int)_direction - 1, (int)Direction.End);
            }

            void TurnRight()
            {
                Debug.Log("TurnRight");

                _direction = (Direction)Mod((int)_direction + 1, (int)Direction.End);
            }

            void GoForward()
            {
                Debug.Log("Forward");

                _targetPosition += DirectionToPosition(_direction);
            }
        }

        private float GetLeftGlobalAngle()
        {
            return ((_controller.GetGlobalAngle() + 90.0f) + 360.0f) % 360.0f;
        }

        private float GetRightGlobalAngle()
        {
            return (_controller.GetGlobalAngle() - 90.0f + 360.0f) % 360.0f;
        }

        private float GetForwardGlobalAngle()
        {
            return _controller.GetGlobalAngle();
        }

        private static float DirectionToAngle(Direction direction)
        {
            return direction switch
            {
                Direction.North => North,
                Direction.East => East,
                Direction.South => South,
                Direction.West => West,
                _ => throw new InvalidOperationException($"INVALID DIRECTION: {direction}")
            };
        }

        private static Vector2Int DirectionToPosition(Direction direction)
        {
            return direction switch
            {
                Direction.North => Vector2Int.up,
                Direction.East => Vector2Int.right,
                Direction.South => Vector2Int.down,
                Direction.West => Vector2Int.left,
                _ => throw new InvalidOperationException($"INVALID DIRECTION: {direction}")
            };
        }


        private bool IsWallForward()
        {
            var wall = _controller.DetectWall(GetForwardGlobalAngle());
            if (!wall.HasValue)
            {
                return false;
            }

            return wall.Value.Distance <= GridSpacing;
        }

        private bool IsWallLeft()
        {
            var wall = _controller.DetectWall(GetLeftGlobalAngle());
            if (!wall.HasValue)
            {
                return false;
            }

            return wall.Value.Distance <= GridSpacing;
        }

        private bool IsWallRight()
        {
            var wall = _controller.DetectWall(GetRightGlobalAngle());
            if (!wall.HasValue)
            {
                return false;
            }

            return wall.Value.Distance <= GridSpacing;
        }

        private bool IsWallBehind()
        {
            var wall = _controller.DetectWall((GetForwardGlobalAngle() + 180.0f) % 360.0f);
            if (!wall.HasValue)
            {
                return false;
            }

            return wall.Value.Distance <= GridSpacing;
        }

        private static int Mod(int x, int m)
        {
            return (x % m + m) % m;
        }

        private static float WrapAngle(float angle)
        {
            angle %= 360;
            if (angle > 180)
            {
                return angle - 360;
            }

            if (angle < -180)
            {
                return angle + 360;
            }

            return angle;
        }
    }
}