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
// Contributors: Rasmus Borrisholt Schmidt, Andreas Sebastian Sørensen, Thor Beregaard, Malte Z. Andreasen, Philip I. Holler and Magnus K. Jensen,
// 
// Original repository: https://github.com/Molitany/MAES

using System;
using System.Collections.Generic;

using Maes.Algorithms;
using Maes.UI;

using UnityEngine;

using Object = UnityEngine.Object;

namespace Maes.Simulation
{
    public abstract class Simulator<TSimulation, TAlgorithm, TScenario>
        where TSimulation : class, ISimulation<TSimulation, TAlgorithm, TScenario>
        where TAlgorithm : IAlgorithm
        where TScenario : SimulationScenario<TSimulation, TAlgorithm>
    {
        public SimulationManager<TSimulation, TAlgorithm, TScenario> SimulationManager { get; }

        private readonly GameObject _maesGameObject;

        protected Simulator(bool autoMaxSpeedInBatchMode = true)
        {
            // Initialize the simulator by loading the prefab from the resources and then instantiating the prefab
            var prefab = LoadSimulatorGameObject();
            _maesGameObject = Object.Instantiate(prefab);
            SimulationManager = _maesGameObject.GetComponentInChildren<SimulationManager<TSimulation, TAlgorithm, TScenario>>();
            SimulationManager.AutoMaxSpeedInBatchMode = autoMaxSpeedInBatchMode;
        }

        protected abstract GameObject LoadSimulatorGameObject();

        // Clears the singleton instance and removes the simulator game object
        public void Destroy()
        {
            Object.Destroy(_maesGameObject);
        }

        public void EnqueueScenario(TScenario scenario)
        {
            SimulationManager.EnqueueScenario(scenario);
            SimulationManager.InitialScenarios.Enqueue(scenario);
        }
        public void EnqueueScenarios(IEnumerable<TScenario> scenario)
        {
            foreach (var simulationScenario in scenario)
            {
                SimulationManager.EnqueueScenario(simulationScenario);
            }
        }

        public void PressPlayButton()
        {
            if (SimulationManager.PlayState == SimulationPlayState.Play)
            {
                throw new InvalidOperationException("Cannot start simulation when it is already in play mode");
            }

            if (!SimulationManager.HasActiveScenario())
            {
                throw new InvalidOperationException("You must enqueue at least one scenario before starting the" +
                                                    " simulation");
            }

            SimulationManager.AttemptSetPlayState(SimulationPlayState.Play);
        }
    }
}