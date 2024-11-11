using System;

using Maes.Algorithms;
using Maes.Robot;

using UnityEngine;

namespace Maes.ExplorationAlgorithm.WallFollower
{
    public class WallFollowerAlgorithm : IExplorationAlgorithm
    {
        // Set by SetController
        private Robot2DController _controller = null!;

        private bool _hasTurnedLeft;

        private const float GridSpacing = 1.8f;

        private float _angle;

        private bool _forwardWall;
        private bool _leftWall;

        private bool _rightWall;
        private bool _behindWall;

        private float TargetAngle => DirectionToAngle(_direction);

        private bool _collided;

        private bool _positionSet;
        private Vector2Int _targetPosition;

        private const float North = 90.0f;
        private const float East = 0.0f;
        private const float South = 270.0f;
        private const float West = 180.0f;

        private Direction _direction = Direction.North;

        private bool _initial = true;


        enum Direction
        {
            North,
            East,
            South,
            West,
            End,
        }


        public string GetDebugInfo()
        {
            return
                $"Status: {_controller.GetStatus()}\n" +
                $"HasTurnedLeft: {_hasTurnedLeft}\n" +
                $"Walls: {(_forwardWall ? 'F' : '_')}{(_leftWall ? 'L' : '_')}{(_behindWall ? 'B' : '_')}{(_rightWall ? 'R' : '_')}\n" +
                $"Angle: {_angle} Target: {TargetAngle}\n" +
                $"Collided: {_collided}\n" +
                $"Pos: {_controller.GetSlamMap().GetCoarseMap().GetCurrentPosition()}\n" +
                $"TPos: {_targetPosition} Dir: {_direction}"
                ;
        }

        public void SetController(Robot2DController controller)
        {
            _controller = controller;
        }

        public void UpdateLogic()
        {
            _angle = _controller.GetGlobalAngle();

            if (!_positionSet)
            {
                _targetPosition = _controller.GetSlamMap().GetCoarseMap().GetCurrentPosition();
                _positionSet = true;
            }

            if (_controller.IsCurrentlyColliding() && !_collided)
            {
                _collided = true;
                _targetPosition -= DirectionToPosition(_direction);
                _controller.StopCurrentTask();
            }

            if (_controller.IsRotating() || _controller.GetStatus() != Robot.Task.RobotStatus.Idle)
            {
                return;
            }


            _leftWall = IsWallLeft();
            _forwardWall = IsWallForward();
            _behindWall = IsWallBehind();
            _rightWall = IsWallRight();

            var epsilon = 0.5;

            if (_targetPosition != _controller.GetSlamMap().GetCoarseMap().GetCurrentPosition())
            {
                _controller.MoveTo(_targetPosition);
                return;
            }

            var testAngle = WrapAngle(_angle - TargetAngle);
            if (testAngle < -epsilon || testAngle > epsilon)
            {
                var rotateAngle = -WrapAngle(_angle - TargetAngle);
                Debug.Log($"angle: {_angle} target: {TargetAngle} rotating: {rotateAngle}");
                _controller.Rotate(rotateAngle);
                return;
            }

            // Before anything else find and hug the west wall
            if (_initial)
            {
                // Turn west
                _direction = Direction.West;

                // Go to the wall
                if (IsWallForward())
                {
                    // Look north we are ready to go :)
                    _direction = Direction.North;
                    _initial = false;
                }
                else
                {
                    GoForward();
                }

                return;
            }


            // Implements the Wall Follower 1 pseudocode from https://blogs.ntu.edu.sg/scemdp-201718s1-g14/exploration-algorithm/

            if (_hasTurnedLeft && (!_forwardWall && !_collided))
            {
                GoForward();
                _hasTurnedLeft = false;
                _collided = false;
                return;
            }

            if (!IsWallLeft())
            {
                TurnLeft();
                _hasTurnedLeft = true;
                _collided = false;
                return;
            }

            if ((!_forwardWall && !_collided))
            {
                GoForward();
                _hasTurnedLeft = false;
                _collided = false;
                return;
            }

            TurnRight();
            _hasTurnedLeft = false;
            _collided = false;
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

        private float DirectionToAngle(Direction direction)
        {
            return direction switch
            {
                Direction.North => North,
                Direction.East => East,
                Direction.South => South,
                Direction.West => West,
                _ => throw new InvalidOperationException($"INVALID DIRECTION: {direction}"),
            };
        }

        private Vector2Int DirectionToPosition(Direction direction)
        {
            return direction switch
            {
                Direction.North => Vector2Int.up,
                Direction.East => Vector2Int.right,
                Direction.South => Vector2Int.down,
                Direction.West => Vector2Int.left,
                _ => throw new InvalidOperationException($"INVALID DIRECTION: {direction}"),
            };
        }

        private void TurnLeft()
        {
            Debug.Log("TurnLeft");

            _direction = (Direction)Mod((int)_direction - 1, (int)Direction.End);
        }

        private void TurnRight()
        {
            Debug.Log("TurnRight");

            _direction = (Direction)Mod((int)_direction + 1, (int)Direction.End);
        }

        private void GoForward()
        {
            Debug.Log("Forward");

            _targetPosition += DirectionToPosition(_direction);
        }

        private bool IsWallForward()
        {
            var wall = _controller.DetectWall(GetForwardGlobalAngle());
            if (!wall.HasValue)
            {
                return false;
            }
            return wall.Value.distance <= GridSpacing;
        }

        private bool IsWallLeft()
        {
            var wall = _controller.DetectWall(GetLeftGlobalAngle());
            if (!wall.HasValue)
            {
                return false;
            }
            return wall.Value.distance <= GridSpacing;
        }

        private bool IsWallRight()
        {
            var wall = _controller.DetectWall(GetRightGlobalAngle());
            if (!wall.HasValue)
            {
                return false;
            }
            return wall.Value.distance <= GridSpacing;
        }

        private bool IsWallBehind()
        {
            var wall = _controller.DetectWall((GetForwardGlobalAngle() + 180.0f) % 360.0f);
            if (!wall.HasValue)
            {
                return false;
            }
            return wall.Value.distance <= GridSpacing;
        }

        private static int Mod(int x, int m)
        {
            return (x % m + m) % m;
        }

        private static float WrapAngle(float angle)
        {
            angle %= 360;
            if (angle > 180)
                return angle - 360;

            if (angle < -180)
                return angle + 360;

            return angle;
        }
    }
}