using System.Collections.Generic;

using Maes.Robot;
using Maes.Statistics;

namespace Maes.Map.Visualization.Patrolling
{
    internal class AllRobotsHighlightingVisualizationMode : IPatrollingVisualizationMode
    {
        public readonly IEnumerable<MonaRobot> Robots;
        public AllRobotsHighlightingVisualizationMode(IEnumerable<MonaRobot> robots)
        {
            Robots = robots;
        }
        public void UpdateVisualization(PatrollingVisualizer visualizer, int currentTick)
        {
            visualizer.ShowAllRobotsHighlighting(Robots);
        }
    }
}