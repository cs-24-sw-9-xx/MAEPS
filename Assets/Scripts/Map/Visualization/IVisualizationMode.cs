using Maes.Statistics;
using Maes.Visualizer;

namespace Maes.Map.Visualization
{
    public interface IVisualizationMode<TCell, in TVisualizer>
        where TCell : ICell
        where TVisualizer : IVisualizer<TCell>
    {
        public void UpdateVisualization(TVisualizer visualizer, int currentTick);
    }
}