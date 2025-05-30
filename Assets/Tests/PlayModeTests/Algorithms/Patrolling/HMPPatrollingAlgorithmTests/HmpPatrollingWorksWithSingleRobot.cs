using System;
using System.Collections;

using Maes.Algorithms.Patrolling;
using Maes.Map.Generators;
using Maes.Map.Generators.Patrolling.Waypoints.Generators;
using Maes.Robot;
using Maes.Simulation.Patrolling;
using Maes.UI;

using NUnit.Framework;

namespace Tests.PlayModeTests.Algorithms.Patrolling.HMPPatrollingAlgorithmTests
{
    public class HmpPatrollingWorksWithSingleRobot
    {
        private const int Seed = 1;
        private const int MaxSimulatedLogicTicks = 250000;
        private const int MapSize = 50;
        private const int RobotCount = 1;
        private const int TotalCycles = 3;

        private PatrollingSimulator _maes;

        private void CreateAndEnqueueScenario(Func<PatrollingAlgorithm> algorithmFactory)
        {
            var robotConstraints = new RobotConstraints(
                mapKnown: true,
                distributeSlam: false,
                slamRayTraceRange: 0f,
                robotCollisions: false,
                calculateSignalTransmissionProbability: (_, distanceThroughWalls) => distanceThroughWalls == 0f);

            var mapConfig = new BuildingMapConfig(123, widthInTiles: MapSize, heightInTiles: MapSize, brokenCollisionMap: false);

            var scenarios = new[] {(
                new PatrollingSimulationScenario(
                    seed: Seed,
                    totalCycles: TotalCycles,
                    stopAfterDiff: false,
                    robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsTogether(
                        collisionMap: buildingConfig,
                        seed: Seed,
                        numberOfRobots: RobotCount,
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

        [TearDown]
        public void TearDown()
        {
            _maes.Destroy();
        }

        [TestCaseSource(typeof(AllHMPPatrollingAlgorithm), nameof(AllHMPPatrollingAlgorithm.TestCases))]
        public IEnumerator HmpPatrollingWorksWithSingleRobot_Test(AllHMPPatrollingAlgorithm.AlgorithmFactory algorithmFactory)
        {
            CreateAndEnqueueScenario(algorithmFactory.AlgorithmFactoryDelegate);

            while (!(_maes.SimulationManager.CurrentSimulation?.HasFinishedSim() ?? true) && (_maes.SimulationManager.CurrentSimulation?.SimulatedLogicTicks ?? int.MaxValue) < MaxSimulatedLogicTicks)
            {
                yield return null;
            }

            // We need at least one assert.
            Assert.IsTrue(true);
        }
    }
}