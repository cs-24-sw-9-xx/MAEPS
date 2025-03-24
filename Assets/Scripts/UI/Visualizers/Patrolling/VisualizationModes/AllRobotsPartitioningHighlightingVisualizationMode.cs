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

using UnityEngine;

namespace Maes.UI.Visualizers.Patrolling.VisualizationModes
{
    public class AllRobotsPartitioningHighlightingVisualizationMode : IPatrollingVisualizationMode
    {
        public AllRobotsPartitioningHighlightingVisualizationMode(List<MonaRobot> robots)
        {
            _robots = robots;
            _robotsPartitionVertexId = robots.ToDictionary(r => r, _ => new HashSet<int>());
        }

        private readonly List<MonaRobot> _robots;

        private Dictionary<MonaRobot, HashSet<int>> _robotsPartitionVertexId;

        public void UpdateVisualization(PatrollingVisualizer visualizer, int currentTick)
        {
            var changedSinceLastUpdate = false;

            if (_robots.Count != _robotsPartitionVertexId.Keys.Count)
            {
                _robotsPartitionVertexId = _robots.ToDictionary(r => r, r => r.Algorithm is IPartitionPatrollingAlgorithm alg ? alg.GetPartitionedVertices() : new HashSet<int>());
                changedSinceLastUpdate = true;
            }
            else
            {
                foreach (var robot in _robots)
                {
                    if (robot.Algorithm is not IPartitionPatrollingAlgorithm alg)
                    {
                        Debug.Log("Must be a partitioning algorithm to be able to use this feature");
                        continue;
                    }

                    var verticesId = alg.GetPartitionedVertices();
                    if (verticesId.SetEquals(_robotsPartitionVertexId[robot]))
                    {
                        continue;
                    }

                    changedSinceLastUpdate = true;
                    _robotsPartitionVertexId[robot] = verticesId;
                }
            }

            if (!changedSinceLastUpdate)
            {
                return;
            }

            var verticesColors = _robotsPartitionVertexId
                .SelectMany(robotPartition =>
                {
                    var (robot, partitionVertex) = robotPartition;
                    return partitionVertex.Select(id => (vertexId: id, color: robot.Color));
                }).Distinct()
                .GroupBy(vertexColor => vertexColor.vertexId, vertexColor => vertexColor.color)
                .ToDictionary(group => group.Key, group => group.ToArray());

            foreach (var (vertexId, vertexVisualizer) in visualizer.VertexVisualizers)
            {
                if (verticesColors.TryGetValue(vertexId, out var colors))
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