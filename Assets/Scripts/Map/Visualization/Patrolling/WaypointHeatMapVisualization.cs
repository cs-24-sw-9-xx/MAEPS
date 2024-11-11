using Maes.Map.Visualization.Patrolling;
using Maes.Statistics;

namespace Maes.Assets.Scripts.Map.Visualization.Patrolling
{
    public class WaypointHeatMapVisualizationMode : IPatrollingVisualizationMode
    {
        public void UpdateVisualization(PatrollingVisualizer visualizer, int currentTick)
        {
            visualizer.ShowWaypointHeatMap(currentTick);
        }
    }
}