using Maes.Algorithms.Patrolling;
using Maes.Robot;

namespace Maes.UI.Visualizers.Patrolling.VisualizationModes
{
    internal class PatrollingTargetWaypointVisualizationMode : IPatrollingVisualizationMode
    {
        private readonly IPatrollingAlgorithm _algorithm;

        public PatrollingTargetWaypointVisualizationMode(MonaRobot robot)
        {
            _algorithm = (IPatrollingAlgorithm)robot.Algorithm;
        }

        public void UpdateVisualization(PatrollingVisualizer visualizer, int currentTick)
        {
            var targetWaypoint = _algorithm.TargetVertex;

            visualizer.ResetWaypointsColor();
            visualizer.ShowTargetWaypoint(targetWaypoint);
        }
    }
}