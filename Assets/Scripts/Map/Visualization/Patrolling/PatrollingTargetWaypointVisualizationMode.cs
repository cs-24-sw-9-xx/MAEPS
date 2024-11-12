using Maes.PatrollingAlgorithms;
using Maes.Robot;
using Maes.Statistics;

namespace Maes.Map.Visualization.Patrolling
{
    internal class PatrollingTargetWaypointVisualizationMode : IPatrollingVisualizationMode
    {
        private readonly MonaRobot _robot;

        public PatrollingTargetWaypointVisualizationMode(MonaRobot robot)
        {
            _robot = robot;
        }
        public void UpdateVisualization(PatrollingVisualizer visualizer, int currentTick)
        {
            var algorithm = (PatrollingAlgorithm)_robot.Algorithm;
            var targetWaypoint = algorithm.TargetVertex;
            if (targetWaypoint == null) return;
            visualizer.ShowTargetWaypoint(targetWaypoint);
        }
    }
}