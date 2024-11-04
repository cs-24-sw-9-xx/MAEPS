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

using System.Collections.Generic;
using JetBrains.Annotations;

using Maes.Algorithms;
using Maes.ExplorationAlgorithm.TheNextFrontier;
using Maes.Map;
using Maes.Map.MapGen;

using MAES.Map.RobotSpawners;

using Maes.Robot;
using MAES.Simulation;
using MAES.Simulation.SimulationScenarios;

using Maes.Trackers;
using Maes.UI;

using MAES.UI.SimulationInfoUIControllers;

using Maes.Visualizer;
using UnityEngine;

namespace Maes.Simulation
{
    public abstract class SimulationBase<TSimulation, TVisualizer, TVisualizerTile, TTracker, TSimulationInfoUIController, TAlgorithm, TScenario, TRobotSpawner> : MonoBehaviour, ISimulation<TSimulation, TAlgorithm, TScenario>
    where TSimulation : SimulationBase<TSimulation, TVisualizer, TVisualizerTile, TTracker, TSimulationInfoUIController, TAlgorithm, TScenario, TRobotSpawner>
    where TVisualizer : MonoBehaviour, IVisualizer<TVisualizerTile>
    where TTracker : ITracker
    where TSimulationInfoUIController : SimulationInfoUIControllerBase<TSimulation, TAlgorithm, TScenario>
    where TAlgorithm : IAlgorithm
    where TScenario : SimulationScenario<TSimulation, TAlgorithm>
    where TRobotSpawner : RobotSpawner<TAlgorithm>
    {
        public static SimulationBase<TSimulation, TVisualizer, TVisualizerTile, TTracker, TSimulationInfoUIController, TAlgorithm, TScenario, TRobotSpawner> SingletonInstance;
        
        public int SimulatedLogicTicks { get; private set; } = 0;
        public int SimulatedPhysicsTicks { get; private set; } = 0;
        public float SimulateTimeSeconds { get; private set; } = 0;

        public MapSpawner MapGenerator;
        
        public TRobotSpawner RobotSpawner;

        public abstract TVisualizer Visualizer { get; }
        
        public abstract TTracker Tracker { get; }
        
        ITracker ISimulation.Tracker => Tracker;

        protected TScenario _scenario;
        protected SimulationMap<Tile> _collisionMap;
        public List<MonaRobot> Robots;


        IReadOnlyList<MonaRobot> ISimulation.Robots => Robots;

        [CanBeNull] private MonaRobot _selectedRobot;
        public bool HasSelectedRobot() => _selectedRobot != null;
        [CanBeNull] private VisibleTagInfoHandler _selectedTag;
        public bool HasSelectedTag() => _selectedTag != null;
        internal CommunicationManager _communicationManager;

        // The debugging visualizer provides 
        protected DebuggingVisualizer _debugVisualizer = new DebuggingVisualizer();

        protected SimulationInfoUIControllerBase<TSimulation, TAlgorithm, TScenario> SimInfoUIController;

        private bool _started;

        protected virtual void AfterStart()
        {
            // Override me
        }

        private void Start()
        {
            MapGenerator = Resources.Load<MapSpawner>("MapGenerator");
            var simInfoUIControllerGameObject = GameObject.Find("SettingsPanel");
            SimInfoUIController = simInfoUIControllerGameObject
                .GetComponent<SimulationInfoUIControllerBase<TSimulation, TAlgorithm, TScenario>>();
            
            AfterStart();
            
            _started = true;
        }

        // Sets up the simulation by generating the map and spawning the robots
        public virtual void SetScenario(TScenario scenario)
        {
            _scenario = scenario;
            var mapInstance = Instantiate(MapGenerator, transform);
            _collisionMap = scenario.MapSpawner(mapInstance);
            AfterCollisionMapGenerated(scenario);
            _communicationManager = new CommunicationManager(_collisionMap, scenario.RobotConstraints, _debugVisualizer);
            RobotSpawner.CommunicationManager = _communicationManager;
            RobotSpawner.RobotConstraints = scenario.RobotConstraints;

            Robots = scenario.RobotSpawner(_collisionMap, RobotSpawner);
            _communicationManager.SetRobotRelativeSize(scenario.RobotConstraints.AgentRelativeSize);
            foreach (var robot in Robots)
                robot.OnRobotSelected = SetSelectedRobot;

            _communicationManager.SetRobotReferences(Robots);

        }

        public void SetInfoUIController(SimulationInfoUIControllerBase<TSimulation, TAlgorithm, TScenario> infoUIController)
        {
            SimInfoUIController = infoUIController;
        }

        protected virtual void AfterCollisionMapGenerated(TScenario scenario)
        {
            
        }

        public void SetSelectedRobot([CanBeNull] MonaRobot newSelectedRobot)
        {
            // Disable outline on previously selected robot
            if (_selectedRobot != null) _selectedRobot.outLine.enabled = false;
            _selectedRobot = newSelectedRobot;
            if (newSelectedRobot != null) newSelectedRobot.outLine.enabled = true;
            Tracker.SetVisualizedRobot(newSelectedRobot);
            if (_selectedRobot == null) SimInfoUIController.ClearSelectedRobot();
            UpdateDebugInfo();
        }

        public void SelectFirstRobot()
        {
            SetSelectedRobot(Robots[0]);
        }

        public void SetSelectedTag([CanBeNull] VisibleTagInfoHandler newSelectedTag)
        {
            if (_selectedTag != null) _selectedTag.outline.enabled = false;
            _selectedTag = newSelectedTag;
            if (newSelectedTag != null) newSelectedTag.outline.enabled = true;
            UpdateDebugInfo();
        }

        public void LogicUpdate()
        {
            _debugVisualizer.LogicUpdate();
            Tracker.LogicUpdate(Robots);
            Robots.ForEach(robot => robot.LogicUpdate());
            SimulatedLogicTicks++;
            _communicationManager.LogicUpdate();
        }

        public void PhysicsUpdate()
        {
            Robots.ForEach(simUnit => simUnit.PhysicsUpdate());
            Physics2D.Simulate(GlobalSettings.PhysicsTickDeltaSeconds);
            SimulateTimeSeconds += GlobalSettings.PhysicsTickDeltaSeconds;
            SimulatedPhysicsTicks++;
            _debugVisualizer.PhysicsUpdate();
            _communicationManager.PhysicsUpdate();
        }

        /// <summary>
        /// Tests specifically if The Next Frontier is no longer doing any work.
        /// </summary>
        public bool TnfBotsOutOfFrontiers()
        {
            var res = true;
            foreach (var monaRobot in Robots)
            {
                res &= (monaRobot.Algorithm as TnfExplorationAlgorithm)?.IsOutOfFrontiers() ?? true;
            }

            return res;
        }

        public void UpdateDebugInfo()
        {
            if (_selectedRobot != null)
            {
                if (GlobalSettings.IsRosMode)
                {
                    SimInfoUIController.UpdateAlgorithmDebugInfo(_selectedRobot.Algorithm.GetDebugInfo());
                    // SimInfoUIController.UpdateControllerDebugInfo(_selectedRobot.Controller.GetDebugInfo());
                }
                else
                {
                    SimInfoUIController.UpdateAlgorithmDebugInfo(_selectedRobot.Algorithm.GetDebugInfo());
                    SimInfoUIController.UpdateControllerDebugInfo(_selectedRobot.Controller.GetDebugInfo());
                }

            }
            if (_selectedTag != null)
            {
                SimInfoUIController.UpdateTagDebugInfo(_selectedTag.GetDebugInfo());
            }
        }

        public virtual void OnSimulationFinished()
        {
            // Override me for functionality.
        }

        public void ShowAllTags()
        {
            _debugVisualizer.RenderVisibleTags();
        }

        public void ShowSelectedTags()
        {
            if (_selectedRobot != null)
            {
                _debugVisualizer.RenderSelectedVisibleTags(_selectedRobot.id);
            }
        }

        public void ClearVisualTags()
        {
            _debugVisualizer.HideAllTags();
        }

        public abstract bool HasFinishedSim();

        public void RenderCommunicationLines()
        {
            _debugVisualizer.RenderCommunicationLines();
        }

        public void Awake()
        {
            SingletonInstance = this;
        }

        public Vector2 WorldCoordinateToSlamPosition(Vector2 worldPosition)
        {
            return worldPosition;
        }

        private void OnDrawGizmos()
        {
            if (_collisionMap == null) {
                return;
            }

            var height = (_collisionMap.HeightInTiles + 1) / 2;
            var width = (_collisionMap.WidthInTiles + 1) / 2;
            Gizmos.color = Color.blue;
            for (float x = -width; x < width; x += 0.5f)
                Gizmos.DrawLine(new Vector3(x, -width, -0.01f), new Vector3(x, width, -0.01f));
            for (float y = -height; y < height; y += 0.5f)
                Gizmos.DrawLine(new Vector3(-height, y, -0.01f), new Vector3(height, y, -0.01f));

            Gizmos.color = Color.red;
            for (float x = -width; x < width; x += 1)
                Gizmos.DrawLine(new Vector3(x, -width, -0.01f), new Vector3(x, width, -0.01f));
            for (float y = -height; y < height; y += 1f)
                Gizmos.DrawLine(new Vector3(-height, y, -0.01f), new Vector3(height, y, -0.01f));

            Gizmos.color = Color.black;
            Gizmos.DrawLine(new Vector3(0, -width, -0.01f), new Vector3(0, width, -0.01f));
            Gizmos.DrawLine(new Vector3(-height, 0, -0.01f), new Vector3(height, 0, -0.01f));
        }
    }
}
