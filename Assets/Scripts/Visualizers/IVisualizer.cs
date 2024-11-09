using Maes.Map;
using Maes.Statistics;

using UnityEngine;

namespace Maes.Visualizers
{
    public interface IVisualizer<TVisualizerTile>
    where TVisualizerTile : ICell
    {
        void SetSimulationMap(SimulationMap<TVisualizerTile> newMap, Vector3 offset);
    }
}