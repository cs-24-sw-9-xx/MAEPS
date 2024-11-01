using System.Collections.Generic;
using Maes;
using Maes.Algorithms;

using MAES.Simulation.SimulationScenarios;

using Maes.UI;
using UnityEngine;
using UnityEngine.UI;

namespace MAES.Simulation
{
    public class ExplorationSimulator : Simulator<ExplorationSimulation, IExplorationAlgorithm, ExplorationSimulationScenario>
    {
        private static ExplorationSimulator _instance = null;
        
        protected override GameObject LoadSimulatorGameObject()
        {
            return Resources.Load<GameObject>("Exploration_MAES");
        }
        
        public static ExplorationSimulator GetInstance() {
            if (_instance == null)
            {
                _instance = new ExplorationSimulator();
            }

            return _instance;
        }
        
        // Clears the singleton instance and removes the simulator game object
        public static void Destroy() {
            _instance.DestroyMe();
            _instance = null;
        }
        
        /// <summary>
        /// This method is used to start the simulation in a predefined configuration that will change depending on	
        /// whether the simulation is in ros mode or not.	
        /// </summary>	
        public void DefaultStart(bool isRosMode = false) {	
            GlobalSettings.IsRosMode = isRosMode;	
            IEnumerable<ExplorationSimulationScenario> generatedScenarios;	
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