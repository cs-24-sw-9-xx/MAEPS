using Maes.Map;
using Maes.Statistics;

namespace Maes.UI.Visualizers
{
    public interface IVisualizer
    {
#if MAEPS_GUI
        void SetSimulationMap(SimulationMap<Cell> newMap);
#endif
    }
}