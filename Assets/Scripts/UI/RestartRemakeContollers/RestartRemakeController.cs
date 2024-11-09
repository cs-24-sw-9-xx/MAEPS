// Copyright 2024 MAES
// 
// This file is part of MAES
// 
// MAES is free software: you can redistribute it and/or modify it under
// the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your option)
// any later version.
// 
// MAES is distributed in the hope that it will be useful, but WITHOUT
// ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
// or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General
// Public License for more details.
// 
// You should have received a copy of the GNU General Public License along
// with MAES. If not, see http://www.gnu.org/licenses/.
// 
// Contributors: Rasmus Borrisholt Schmidt, Andreas Sebastian Sørensen, Thor Beregaard
// 
// Original repository: https://github.com/Molitany/MAES

using System.Collections.Generic;
using Maes.Algorithms;
using Maes.Simulation;
using Maes.Simulation.SimulationScenarios;

using UnityEngine;
using UnityEngine.UI;

namespace Maes.UI.RestartRemakeContollers
{
    public abstract class RestartRemakeController<TSimulation, TAlgorithm, TScenario> : MonoBehaviour
        where TSimulation : class, ISimulation<TSimulation, TAlgorithm, TScenario>
        where TAlgorithm : IAlgorithm
        where TScenario : SimulationScenario<TSimulation, TAlgorithm>
    {

        public Button RestartCurrentButton = null!;
        public Button RestartAllButton = null!;
        public Button MakeAndRunButton = null!;
        public Button CreateBatchButton = null!;
        public SimulationManager<TSimulation, TAlgorithm, TScenario> simulationManager = null!;


        // Start is called before the first frame update
        void Start()
        {
            RestartCurrentButton.onClick.AddListener(RestartCurrentScenario);

            RestartAllButton.onClick.AddListener(RestartAllScenarios);

            MakeAndRunButton.onClick.AddListener(() => {
                Debug.LogWarning("Does nothing");
            });

            CreateBatchButton.onClick.AddListener(() => {
                Debug.LogWarning("Does nothing");
            });
        }

        private void RestartCurrentScenario() {
            if (simulationManager.CurrentScenario == null)
            {
                Debug.LogWarning("There is no current scenario");
                return;
            }
            
            simulationManager.AttemptSetPlayState(SimulationPlayState.Play); //Avoids a crash when restarting during pause
            var newScenariosQueue = new Queue<TScenario>();
            newScenariosQueue.Enqueue(simulationManager.CurrentScenario);
            simulationManager.RemoveCurrentSimulation();
            if (simulationManager.Scenarios.Count != 0)
            {
                while (simulationManager.Scenarios.Count != 0)
                {
                    newScenariosQueue.Enqueue(simulationManager.Scenarios.Dequeue());
                }
            }

            simulationManager.Scenarios = newScenariosQueue;
            
            //Basically adds the same simulation to the front of the queue again
            //Second time it get a crash, for some reason
            
            // TODO: WTF is this comment talking about? ^
        }
        
        private void RestartAllScenarios() {
            simulationManager.AttemptSetPlayState(SimulationPlayState.Play); //Avoids a crash when restarting during pause
            var tempScenariosQueue = new Queue<TScenario>();
            foreach (var scenario in simulationManager.InitialScenarios){
                tempScenariosQueue.Enqueue(scenario);
            }
            simulationManager.RemoveCurrentSimulation();

            simulationManager.Scenarios = tempScenariosQueue;
        }
    }
}
