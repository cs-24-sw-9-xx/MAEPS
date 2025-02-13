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

using Maes.Simulation;

using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UIElements;

namespace Maes.UI
{
    public class SimulationSpeedController : MonoBehaviour
    {
        public UIDocument uiDocument = null!; // Set in editor
        
        // Set by Start
        private ISimulationManager _simulationManager = null!;
        private Button _pauseButton = null!;
        private Button _playButton = null!;
        public Button FastForwardButton { get; private set; } = null!;
        public Button FastAsPossibleButton { get; private set; } = null!;
        public Button StepButton { get; private set; } = null!;

        private void Awake()
        {
            _simulationManager = GetComponent<ISimulationManager>();
            
            _pauseButton = uiDocument.rootVisualElement.Q<Button>("PauseButton");
            _playButton = uiDocument.rootVisualElement.Q<Button>("PlayButton");
            FastForwardButton = uiDocument.rootVisualElement.Q<Button>("FastForwardButton");
            FastAsPossibleButton = uiDocument.rootVisualElement.Q<Button>("FastAsPossibleButton");
            StepButton = uiDocument.rootVisualElement.Q<Button>("StepButton");
            
            
            _pauseButton.RegisterCallback<ClickEvent>(Pause);
            _playButton.RegisterCallback<ClickEvent>(Play);
            FastForwardButton.RegisterCallback<ClickEvent>(FastForward);
            FastAsPossibleButton.RegisterCallback<ClickEvent>(FastAsPossible);
            StepButton.RegisterCallback<ClickEvent>(Step);
        }

        public void UpdateButtonsUI(SimulationPlayState currentState)
        {
            // Do not change ui for the duration of the step
            if (currentState == SimulationPlayState.Step)
            {
                return;
            }

            _pauseButton.EnableInClassList("toggled", currentState == SimulationPlayState.Paused);
            _playButton.EnableInClassList("toggled", currentState == SimulationPlayState.Play);
            FastForwardButton.EnableInClassList("toggled", currentState == SimulationPlayState.FastForward);
            FastAsPossibleButton.EnableInClassList("toggled", currentState == SimulationPlayState.FastAsPossible);
        }

        private void Pause(ClickEvent clickEvent)
        {
            AttemptSwitchState(SimulationPlayState.Paused);
        }

        private void Play(ClickEvent clickEvent)
        {
            AttemptSwitchState(SimulationPlayState.Play);
        }

        private void FastForward(ClickEvent clickEvent)
        {
            AttemptSwitchState(SimulationPlayState.FastForward);
        }

        private void FastAsPossible(ClickEvent clickEvent)
        {
            AttemptSwitchState(SimulationPlayState.FastAsPossible);
        }

        // Perform a single logic step then stop again
        private void Step(ClickEvent clickEvent)
        {
            AttemptSwitchState(SimulationPlayState.Step);
        }

        private void AttemptSwitchState(SimulationPlayState newPlayState)
        {
            var actualState = _simulationManager.AttemptSetPlayState(newPlayState);
            UpdateButtonsUI(actualState);
        }
    }
}