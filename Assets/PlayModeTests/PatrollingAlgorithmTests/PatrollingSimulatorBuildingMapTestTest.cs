using System.Collections;

using Maes.Algorithms.Patrolling;
using Maes.Map.MapGen;
using Maes.Simulation;
using Maes.Simulation.SimulationScenarios;

using NUnit.Framework;

using UnityEngine;

namespace PlayModeTests
{
    using MySimulationScenario = PatrollingSimulationScenario;
    using MySimulator = PatrollingSimulator;

    public class PatrollingSimulatorBuildingMapTestTest : MonoBehaviour
    {
        private MySimulator _maes;
        private PatrollingSimulation _simulation;

        private void InitializeTestingSimulator()
        {
            _maes = new MySimulator();
            var random = new System.Random(12345);
            var mapSize = 50;

            var constraintName = "Global";
            var robotConstraints = StandardTestingConfiguration.GlobalRobotConstraints();
            var mapConfig = new BuildingMapConfig(123, widthInTiles: mapSize, heightInTiles: mapSize, brokenCollisionMap: false);
            var algoName = "conscientious_reactive";
            const int robotCount = 1;
            var spawningPosList = ScenarioBuilderUtilities.GenerateRandomSpawningPositions(random, mapSize, robotCount);

            _maes.EnqueueScenario(
                new MySimulationScenario(
                    seed: 123,
                    totalCycles: 4,
                    stopAfterDiff: false,
                    robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsAtPositions(
                        collisionMap: buildingConfig,
                        seed: 123,
                        numberOfRobots: robotCount,
                        spawnPositions: spawningPosList,
                        createAlgorithmDelegate: (_) => new ConscientiousReactiveAlgorithm()),
                    mapSpawner: generator => generator.GenerateMap(mapConfig),
                    robotConstraints: robotConstraints,
                    statisticsFileName: $"{algoName}-seed-{mapConfig.RandomSeed}-size-{mapSize}-comms-{constraintName}-robots-{robotCount}-RandomRobotSpawn")
            );
            _simulation = _maes.SimulationManager.CurrentSimulation;
        }

        [TearDown]
        public void ClearSimulator()
        {
            _maes.Destroy();
        }

        [Test(ExpectedResult = null)]
        public IEnumerator TestConscientiousReactiveBuildingMap()
        {
            InitializeTestingSimulator();
            _maes.SimulationManager.AttemptSetPlayState(Maes.UI.SimulationPlayState.FastAsPossible);
            while (!_simulation.HasFinishedSim())
            {
                yield return null;
            }
            Assert.True(_simulation.HasFinishedSim());
        }
    }
}