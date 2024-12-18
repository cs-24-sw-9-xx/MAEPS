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
using Maes.UI.SimulationInfoUIControllers;
using Maes.Utilities;

using UnityEngine;
using UnityEngine.UI;

namespace Maes.Simulation
{
    public abstract class SimulationManager<TSimulation, TAlgorithm, TScenario> : MonoBehaviour, ISimulationManager
    where TSimulation : class, ISimulation<TSimulation, TAlgorithm, TScenario>
    where TAlgorithm : IAlgorithm
    where TScenario : SimulationScenario<TSimulation, TAlgorithm>
    {
        public Queue<TScenario> Scenarios = new();
        public readonly Queue<TScenario> InitialScenarios = new();

        public GameObject SimulationPrefab = null!;
        public GameObject RosClockPrefab = null!;
        public GameObject RosVisualizerPrefab = null!;
        public SimulationSpeedController UISpeedController = null!;
        public GameObject UIControllerDebugTitle = null!;
        public GameObject UIControllerDebugInfo = null!;
        public Text SimulationStatusText = null!;
        public RectTransform controlPanel = null!;
        public RectTransform playButton = null!;
        public RectTransform pauseButton = null!;

        public SimulationInfoUIControllerBase<TSimulation, TAlgorithm, TScenario> SimulationInfoUIController = null!;
        ISimulationInfoUIController ISimulationManager.SimulationInfoUIController => SimulationInfoUIController;

        public TScenario? CurrentScenario { get; private set; }
        ISimulationScenario? ISimulationManager.CurrentScenario => CurrentScenario;

        public TSimulation? CurrentSimulation { get; private set; }
        ISimulation? ISimulationManager.CurrentSimulation => CurrentSimulation;

        public SimulationPlayState PlayState { get; private set; } = SimulationPlayState.Paused;

        private GameObject? _simulationGameObject;

        private int _physicsTicksSinceUpdate;

        // Timing variables for controlling the simulation in a manner that is decoupled from Unity's update system
        private long _nextUpdateTimeMillis;

        public bool AutoMaxSpeedInBatchMode { get; set; }

        private float _startTime;

        private int _simulations;

        // Runs once when starting the program
        private void Start()
        {
            // This simulation handles physics updates custom time factors, so disable built in real time physics calls
            Physics.simulationMode = SimulationMode.Script;
            //Physics.autoSimulation = false;
            Physics2D.simulationMode = SimulationMode2D.Script;

            // Adapt UI for ros mode
            if (GlobalSettings.IsRosMode)
            {
                CreateRosClockAndVisualiserObjects();
                RemoveFastForwardButtonsFromControlPanel();
                UIControllerDebugTitle.SetActive(false);
                UIControllerDebugInfo.SetActive(false);
            }
            UISpeedController.UpdateButtonsUI(SimulationPlayState.Play);

            _startTime = Time.realtimeSinceStartup;
        }

        private void RemoveFastForwardButtonsFromControlPanel()
        {
            // Deactivate fast forward buttons
            UISpeedController.stepperButton.gameObject.SetActive(false);
            UISpeedController.fastForwardButton.gameObject.SetActive(false);
            UISpeedController.fastAsPossibleButton.gameObject.SetActive(false);

            // Resize background
            controlPanel.sizeDelta = new Vector2(100, 50);

            // Reposition play button
            playButton.anchoredPosition = new Vector2(-20, 0);

            // Reposition pause button
            pauseButton.anchoredPosition = new Vector2(20, 0);
        }

        private void CreateRosClockAndVisualiserObjects()
        {
            Instantiate(RosClockPrefab, new Vector3(0, 0, 0), Quaternion.identity);
            Instantiate(RosVisualizerPrefab, new Vector3(0, 0, 0), Quaternion.identity);
        }

        public SimulationPlayState AttemptSetPlayState(SimulationPlayState targetState)
        {
            if (targetState == PlayState)
            {
                return PlayState;
            }

            if (CurrentScenario == null)
            {
                targetState = SimulationPlayState.Paused;
            }

            PlayState = targetState;
            // Reset next update time when changing play mode to avoid skipping ahead
            _nextUpdateTimeMillis = TimeUtils.CurrentTimeMillis();
            UISpeedController.UpdateButtonsUI(PlayState);
            return PlayState;
        }

        private void Update()
        {
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                if (Input.GetKeyDown(KeyCode.Alpha1))
                {
                    AttemptSetPlayState(SimulationPlayState.Play);
                }
                else if (Input.GetKeyDown(KeyCode.Alpha2))
                {
                    AttemptSetPlayState(SimulationPlayState.FastForward);
                }
                else if (Input.GetKeyDown(KeyCode.Alpha3))
                {
                    AttemptSetPlayState(SimulationPlayState.FastAsPossible);
                }
                else if (Input.GetKeyDown(KeyCode.P))
                {
                    AttemptSetPlayState(SimulationPlayState.Step);
                }
            }

            if (CurrentSimulation == null)
            {
                return;
            }

            var simulatedTimeSpan = TimeSpan.FromSeconds(CurrentSimulation.SimulateTimeSeconds);
            var output = simulatedTimeSpan.ToString(@"hh\:mm\:ss");
            SimulationStatusText.text = "Phys. ticks: " + CurrentSimulation.SimulatedPhysicsTicks +
                                        "\nLogic ticks: " + CurrentSimulation.SimulatedLogicTicks +
                                        "\nSimulated: " + output;
        }

        // This method is responsible for executing simulation updates at an appropriate speed, to provide simulation in
        // real time (or whatever speed setting is chosen)
        private void FixedUpdate()
        {
            if (PlayState == SimulationPlayState.Paused)
            {
                if (Application.isBatchMode)
                {
                    // The simulation will only enter paused mode after finishing when in headless/batch mode
                    Application.Quit(0);
                }
                return;
            }

            if (Application.isBatchMode && AutoMaxSpeedInBatchMode && PlayState != SimulationPlayState.FastAsPossible)
            {
                AttemptSetPlayState(SimulationPlayState.FastAsPossible);
            }

            var startTimeMillis = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            var millisPerFixedUpdate = (int)(1000f * Time.fixedDeltaTime);
            // Subtract 8 milliseconds to allow for other procedures such as rendering to occur between updates 
            millisPerFixedUpdate -= 8;
            var fixedUpdateEndTime = startTimeMillis + millisPerFixedUpdate;

            // ReSharper disable once PossibleLossOfFraction
            var physicsTickDeltaMillis =
                GlobalSettings.LogicTickDeltaMillis / GlobalSettings.PhysicsTicksPerLogicUpdate;

            // Only calculate updates if there is still time left in the current update
            while (TimeUtils.CurrentTimeMillis() - startTimeMillis < millisPerFixedUpdate)
            {
                // Yield if no more updates are needed this FixedUpdate cycle
                if (_nextUpdateTimeMillis > fixedUpdateEndTime)
                {
                    break;
                }

                var shouldContinue = UpdateSimulation();
                if (!shouldContinue)
                {
                    AttemptSetPlayState(SimulationPlayState.Paused);
                    return;
                }

                // The delay before simulating the next update is dependant on the current simulation play speed
                var updateDelayMillis = physicsTickDeltaMillis / (int)PlayState;
                // Do not try to catch up if more than 0.5 seconds behind (higher if tick delta is high)
                long maxDelayMillis = Math.Max(500, physicsTickDeltaMillis * 10);
                _nextUpdateTimeMillis = Math.Max(_nextUpdateTimeMillis + updateDelayMillis, TimeUtils.CurrentTimeMillis() - maxDelayMillis);
            }
        }

        // Calls update on all children of SimulationContainer that are of type SimulationUnit
        private bool UpdateSimulation()
        {
            if (CurrentScenario != null && CurrentSimulation != null && CurrentScenario.HasFinishedSim(CurrentSimulation))
            {
                CurrentSimulation.OnSimulationFinished();
                RemoveCurrentSimulation();
            }

            if (CurrentScenario == null || CurrentSimulation == null)
            {
                if (Scenarios.Count == 0)
                {
                    // Indicate that no further updates are needed
                    Debug.Log($"Finished in {Time.realtimeSinceStartup - _startTime} seconds.");
                    return false;
                }

                // Otherwise continue to next simulation in the queue
                CreateSimulation(Scenarios.Dequeue());
            }

            CurrentSimulation!.PhysicsUpdate();
            _physicsTicksSinceUpdate++;
            var shouldContinueSim = true;
            if (_physicsTicksSinceUpdate >= GlobalSettings.PhysicsTicksPerLogicUpdate)
            {
                CurrentSimulation.LogicUpdate();
                _physicsTicksSinceUpdate = 0;
                UpdateStatisticsUI();


                // If the simulator is in step mode, then automatically pause after logic step has been performed
                if (PlayState == SimulationPlayState.Step)
                {
                    shouldContinueSim = false;
                }
            }

            return shouldContinueSim;
        }

        private void CreateSimulation(TScenario scenario)
        {
            Debug.Log($"Creating Simulation nr {_simulations++} rest: {Scenarios.Count}");
            CurrentScenario = scenario;
            _simulationGameObject = Instantiate(SimulationPrefab, transform);
            CurrentSimulation = _simulationGameObject.GetComponent<TSimulation>();
            CurrentSimulation.SetScenario(scenario);
            CurrentSimulation.SetInfoUIController(SimulationInfoUIController);

            SimulationInfoUIController.NotifyNewSimulation(CurrentSimulation);
        }


        private void UpdateStatisticsUI()
        {
            SimulationInfoUIController.UpdateStatistics(CurrentSimulation);
            CurrentSimulation?.UpdateDebugInfo();
            CurrentSimulation?.Tracker.UIUpdate();
        }

        public void RemoveCurrentSimulation()
        {
            Destroy(_simulationGameObject);
            CurrentScenario = null;
            CurrentSimulation = null;
            _simulationGameObject = null;
        }

        public void EnqueueScenario(TScenario simulationScenario)
        {
            if (HasActiveScenario())
            {
                Scenarios.Enqueue(simulationScenario);
            }
            else // This is the first scenario, initialize it immediately 
            {
                CreateSimulation(simulationScenario);
            }
        }

        public bool HasActiveScenario()
        {
            return CurrentScenario is not null;
        }

    }
}