using System;
using System.Collections;

using Maes.Algorithms.Patrolling;
using Maes.Map.Generators;
using Maes.Map.Generators.Patrolling.Waypoints.Generators;
using Maes.Robot;
using Maes.Simulation.Patrolling;
using Maes.UI;

using NUnit.Framework;

using UnityEngine;

namespace Tests.PlayModeTests.Algorithms.Patrolling.HMPPatrollingAlgorithmTests
{
    public class TestFewerPartitionThanRobotsTest : MonoBehaviour
    {
        private PatrollingSimulator _maes;
        private const int Seed = 1;
        private const int MaxSimulatedLogicTicks = 250000;

        [TearDown]
        public void ClearSimulator()
        {
            _maes.Destroy();
        }

        [TestCaseSource(typeof(AllHMPPatrollingAlgorithm), nameof(AllHMPPatrollingAlgorithm.TestCases))]
        public IEnumerator HmpPatrollingWorksWithFewerPartitionThanRobots_Test(AllHMPPatrollingAlgorithm.AlgorithmFactory algorithmFactory)
        {
            CreateAndEnqueueScenario(algorithmFactory.AlgorithmFactoryDelegate, mapSize: 50, robotCount: 20);

            while (!(_maes.SimulationManager.CurrentSimulation?.HasFinishedSim() ?? true) && (_maes.SimulationManager.CurrentSimulation?.SimulatedLogicTicks ?? int.MaxValue) < MaxSimulatedLogicTicks)
            {
                yield return null;
            }

            // We need at least one assert.
            Assert.IsTrue(true);
        }

        private void CreateAndEnqueueScenario(Func<PatrollingAlgorithm> algorithmFactory, int mapSize = 50, int robotCount = 2, int totalCycles = 3)
        {
            var robotConstraints = new RobotConstraints(
                mapKnown: true,
                distributeSlam: false,
                slamRayTraceRange: 0f,
                robotCollisions: false,
                calculateSignalTransmissionProbability: (_, distanceThroughWalls) => distanceThroughWalls == 0f);

            var mapConfig = new BuildingMapConfig(Seed, widthInTiles: mapSize, heightInTiles: mapSize, brokenCollisionMap: false);

            var scenarios = new[] {(
                new PatrollingSimulationScenario(
                    seed: Seed,
                    totalCycles: totalCycles,
                    stopAfterDiff: false,
                    robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsTogether(
                        collisionMap: buildingConfig,
                        seed: Seed,
                        numberOfRobots: robotCount,
                        suggestedStartingPoint: null,
                        createAlgorithmDelegate: _ => algorithmFactory()),
                    mapSpawner: generator => generator.GenerateMap(mapConfig),
                    robotConstraints: robotConstraints,
                    statisticsFileName: $"test",
                    patrollingMapFactory: AllWaypointConnectedGenerator.MakePatrollingMap)
            )};

            _maes = new PatrollingSimulator(scenarios);
            _maes.SimulationManager.AttemptSetPlayState(SimulationPlayState.FastAsPossible);
        }
    }
}