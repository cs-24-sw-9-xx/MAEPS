using System.Collections.Generic;
using Maes.Algorithms;
using Maes.Map;
using Maes.Robot;
using Maes.Simulation.SimulationScenarios;
using Maes.Trackers;
using Maes.UI.SimulationInfoUIControllers;

namespace Maes.Simulation
{
    public interface ISimulation<TSimulation, TAlgorithm, TScenario> : ISimulation
    where TSimulation : class, ISimulation<TSimulation, TAlgorithm, TScenario>
    where TAlgorithm : IAlgorithm
    where TScenario : SimulationScenario<TSimulation, TAlgorithm>
    {
        void SetScenario(TScenario scenario);
        void SetInfoUIController(SimulationInfoUIControllerBase<TSimulation, TAlgorithm, TScenario> infoUIController);
    }
    
    public interface ISimulation : ISimulationUnit
    {
        int SimulatedLogicTicks { get; }
        int SimulatedPhysicsTicks { get; }
        float SimulateTimeSeconds { get; }
        
        ITracker Tracker { get; }
        
        IReadOnlyList<MonaRobot> Robots { get; }
        
        void SetSelectedRobot(MonaRobot? newSelectedRobot);
        
        // TODO: Remove this!
        void SetSelectedTag(VisibleTagInfoHandler? newSelectedTag);
        
        // TODO: Remove this!
        void ClearVisualTags();

        bool HasFinishedSim();

        bool HasSelectedRobot();

        void ShowAllTags();

        void ShowSelectedTags();

        void RenderCommunicationLines();

        void UpdateDebugInfo();
        
        void OnSimulationFinished();
    }
}