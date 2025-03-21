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