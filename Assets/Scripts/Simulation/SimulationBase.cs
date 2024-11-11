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

using Maes.Algorithms;
using Maes.ExplorationAlgorithm.TheNextFrontier;
using Maes.Map;
using Maes.Map.MapGen;
using Maes.Map.RobotSpawners;
using Maes.Robot;
using Maes.Simulation.SimulationScenarios;
using Maes.Statistics;
using Maes.Trackers;
using Maes.UI.SimulationInfoUIControllers;
using Maes.Visualizers;

using UnityEngine;

namespace Maes.Simulation
{
    public abstract class SimulationBase<TSimulation, TVisualizer, TVisualizerTile, TTracker, TSimulationInfoUIController, TAlgorithm, TScenario, TRobotSpawner> : MonoBehaviour, ISimulation<TSimulation, TAlgorithm, TScenario>
    where TSimulation : SimulationBase<TSimulation, TVisualizer, TVisualizerTile, TTracker, TSimulationInfoUIController, TAlgorithm, TScenario, TRobotSpawner>
    where TVisualizerTile : ICell
    where TVisualizer : MonoBehaviour, IVisualizer<TVisualizerTile>
    where TTracker : ITracker
    where TSimulationInfoUIController : SimulationInfoUIControllerBase<TSimulation, TAlgorithm, TScenario>
    where TAlgorithm : IAlgorithm
    where TScenario : SimulationScenario<TSimulation, TAlgorithm>
    where TRobotSpawner : RobotSpawner<TAlgorithm>
    {
        public int SimulatedLogicTicks { get; private set; }
        public int SimulatedPhysicsTicks { get; private set; }
        public float SimulateTimeSeconds { get; private set; }

        public MapSpawner MapGenerator = null!;

        public TRobotSpawner RobotSpawner = null!;

        public IReadOnlyList<MonaRobot> Robots => _robots;

        private List<MonaRobot> _robots = new();

        public abstract TVisualizer Visualizer { get; }

        public abstract TTracker Tracker { get; }

        ITracker ISimulation.Tracker => Tracker;

        // Set by SetScenario
        protected TScenario _scenario = null!;

        // Set by SetScenario
        protected SimulationMap<Tile> _collisionMap = null!;

        // Set by SetScenario
        public CommunicationManager CommunicationManager { get; private set; } = null!;


        private MonaRobot? _selectedRobot;
        public bool HasSelectedRobot() => _selectedRobot != null;
        private VisibleTagInfoHandler? _selectedTag;
        public bool HasSelectedTag() => _selectedTag != null;

        // The debugging visualizer provides 
        protected DebuggingVisualizer _debugVisualizer = new DebuggingVisualizer();

        // Set by SetInfoUIController
        protected SimulationInfoUIControllerBase<TSimulation, TAlgorithm, TScenario> SimInfoUIController
        {
            get;
            private set;
        } = null!;

        // Sets up the simulation by generating the map and spawning the robots
        public virtual void SetScenario(TScenario scenario)
        {
            _scenario = scenario;
            var mapInstance = Instantiate(MapGenerator, transform);
            _collisionMap = scenario.MapSpawner(mapInstance);
            AfterCollisionMapGenerated(scenario);
            CommunicationManager = new CommunicationManager(_collisionMap, scenario.RobotConstraints, _debugVisualizer);
            RobotSpawner.CommunicationManager = CommunicationManager;
            RobotSpawner.RobotConstraints = scenario.RobotConstraints;

            _robots = scenario.RobotSpawner(_collisionMap, RobotSpawner);
            CommunicationManager.SetRobotRelativeSize(scenario.RobotConstraints.AgentRelativeSize);
            foreach (var robot in Robots)
                robot.OnRobotSelected = SetSelectedRobot;

            CommunicationManager.SetRobotReferences(Robots);

        }

        public void SetInfoUIController(SimulationInfoUIControllerBase<TSimulation, TAlgorithm, TScenario> infoUIController)
        {
            SimInfoUIController = infoUIController;
        }

        protected virtual void AfterCollisionMapGenerated(TScenario scenario)
        {

        }

        public void SetSelectedRobot(MonaRobot? newSelectedRobot)
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

        public void SetSelectedTag(VisibleTagInfoHandler? newSelectedTag)
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
            foreach (var robot in Robots)
            {
                robot.LogicUpdate();
            }
            SimulatedLogicTicks++;
            CommunicationManager.LogicUpdate();
        }

        public void PhysicsUpdate()
        {
            foreach (var robot in Robots)
            {
                robot.PhysicsUpdate();
            }
            Physics2D.Simulate(GlobalSettings.PhysicsTickDeltaSeconds);
            SimulateTimeSeconds += GlobalSettings.PhysicsTickDeltaSeconds;
            SimulatedPhysicsTicks++;
            _debugVisualizer.PhysicsUpdate();
            CommunicationManager.PhysicsUpdate();
        }

        // TODO: This function should not be here wtf is this?
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

        public Vector2 WorldCoordinateToSlamPosition(Vector2 worldPosition)
        {
            return worldPosition;
        }

        private void OnDrawGizmos()
        {
            if (_collisionMap == null)
            {
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