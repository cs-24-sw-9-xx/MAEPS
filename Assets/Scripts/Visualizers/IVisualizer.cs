using Maes.Map;

using UnityEngine;

namespace Maes.Visualizer
{
    public interface IVisualizer<TVisualizerTile>
    {
        void SetSimulationMap(SimulationMap<TVisualizerTile> newMap, Vector3 offset);
    }
}