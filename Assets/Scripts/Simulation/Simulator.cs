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
using Maes.Simulation.SimulationScenarios;
using Maes.UI;

using UnityEngine;

using Object = UnityEngine.Object;

namespace Maes.Simulation {
    public abstract class Simulator<TSimulation, TAlgorithm, TScenario>
        where TSimulation : class, ISimulation<TSimulation, TAlgorithm, TScenario>
        where TAlgorithm : IAlgorithm
        where TScenario : SimulationScenario<TSimulation, TAlgorithm>
    {
        protected static Simulator<TSimulation, TAlgorithm, TScenario>? _instance;
        private readonly GameObject _maesGameObject;
        protected SimulationManager<TSimulation, TAlgorithm, TScenario> _simulationManager;

        protected Simulator() {
            // Initialize the simulator by loading the prefab from the resources and then instantiating the prefab
            var prefab = LoadSimulatorGameObject();
            _maesGameObject = Object.Instantiate(prefab);
            _simulationManager = _maesGameObject.GetComponentInChildren<SimulationManager<TSimulation, TAlgorithm, TScenario>>();
        }

        protected abstract GameObject LoadSimulatorGameObject();

        // Clears the singleton instance and removes the simulator game object
        public static void Destroy() {
            if (_instance != null) {
                Object.Destroy(_instance._maesGameObject);
                _instance = null;
            }
        }
        
        public void EnqueueScenario(TScenario scenario) {
            _simulationManager.EnqueueScenario(scenario);
            _simulationManager.InitialScenarios.Enqueue(scenario);
        }
        public void EnqueueScenarios(IEnumerable<TScenario> scenario) {
            foreach (var simulationScenario in scenario) {
                _simulationManager.EnqueueScenario(simulationScenario);
            }
        }
        
        public void PressPlayButton() {
            if (_simulationManager.PlayState == SimulationPlayState.Play)
                throw new InvalidOperationException("Cannot start simulation when it is already in play mode");
            if (!_simulationManager.HasActiveScenario())
                throw new InvalidOperationException("You must enqueue at least one scenario before starting the" +
                                                    " simulation");


            _simulationManager.AttemptSetPlayState(SimulationPlayState.Play);
        }

        public SimulationManager<TSimulation, TAlgorithm, TScenario> GetSimulationManager() {
            return _simulationManager;
        }
    }
}