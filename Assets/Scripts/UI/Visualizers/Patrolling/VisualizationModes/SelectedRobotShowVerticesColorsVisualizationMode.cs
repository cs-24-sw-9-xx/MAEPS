// Copyright 2025 MAEPS
// 
// This file is part of MAEPS
// 
// MAEPS is free software: you can redistribute it and/or modify it under
// the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your option)
// any later version.
// 
// MAEPS is distributed in the hope that it will be useful, but WITHOUT
// ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
// or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General
// Public License for more details.
// 
// You should have received a copy of the GNU General Public License along
// with MAEPS. If not, see http://www.gnu.org/licenses/.
// 
// Contributors: Puvikaran Santhirasegaram

using System.Collections.Generic;

using Maes.Algorithms.Patrolling;
using Maes.Robot;
using Maes.Utilities;

using UnityEngine;

namespace Maes.UI.Visualizers.Patrolling.VisualizationModes
{
    public class SelectedRobotShowVerticesColorsVisualizationMode : IPatrollingVisualizationMode
    {
        public SelectedRobotShowVerticesColorsVisualizationMode(MonaRobot robot)
        {
            _robot = robot;

        }

        private readonly MonaRobot _robot;

        private Dictionary<int, Color32[]>? _currentColorsByVertexId;

        public void UpdateVisualization(PatrollingVisualizer visualizer, int currentTick)
        {
            var alg = (IPatrollingAlgorithm)_robot.Algorithm;

            var colorsByVertexId = alg.ColorsByVertexId;

            if (_currentColorsByVertexId is not null && colorsByVertexId.SetEquals(_currentColorsByVertexId))
            {
                return;
            }

            foreach (var (id, vertexVisualizer) in visualizer.VertexVisualizers)
            {
                if (colorsByVertexId.TryGetValue(id, out var colors))
                {
                    vertexVisualizer.SetWaypointColor(colors);
                }
                else
                {
                    vertexVisualizer.SetWaypointColor(Color.black);
                }
            }

            _currentColorsByVertexId = colorsByVertexId;
            _robot.ShowOutline();
        }
    }
}