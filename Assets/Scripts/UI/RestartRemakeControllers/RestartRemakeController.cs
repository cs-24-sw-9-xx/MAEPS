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
using UnityEngine.UIElements;

namespace Maes.UI.RestartRemakeControllers
{
    public abstract class RestartRemakeController<TSimulation, TAlgorithm, TScenario> : MonoBehaviour
        where TSimulation : class, ISimulation<TSimulation, TAlgorithm, TScenario>
        where TAlgorithm : IAlgorithm
        where TScenario : SimulationScenario<TSimulation, TAlgorithm>
    {
        public UIDocument uiDocument = null!;
        public SimulationManager<TSimulation, TAlgorithm, TScenario> simulationManager = null!;

        // Set by Start
        private Button _restartCurrentButton = null!;
        private Button _restartAllButton = null!;
        private Button _makeAndRunButton = null!;
        private Button _createBatchButton = null!;


        private void Start()
        {
            _restartCurrentButton = uiDocument.rootVisualElement.Q<Button>("RestartCurrentButton");
            _restartAllButton = uiDocument.rootVisualElement.Q<Button>("RestartAllButton");

            _makeAndRunButton = uiDocument.rootVisualElement.Q<Button>("MakeAndRunButton");
            _createBatchButton = uiDocument.rootVisualElement.Q<Button>("CreateBatchButton");

            // TODO: What is the point of this panel?
            // Why would you ever restart scenarios?
            // It should all be deterministic.
            // 2 of the buttons don't even do anything.
            // FIXME: !!!!! IT IS NOT DETERMINISTIC WHEN YOU RESTART SCENARIOS !!!!!

            _restartCurrentButton.RegisterCallback<ClickEvent>(RestartCurrentScenario);
            _restartAllButton.RegisterCallback<ClickEvent>(RestartAllScenarios);

            _makeAndRunButton.RegisterCallback<ClickEvent>(MakeAndRunClicked);
            _createBatchButton.RegisterCallback<ClickEvent>(CreateBatchClicked);
        }

        private void RestartCurrentScenario(ClickEvent clickEvent)
        {
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

        private void RestartAllScenarios(ClickEvent clickEvent)
        {
            simulationManager.AttemptSetPlayState(SimulationPlayState.Play); //Avoids a crash when restarting during pause
            var tempScenariosQueue = new Queue<TScenario>();
            foreach (var scenario in simulationManager.InitialScenarios)
            {
                tempScenariosQueue.Enqueue(scenario);
            }
            simulationManager.RemoveCurrentSimulation();

            simulationManager.Scenarios = tempScenariosQueue;
        }

        private static void MakeAndRunClicked(ClickEvent clickEvent)
        {
            Debug.LogWarning("Does nothing");
        }

        private static void CreateBatchClicked(ClickEvent clickEvent)
        {
            Debug.LogWarning("Does nothing");
        }
    }
}