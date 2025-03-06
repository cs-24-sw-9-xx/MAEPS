namespace Maes.UI.Visualizers
{
    public interface IVisualizationMode<in TVisualizer>
        where TVisualizer : IVisualizer
    {
        public void UpdateVisualization(TVisualizer visualizer, int currentTick);
    }
}