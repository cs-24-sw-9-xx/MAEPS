using Maes.Map;
using Maes.Statistics;

namespace Maes.UI.Visualizers
{
    public interface IVisualizer
    {
        void SetSimulationMap(SimulationMap<Cell> newMap);
    }
}