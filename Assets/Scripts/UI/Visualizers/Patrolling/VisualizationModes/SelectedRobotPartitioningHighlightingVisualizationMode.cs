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

using UnityEngine;

namespace Maes.UI.Visualizers.Patrolling.VisualizationModes
{
    public class SelectedRobotPartitioningHighlightingVisualizationMode : IPatrollingVisualizationMode
    {
        public SelectedRobotPartitioningHighlightingVisualizationMode(MonaRobot robot)
        {
            _robot = robot;
            _color = robot.Color;
        }

        private readonly MonaRobot _robot;
        private readonly Color32 _color;

        private HashSet<int> _partitionedVertices = new();

        public void UpdateVisualization(PatrollingVisualizer visualizer, int currentTick)
        {
            if (_robot.Algorithm is not IPartitionPatrollingAlgorithm alg)
            {
                Debug.Log("Selected robot does not have a partitioning algorithm.");
                return;
            }

            var partitionedVertices = alg.GetPartitionedVertices();

            if (partitionedVertices.SetEquals(_partitionedVertices))
            {
                return;
            }

            foreach (var (id, vertexVisualizer) in visualizer.VertexVisualizers)
            {
                if (partitionedVertices.Contains(id))
                {
                    vertexVisualizer.SetWaypointColor(_color);
                }
                else
                {
                    vertexVisualizer.SetWaypointColor(Color.black);
                }
            }

            _partitionedVertices = partitionedVertices;
            _robot.ShowOutline();
        }
    }
}