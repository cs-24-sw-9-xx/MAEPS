using Maes.Statistics;
using Maes.Visualizers;

namespace Maes.Map.Visualization
{
    public interface IVisualizationMode<TCell, in TVisualizer>
        where TCell : Cell
        where TVisualizer : IVisualizer<TCell>
    {
        public void UpdateVisualization(TVisualizer visualizer, int currentTick);
    }
}