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

using System.Collections.Generic;
using System.IO;

using Maes.Algorithms;
using Maes.Robot;

using UnityEngine;

namespace Maes.ExplorationAlgorithm
{
    internal class CircleTestAlgorithm : IExplorationAlgorithm
    {
        // Set by SetController
        private Robot2DController _controller = null!;

        private readonly List<Vector2Int> _points = new();
        private readonly float _leftForce, _rightForce;
        private bool _turning;
        private int _ticks;
        private bool _shouldEnd;

        public CircleTestAlgorithm(float leftForce, float rightForce)
        {
            _leftForce = leftForce;
            _rightForce = rightForce;
        }

        public string GetDebugInfo()
        {
            // This is used to end the simulation
            return _shouldEnd.ToString();
        }

        public void SetController(Robot2DController controller)
        {
            _controller = controller;
        }

        public void UpdateLogic()
        {
            // Don't collect data if dragging against the wall
            if (_controller.IsCurrentlyColliding())
            {
                _controller.StopCurrentTask();
                _shouldEnd = true;
            }

            _ticks++;
            var position = _controller.SlamMap.GetCurrentPosition();
            // Simple state machine to not set a new task constantly
            if (!_turning)
            {
                Debug.Log($"left: {_leftForce}, right: {_rightForce}");
                _controller.SetWheelForceFactors(_leftForce, _rightForce);
                _turning = true;
                _points.Add(position);
            }
            // Add a point if we have created a half circle assuming it starts at y=0
            if (_controller.Transform.position.y < 0)
            {
                _points.Add(position);
            }
            // Store circle in csv when we have the start point and the half circle point
            if (_points.Count == 2)
            {
                var radius = Vector2Int.Distance(_points[0], _points[1]) / 2;
                var distanceBetweenWheels = Vector2.Distance(_controller.LeftWheel.position, _controller.RightWheel.position);
                var innerRadius = radius - distanceBetweenWheels / 2;
                var outerRadius = radius + distanceBetweenWheels / 2;
                var ratioBetweenRadii = innerRadius / outerRadius;
                if (!File.Exists($@"circle_data.csv"))
                {
                    using var file = File.AppendText($@"circle_data.csv");
                    file.WriteLine($"radius;ticks;leftForce;rightForce;distance;ratio");
                }

                using (var file = File.AppendText($@"circle_data.csv"))
                {
                    file.WriteLine($"{radius};{_ticks * 2};{_leftForce};{_rightForce};{distanceBetweenWheels};{ratioBetweenRadii}");
                }
                _controller.StopCurrentTask();
                _shouldEnd = true;
            }
        }
    }
}