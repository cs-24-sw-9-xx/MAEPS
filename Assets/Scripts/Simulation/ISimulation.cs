using System.Collections.Generic;
using JetBrains.Annotations;
using Maes;
using Maes.Map;
using Maes.Robot;
using Maes.Trackers;
using Maes.UI;
using UnityEngine;

namespace MAES.Simulation
{
    public interface ISimulation<TSimulation> : ISimulation
    where TSimulation : ISimulation<TSimulation>
    {
        void SetScenario(SimulationScenario<TSimulation> scenario);
    }
    public interface ISimulation : ISimulationUnit
    {
        int SimulatedLogicTicks { get; }
        int SimulatedPhysicsTicks { get; }
        float SimulateTimeSeconds { get; }

        ITracker Tracker { get; }

        ISimulationInfoUIController AddSimulationInfoUIController(GameObject gameObject);

        IReadOnlyList<MonaRobot> Robots { get; }

        void SetSelectedRobot([CanBeNull] MonaRobot newSelectedRobot);

        // TODO: Remove this!
        void SetSelectedTag([CanBeNull] VisibleTagInfoHandler newSelectedTag);

        // TODO: Remove this!
        void ClearVisualTags();

        bool HasFinishedSim();

        bool HasSelectedRobot();

        void ShowAllTags();

        void ShowSelectedTags();

        void RenderCommunicationLines();

        void UpdateDebugInfo();

        void OnSimulationFinished();

        void OnDestory();
    }
}