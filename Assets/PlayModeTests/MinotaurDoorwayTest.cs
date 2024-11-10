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

using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Maes.Robot;
using Maes.ExplorationAlgorithm.Minotaur;
using Maes.Utilities.Files;
using Maes.Simulation;
using Maes.Simulation.SimulationScenarios;

using NUnit.Framework;

using UnityEngine;

namespace PlayModeTests
{
    using MySimulator = ExplorationSimulator;
    using MySimulationScenario = ExplorationSimulationScenario;
    public class MinotaurDoorwayMock : MinotaurAlgorithm
    {
        public MinotaurDoorwayMock(RobotConstraints robotConstraints, int doorWidth) : base(robotConstraints, doorWidth)
        { }

        public IReadOnlyList<Doorway> GetDoorways()
        {
            return _doorways;
        }
    }


    public class MinotaurDoorwayTest
    {
        private const int RandomSeed = 123;
        private MySimulator _maes;
        private ExplorationSimulation _explorationSimulation;
        private List<MinotaurDoorwayMock> _minotaurs = new();

        [TearDown]
        public void ClearSimulator()
        {
            _maes.Destroy();
            _minotaurs.Clear();
        }
        private void InitSimulator(string mapName, List<Vector2Int> robotSpawnPositions)
        {
            var constraints = new RobotConstraints(
                senseNearbyAgentsRange: 5f,
                senseNearbyAgentsBlockedByWalls: true,
                automaticallyUpdateSlam: true,
                slamUpdateIntervalInTicks: 1,
                slamSynchronizeIntervalInTicks: 10,
                slamPositionInaccuracy: 0.2f,
                distributeSlam: false,
                environmentTagReadRange: 4.0f,
                slamRayTraceRange: 4f,
                relativeMoveSpeed: 1f,
                agentRelativeSize: 0.6f,
                calculateSignalTransmissionProbability: (distanceTravelled, distanceThroughWalls) =>
                {
                    // Blocked by walls
                    if (distanceThroughWalls > 0)
                    {
                        return false;
                    }
                    // Max distance 15.0f
                    else if (15.0f < distanceTravelled)
                    {
                        return false;
                    }

                    return true;
                }
            );
            var map = PgmMapFileLoader.LoadMapFromFileIfPresent(mapName + ".pgm");
            var testingScenario = new MySimulationScenario(RandomSeed,
                mapSpawner: generator => generator.GenerateMap(map, RandomSeed),
                hasFinishedSim: _ => false,
                robotConstraints: constraints,
                robotSpawner: (map, spawner) => spawner.SpawnRobotsAtPositions(robotSpawnPositions, map, RandomSeed, 1,
                    _ =>
                    {
                        var algorithm = new MinotaurDoorwayMock(constraints, 4);
                        _minotaurs.Add(algorithm);
                        return algorithm;
                    }));

            _maes = new MySimulator();
            _maes.EnqueueScenario(testingScenario);
            _explorationSimulation = _maes.SimulationManager.CurrentSimulation;
        }

        private IEnumerator AssertDoorsWhenFinished(int doorAmount)
        {
            if (_explorationSimulation.SimulatedLogicTicks > 36000)
                yield return false;
            while (_explorationSimulation.ExplorationTracker.ExploredProportion < 0.999f)
            {
                yield return null;
            }

            Assert.AreEqual(doorAmount, _minotaurs.First().GetDoorways().Count);
        }

        [Test(ExpectedResult = null)]
        public IEnumerator BlankMap()
        {
            InitSimulator("blank", new List<Vector2Int> { new Vector2Int(0, 0) });

            _maes.PressPlayButton();
            _maes.SimulationManager.AttemptSetPlayState(Maes.UI.SimulationPlayState.FastAsPossible);
            return AssertDoorsWhenFinished(0);
        }

        [Test(ExpectedResult = null)]
        public IEnumerator SingleDoorway()
        {
            InitSimulator("doorway", new List<Vector2Int> { new Vector2Int(0, 0) });

            _maes.PressPlayButton();
            _maes.SimulationManager.AttemptSetPlayState(Maes.UI.SimulationPlayState.FastAsPossible);
            return AssertDoorsWhenFinished(1);
        }

        [Test(ExpectedResult = null)]
        public IEnumerator Corner()
        {
            InitSimulator("doorway_corner", new List<Vector2Int> { new Vector2Int(0, 0) });

            _maes.PressPlayButton();
            _maes.SimulationManager.AttemptSetPlayState(Maes.UI.SimulationPlayState.FastAsPossible);
            return AssertDoorsWhenFinished(1);
        }

        [Test(ExpectedResult = null)]
        public IEnumerator Hallway()
        {
            InitSimulator("hallway", new List<Vector2Int> { new Vector2Int(0, -24) });

            _maes.PressPlayButton();
            _maes.SimulationManager.AttemptSetPlayState(Maes.UI.SimulationPlayState.FastAsPossible);
            return AssertDoorsWhenFinished(1);
        }
    }
}