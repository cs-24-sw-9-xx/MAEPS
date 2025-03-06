namespace Maes.UI.Visualizers.Patrolling.VisualizationModes
{
    public class WaypointHeatMapVisualizationMode : IPatrollingVisualizationMode
    {
        public void UpdateVisualization(PatrollingVisualizer visualizer, int currentTick)
        {
            visualizer.ShowWaypointHeatMap(currentTick);
        }
    }
}