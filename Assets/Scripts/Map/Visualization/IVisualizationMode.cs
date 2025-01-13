using Maes.Visualizers;

namespace Maes.Map.Visualization
{
    public interface IVisualizationMode<in TVisualizer>
        where TVisualizer : IVisualizer
    {
        public void UpdateVisualization(TVisualizer visualizer, int currentTick);
    }
}