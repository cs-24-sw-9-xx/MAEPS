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
using System.Linq;

using Maes.Algorithms.Patrolling;
using Maes.Robot;
using Maes.Utilities;

using UnityEngine;

namespace Maes.UI.Visualizers.Patrolling.VisualizationModes
{
    public class AllRobotsShowVerticesColorsVisualizationMode : IPatrollingVisualizationMode
    {
        public AllRobotsShowVerticesColorsVisualizationMode(List<MonaRobot> robots)
        {
            _robots = robots;
        }

        private readonly List<MonaRobot> _robots;

        private Dictionary<int, Dictionary<int, Color32[]>> _vertexColorsByRobotId = new();

        public void UpdateVisualization(PatrollingVisualizer visualizer, int currentTick)
        {
            var changedSinceLastUpdate = false;

            if (_robots.Count != _vertexColorsByRobotId.Keys.Count) // If the number of robots has changed, we need to update the colors
            {
                _vertexColorsByRobotId = _robots.ToDictionary(robot => robot.id,
                                                              robot => ((IPatrollingAlgorithm)robot.Algorithm).ColorsByVertexId.ToDictionary(kv => kv.Key, kv => kv.Value));

                changedSinceLastUpdate = true;
            }
            else
            {
                foreach (var robot in _robots)
                {
                    var alg = (IPatrollingAlgorithm)robot.Algorithm;

                    var vertexColors = alg.ColorsByVertexId;
                    if (_vertexColorsByRobotId.TryGetValue(robot.id, out var colors))
                    {
                        if (vertexColors.SetEquals(colors))
                        {
                            continue;
                        }

                        _vertexColorsByRobotId[robot.id] = vertexColors.ToDictionary(kv => kv.Key, kv => kv.Value);
                    }
                    else
                    {
                        _vertexColorsByRobotId.Add(robot.id, vertexColors.ToDictionary(kv => kv.Key, kv => kv.Value));
                    }

                    changedSinceLastUpdate = true;
                }
            }

            if (!changedSinceLastUpdate)
            {
                return;
            }

            var colorsByVertexId = _vertexColorsByRobotId
                .SelectMany(robotVertexColorsPair => robotVertexColorsPair.Value)
                .GroupBy(vertexIdColorsPair => vertexIdColorsPair.Key, vertexIdColorsPair => vertexIdColorsPair.Value)
                .ToDictionary(colorsByVertexIdGroup => colorsByVertexIdGroup.Key,
                    colorsByVertexIdGroup => colorsByVertexIdGroup.SelectMany(colors => colors).ToArray());

            foreach (var (vertexId, vertexVisualizer) in visualizer.VertexVisualizers)
            {
                if (colorsByVertexId.TryGetValue(vertexId, out var colors))
                {
                    vertexVisualizer.SetWaypointColor(colors);
                }
                else
                {
                    vertexVisualizer.SetWaypointColor(Color.black);
                }
            }

            foreach (var robot in _robots)
            {
                robot.ShowOutline();
            }
        }
    }
}