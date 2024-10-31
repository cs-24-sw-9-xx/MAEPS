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
using Maes;
using MAES.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace MAES.UI.RestartRemakeContollers
{
    public abstract class RestartRemakeController<TSimulation> : MonoBehaviour
        where TSimulation : class, ISimulation<TSimulation>
    {

        public Button RestartCurrentButton;
        public Button RestartAllButton;
        public Button MakeAndRunButton;
        public Button CreateBatchButton;
        public SimulationManager<TSimulation> simulationManager;

        private ISimulation _previousSimulationBase;



        // Start is called before the first frame update
        void Start()
        {
            simulationManager = GameObject.Find("SimulationManager").GetComponent<SimulationManager<TSimulation>>();
            RestartCurrentButton = GameObject.Find("RestartCurrentButton").GetComponent<Button>();
            RestartAllButton = GameObject.Find("RestartAllButton").GetComponent<Button>();
            MakeAndRunButton = GameObject.Find("MakeAndRunButton").GetComponent<Button>();
            CreateBatchButton = GameObject.Find("CreateBatchButton").GetComponent<Button>();

            RestartCurrentButton.onClick.AddListener(() =>
            {
                RestartCurrentScenario();
            });

            RestartAllButton.onClick.AddListener(() =>
            {
                RestartAllScenarios();
            });

            MakeAndRunButton.onClick.AddListener(() =>
            {

            });

            CreateBatchButton.onClick.AddListener(() =>
            {

            });
        }

        private void RestartCurrentScenario()
        {
            simulationManager.AttemptSetPlayState(Maes.UI.SimulationPlayState.Play); //Avoids a crash when restarting during pause
            var newScenariosQueue = new Queue<SimulationScenario<TSimulation>>();
            newScenariosQueue.Enqueue(simulationManager._currentScenario);
            simulationManager.RemoveCurrentSimulation();
            if (simulationManager._scenarios.Count != 0)
            {
                while (simulationManager._scenarios.Count != 0)
                {
                    newScenariosQueue.Enqueue(simulationManager._scenarios.Dequeue());
                }
                simulationManager._scenarios = newScenariosQueue;
            }
            else
            {
                simulationManager._scenarios = newScenariosQueue;
            }

            //Basically adds the same simulation to the front of the queue again
            //Second time it get a crash, for some reason

        }
        private void RestartAllScenarios()
        {
            simulationManager.AttemptSetPlayState(Maes.UI.SimulationPlayState.Play); //Avoids a crash when restarting during pause
            Queue<SimulationScenario<TSimulation>> tempScenariosQueue = new Queue<SimulationScenario<TSimulation>>();
            foreach (var scenario in simulationManager._initialScenarios)
            {
                tempScenariosQueue.Enqueue(scenario);
            }
            simulationManager.RemoveCurrentSimulation();

            simulationManager._scenarios = tempScenariosQueue;
        }
        private void MakeAndRunSinglePopup()
        {

        }
        private void MakeBatchPopup()
        {

        }
    }
}
