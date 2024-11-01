using JetBrains.Annotations;

using MAES.Simulation;

using UnityEngine;

namespace MAES.UI.SimulationInfoUIControllers
{
    public interface ISimulationInfoUIController
    {
        void NotifyNewSimulation([CanBeNull] ISimulation simulation);

        void UpdateStatistics([CanBeNull] ISimulation simulation);

        void UpdateMouseCoordinates(Vector2 mousePosition);
    }
}