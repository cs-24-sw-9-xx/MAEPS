using Maes.Map;
using Maes.Statistics;

namespace Maes.Visualizers
{
    public interface IVisualizer
    {
        void SetSimulationMap(SimulationMap<Cell> newMap);
    }
}