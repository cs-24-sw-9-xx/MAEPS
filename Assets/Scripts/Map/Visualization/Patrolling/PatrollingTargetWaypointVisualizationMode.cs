using Maes.PatrollingAlgorithms;
using Maes.Robot;
using Maes.Statistics;

namespace Maes.Map.Visualization.Patrolling
{
    internal class PatrollingTargetWaypointVisualizationMode : IPatrollingVisualizationMode
    {
        private readonly PatrollingAlgorithm _algorithm;

        public PatrollingTargetWaypointVisualizationMode(MonaRobot robot)
        {
            _algorithm = (PatrollingAlgorithm)robot.Algorithm;
        }

        public void UpdateVisualization(PatrollingVisualizer visualizer, int currentTick)
        {
            var targetWaypoint = _algorithm.TargetVertex;

            visualizer.ShowTargetWaypoint(targetWaypoint);
        }
    }
}