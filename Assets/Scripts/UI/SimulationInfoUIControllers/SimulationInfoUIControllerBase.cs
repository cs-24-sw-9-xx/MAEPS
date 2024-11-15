// Copyright 2022 MAES
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
// Contributors: Malte Z. Andreasen, Philip I. Holler and Magnus K. Jensen
// 
// Original repository: https://github.com/MalteZA/MAES

using Maes.Algorithms;
using Maes.Simulation;
using Maes.Simulation.SimulationScenarios;

using UnityEngine;
using UnityEngine.UI;

namespace Maes.UI.SimulationInfoUIControllers
{
    public abstract class SimulationInfoUIControllerBase<TSimulation, TAlgorithm, TScenario> : MonoBehaviour, ISimulationInfoUIController
    where TSimulation : class, ISimulation<TSimulation, TAlgorithm, TScenario>
    where TAlgorithm : IAlgorithm
    where TScenario : SimulationScenario<TSimulation, TAlgorithm>
    {
        public Text AlgorithmDebugText = null!;
        public Text ControllerDebugText = null!;
        public Text TagDebugText = null!;

        public Text MouseCoordinateText = null!;

        public Button StickyCameraButton = null!;

        public TSimulation? Simulation => simulationManager.CurrentSimulation;

        public SimulationManager<TSimulation, TAlgorithm, TScenario> simulationManager = null!;
        // Represents a function that modifies the given simulation in some way
        // (for example by changing map visualization mode)
        protected delegate void SimulationModification(TSimulation? simulation);

        protected SimulationModification? _mostRecentMapVisualizationModification;

        protected readonly Color _mapVisualizationColor = Color.white;
        protected readonly Color _mapVisualizationSelectedColor = new(150 / 255f, 200 / 255f, 150 / 255f);

        protected abstract Button[] MapVisualizationToggleGroup { get; }

        protected abstract void AfterStart();

        private void Start()
        {
            StickyCameraButton.onClick.AddListener(() =>
            {
                CameraController.SingletonInstance.stickyCam = !CameraController.SingletonInstance.stickyCam;
                StickyCameraButton.image.color = CameraController.SingletonInstance.stickyCam ? _mapVisualizationSelectedColor : _mapVisualizationColor;
            });

            AfterStart();
        }

        public virtual void Update() { }

        public virtual void ClearSelectedRobot()
        {
            CameraController.SingletonInstance.stickyCam = false;
            StickyCameraButton.image.color = _mapVisualizationColor;
        }

        public void UpdateMouseCoordinates(Vector2 mousePosition)
        {
            var xNumberString = $"{mousePosition.x:00.00}".PadLeft(6);
            var yNumberString = $"{mousePosition.y:00.00}".PadLeft(6);
            MouseCoordinateText.text = $"(x: {xNumberString}, y: {yNumberString})";
        }

        // This function executes the given map visualization change and remembers it.
        // Whenever the simulator creates a new simulation the most recent visualization change is repeated 
        protected void ExecuteAndRememberMapVisualizationModification(SimulationModification modificationFunc)
        {
            _mostRecentMapVisualizationModification = modificationFunc;
            modificationFunc(Simulation);
        }

        public void UpdateAlgorithmDebugInfo(string info)
        {
            AlgorithmDebugText.text = info;
        }


        public void UpdateControllerDebugInfo(string info)
        {
            ControllerDebugText.text = info;
        }

        public void UpdateTagDebugInfo(string info)
        {
            TagDebugText.text = info;
        }

        // Called whenever the simulator instantiates a new simulation object 
        protected abstract void NotifyNewSimulation(TSimulation? newSimulation);

        public void NotifyNewSimulation(ISimulation? simulation)
        {
            NotifyNewSimulation((TSimulation?)simulation);
        }

        protected abstract void UpdateStatistics(TSimulation? simulation);

        public void UpdateStatistics(ISimulation? simulation)
        {
            UpdateStatistics((TSimulation?)simulation);
        }

        // Highlights the selected map visualization button
        protected void SelectVisualizationButton(Button selectedButton)
        {
            foreach (var button in MapVisualizationToggleGroup)
            {
                button.image.color = _mapVisualizationColor;
            }

            selectedButton.image.color = _mapVisualizationSelectedColor;
        }
    }
}