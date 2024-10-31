using System.Collections.Generic;
using Maes;
using Maes.UI;
using UnityEngine;

namespace MAES.Simulation
{
    public class ExplorationSimulator : Simulator<ExplorationSimulation>
    {
        protected override SimulationManager<ExplorationSimulation> AddSimulationManager(GameObject gameObject)
        {
            return gameObject.AddComponent<ExplorationSimulationManager>();
        }

        public static ExplorationSimulator GetInstance() {
            return (ExplorationSimulator) (_instance ??= new ExplorationSimulator());
        }
        
        /// <summary>
        /// This method is used to start the simulation in a predefined configuration that will change depending on	
        /// whether the simulation is in ros mode or not.	
        /// </summary>	
        public void DefaultStart(bool isRosMode = false) {	
            GlobalSettings.IsRosMode = isRosMode;	
            IEnumerable<SimulationScenario<ExplorationSimulation>> generatedScenarios;	
            if (GlobalSettings.IsRosMode) {	
                generatedScenarios = ExplorationScenarioGenerator.GenerateROS2Scenario();	
            } else {	
                generatedScenarios = ExplorationScenarioGenerator.GenerateYoutubeVideoScenarios();	
            }	
            EnqueueScenarios(generatedScenarios);	
            if (Application.isBatchMode) {	
                _simulationManager.AttemptSetPlayState(SimulationPlayState.FastAsPossible);	
            }	
        }
    }
}