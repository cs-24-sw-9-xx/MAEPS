using System.Collections.Generic;

using Maes.Algorithms.Exploration;
using Maes.UI;

using UnityEngine;

namespace Maes.Simulation.Exploration
{
    public class ExplorationSimulator : Simulator<ExplorationSimulation, IExplorationAlgorithm, ExplorationSimulationScenario>
    {
        public ExplorationSimulator(bool autoMaxSpeedInBatchMode = true) : base(autoMaxSpeedInBatchMode) { }
        protected override GameObject LoadSimulatorGameObject()
        {
            return Resources.Load<GameObject>("Exploration_MAEPS");
        }

        /// <summary>
        /// This method is used to start the simulation in a predefined configuration that will change depending on	
        /// whether the simulation is in ros mode or not.	
        /// </summary>	
        public void DefaultStart(bool isRosMode = false)
        {
            GlobalSettings.IsRosMode = isRosMode;
            IEnumerable<ExplorationSimulationScenario> generatedScenarios;
            if (GlobalSettings.IsRosMode)
            {
                generatedScenarios = ExplorationScenarioGenerator.GenerateROS2Scenario();
            }
            else
            {
                generatedScenarios = ExplorationScenarioGenerator.GenerateYoutubeVideoScenarios();
            }
            EnqueueScenarios(generatedScenarios);
            if (Application.isBatchMode)
            {
                SimulationManager.AttemptSetPlayState(SimulationPlayState.FastAsPossible);
            }
        }

    }
}