using System.Collections.Generic;

using Maes.Robot;

namespace Maes.UI.Visualizers.Patrolling.VisualizationModes
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
            visualizer.ShowRobotsHighlighting(Robots);
        }
    }
}