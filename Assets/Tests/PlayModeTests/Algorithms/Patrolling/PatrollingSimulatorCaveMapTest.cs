using System.Collections;

using Maes.Algorithms.Patrolling;
using Maes.Map.Generators;
using Maes.Simulation.Patrolling;

using NUnit.Framework;

using UnityEngine;

namespace Tests.PlayModeTests.Algorithms.Patrolling
{
    using MySimulationScenario = PatrollingSimulationScenario;
    using MySimulator = PatrollingSimulator;

    public class PatrollingSimulatorCaveMapTest : MonoBehaviour
    {
        private MySimulator _maes;
        private PatrollingSimulation _simulation;

        private void InitializeTestingSimulator()
        {
            var random = new System.Random(12345);
            var mapSize = 50;

            var constraintName = "Global";
            var robotConstraints = StandardTestingConfiguration.GlobalRobotConstraints();
            var mapConfig = new CaveMapConfig(123, widthInTiles: mapSize, heightInTiles: mapSize, brokenCollisionMap: false);
            var algorithm = new ConscientiousReactiveAlgorithm();
            var algoName = algorithm.AlgorithmName;
            const int robotCount = 1;
            var spawningPosList = ScenarioBuilderUtilities.GenerateRandomSpawningPositions(random, mapSize, robotCount);

            var scenarios = new[] {(
                new MySimulationScenario(
                    seed: 123,
                    totalCycles: 4,
                    stopAfterDiff: false,
                    robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsAtPositions(
                        collisionMap: buildingConfig,
                        seed: 123,
                        numberOfRobots: robotCount,
                        spawnPositions: spawningPosList,
                        createAlgorithmDelegate: (_) => algorithm),
                    mapSpawner: generator => generator.GenerateMap(mapConfig),
                    robotConstraints: robotConstraints, statisticsFileName: $"{algoName}-seed-{mapConfig.RandomSeed}-size-{mapSize}-comms-{constraintName}-robots-{robotCount}-RandomRobotSpawn")
            )};

            _maes = new MySimulator(scenarios);

            _simulation = _maes.SimulationManager.CurrentSimulation;
        }

        [TearDown]
        public void ClearSimulator()
        {
            _maes.Destroy();
        }

        [Test(ExpectedResult = null)]
        public IEnumerator TestConscientiousReactiveCaveMap()
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