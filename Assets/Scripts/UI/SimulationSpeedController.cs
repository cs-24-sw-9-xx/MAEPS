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
using UnityEngine.UI;

namespace Maes.UI
{
    public class SimulationSpeedController : MonoBehaviour
    {
        public Button pauseButton = null!;
        public Button playButton = null!;
        public Button fastForwardButton = null!;
        public Button fastAsPossibleButton = null!;
        public Button stepperButton = null!;

        // Set by Start
        private ISimulationManager _simulationManager = null!;

        private void Start()
        {
            _simulationManager = GetComponent<ISimulationManager>();

            pauseButton.onClick.AddListener(Pause);
            playButton.onClick.AddListener(Play);
            fastForwardButton.onClick.AddListener(FastForward);
            fastAsPossibleButton.onClick.AddListener(FastAsPossible);
            stepperButton.onClick.AddListener(Step);
        }

        public void UpdateButtonsUI(SimulationPlayState currentState)
        {
            // Do not change ui for the duration of the step
            if (currentState == SimulationPlayState.Step)
            {
                return;
            }

            pauseButton.image.color = (currentState == SimulationPlayState.Paused) ? Color.green : Color.white;
            playButton.image.color = (currentState == SimulationPlayState.Play) ? Color.green : Color.white;
            fastForwardButton.image.color =
                (currentState == SimulationPlayState.FastForward) ? Color.green : Color.white;
            fastAsPossibleButton.image.color =
                (currentState == SimulationPlayState.FastAsPossible) ? Color.green : Color.white;
        }

        public void Pause()
        {
            AttemptSwitchState(SimulationPlayState.Paused);
        }

        public void Play()
        {
            AttemptSwitchState(SimulationPlayState.Play);
        }

        public void FastForward()
        {
            AttemptSwitchState(SimulationPlayState.FastForward);
        }

        public void FastAsPossible()
        {
            AttemptSwitchState(SimulationPlayState.FastAsPossible);
        }

        // Perform a single logic step then stop again
        public void Step()
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