using Maes.Statistics;

namespace Maes.Map.Visualization
{
    public class WaypointHeatMapVisualizationMode : IPatrollingVisualizationMode
    {
        public void UpdateVisualization(PatrollingVisualizer visualizer, int currentTick)
        {
            visualizer.ShowWaypointHeatMap(currentTick);
        }
    }
}