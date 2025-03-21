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
using Maes.FaultInjections;
using Maes.Map;
using Maes.Map.Generators;
using Maes.Map.RobotSpawners;
using Maes.Robot;
using Maes.Simulation.Patrolling;
using Maes.Statistics.Trackers;
using Maes.UI;
using Maes.UI.SimulationInfoUIControllers;
using Maes.UI.Visualizers;

using UnityEngine;

namespace Maes.Simulation
{
    public abstract class SimulationBase<TSimulation, TVisualizer, TTracker, TSimulationInfoUIController, TAlgorithm, TScenario, TRobotSpawner> : MonoBehaviour, ISimulation<TSimulation, TAlgorithm, TScenario>
    where TSimulation : SimulationBase<TSimulation, TVisualizer, TTracker, TSimulationInfoUIController, TAlgorithm, TScenario, TRobotSpawner>
    where TVisualizer : MonoBehaviour, IVisualizer
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

        // Set by SetScenario
        public List<MonaRobot> Robots
        {
            get;
            private set;
        } = null!;

        public int NumberOfActiveRobots => RobotSpawner.transform.childCount;

        public abstract TVisualizer Visualizer { get; }

        public abstract TTracker Tracker { get; }

        ITracker ISimulation.Tracker => Tracker;

        // Set by SetScenario
        protected IFaultInjection? FaultInjection { get; private set; } = null!;

        // Set by SetScenario
        protected TScenario _scenario = null!;

        // Set by SetScenario
        protected SimulationMap<Tile> _collisionMap = null!;

        // Set by SetScenario
        public CommunicationManager CommunicationManager { get; private set; } = null!;


        private MonaRobot? _selectedRobot;

        public bool HasSelectedRobot => _selectedRobot != null;

        private VisibleTagInfoHandler? _selectedTag;

        public bool HasSelectedTag => _selectedTag != null;

        // The debugging visualizer provides 
        private readonly DebuggingVisualizer _debugVisualizer = new();

        // Set by SetInfoUIController
        private SimulationInfoUIControllerBase<TSimulation, TAlgorithm, TScenario> SimInfoUIController = null!;

        // Sets up the simulation by generating the map and spawning the robots
        public virtual void SetScenario(TScenario scenario)
        {
            _scenario = scenario;
            var mapInstance = Instantiate(MapGenerator, transform);
            _collisionMap = scenario.MapSpawner(mapInstance);
            CommunicationManager = new CommunicationManager(_collisionMap, scenario.RobotConstraints, _debugVisualizer);
            AfterCollisionMapGenerated(scenario);
            RobotSpawner.CommunicationManager = CommunicationManager;
            RobotSpawner.RobotConstraints = scenario.RobotConstraints;

            Robots = scenario.RobotSpawner(_collisionMap, RobotSpawner);
            CommunicationManager.SetRobotRelativeSize(scenario.RobotConstraints.AgentRelativeSize);
            foreach (var robot in Robots)
            {
                if (scenario is PatrollingSimulationScenario simulationScenario)
                {
                    // Assign robots to partitions, distributed evenly.
                    robot.AssignedPartition = (((robot.id) % simulationScenario.Partitions) + 1);
                }
                robot.OnRobotSelected = SetSelectedRobot;
            }

            CommunicationManager.SetRobotReferences(Robots);
            FaultInjection = scenario.FaultInjection;
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
            if (_selectedRobot != null)
            {
                _selectedRobot.HideOutline();
            }

            _selectedRobot = newSelectedRobot;
            if (newSelectedRobot != null)
            {
                newSelectedRobot.ShowOutline();
            }

            Tracker.SetVisualizedRobot(newSelectedRobot);
            if (_selectedRobot == null)
            {
                SimInfoUIController.ClearSelectedRobot();
            }

            UpdateDebugInfo();
        }

        public void SelectFirstRobot()
        {
            SetSelectedRobot(Robots[0]);
        }

        public void SetSelectedTag(VisibleTagInfoHandler? newSelectedTag)
        {
            if (_selectedTag != null)
            {
                _selectedTag.outline.enabled = false;
            }

            _selectedTag = newSelectedTag;
            if (newSelectedTag != null)
            {
                newSelectedTag.outline.enabled = true;
            }

            UpdateDebugInfo();
        }

        public void LogicUpdate()
        {
            _debugVisualizer.LogicUpdate();
            FaultInjection?.LogicUpdate(Robots, SimulatedLogicTicks);
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

        public void UpdateDebugInfo()
        {
            if (_selectedRobot is not null)
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
            if (_selectedTag is not null)
            {
                SimInfoUIController.UpdateTagDebugInfo(_selectedTag.GetDebugInfo());
            }
        }

        public virtual void OnSimulationFinished()
        {
            if (GlobalSettings.ShouldWriteCsvResults)
            {
                CreateStatisticsFile();
            }
        }

        protected virtual void CreateStatisticsFile() { }

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
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (_collisionMap == null)
            {
                return;
            }

            var height = (_collisionMap.HeightInTiles + 1) / 2;
            var width = (_collisionMap.WidthInTiles + 1) / 2;
            Gizmos.color = Color.blue;
            for (float x = -width; x < width; x += 0.5f)
            {
                Gizmos.DrawLine(new Vector3(x, -width, -0.01f), new Vector3(x, width, -0.01f));
            }

            for (float y = -height; y < height; y += 0.5f)
            {
                Gizmos.DrawLine(new Vector3(-height, y, -0.01f), new Vector3(height, y, -0.01f));
            }

            Gizmos.color = Color.red;
            for (float x = -width; x < width; x += 1)
            {
                Gizmos.DrawLine(new Vector3(x, -width, -0.01f), new Vector3(x, width, -0.01f));
            }

            for (float y = -height; y < height; y += 1f)
            {
                Gizmos.DrawLine(new Vector3(-height, y, -0.01f), new Vector3(height, y, -0.01f));
            }

            Gizmos.color = Color.black;
            Gizmos.DrawLine(new Vector3(0, -width, -0.01f), new Vector3(0, width, -0.01f));
            Gizmos.DrawLine(new Vector3(-height, 0, -0.01f), new Vector3(height, 0, -0.01f));
        }
    }
}