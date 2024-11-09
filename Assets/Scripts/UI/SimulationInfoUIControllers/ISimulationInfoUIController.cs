using Maes.Simulation;

using UnityEngine;

namespace Maes.UI.SimulationInfoUIControllers
{
    public interface ISimulationInfoUIController
    {
        void NotifyNewSimulation(ISimulation? simulation);

        void UpdateStatistics(ISimulation? simulation);

        void UpdateMouseCoordinates(Vector2 mousePosition);
    }
}