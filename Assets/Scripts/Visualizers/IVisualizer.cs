using Maes.Map;
using Maes.Statistics;

using UnityEngine;

namespace Maes.Visualizers
{
    public interface IVisualizer
    {
        void SetSimulationMap(SimulationMap<Cell> newMap, Vector3 offset);
    }
}