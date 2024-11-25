using System.Collections;
using System.Collections.Generic;

using Maes.Map.MapGen;
using Maes.PatrollingAlgorithms;
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

        public void InitializeTestingSimulator()
        {
            _maes = new MySimulator();
            var random = new System.Random(12345);
            var mapSize = 50;

            var constraintName = "Global";
            var robotConstraints = StandardTestingConfiguration.GlobalRobotConstraints();
            var mapConfig = new BuildingMapConfig(123, widthInTiles: mapSize, heightInTiles: mapSize);
            var algoName = "conscientious_reactive";
            const int robotCount = 1;
            var spawningPosList = new List<Vector2Int>();
            for (var amountOfSpawns = 0; amountOfSpawns < robotCount; amountOfSpawns++)
            {
                spawningPosList.Add(new Vector2Int(random.Next(0, mapSize), random.Next(0, mapSize)));
            }

            _maes.EnqueueScenario(
                new MySimulationScenario(
                    seed: 123,
                    totalCycles: 4,
                    stopAfterDiff: false,
                    mapSpawner: generator => generator.GenerateMap(mapConfig),
                    robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsAtPositions(
                        collisionMap: buildingConfig,
                        seed: 123,
                        numberOfRobots: robotCount,
                        spawnPositions: spawningPosList,
                        createAlgorithmDelegate: (_) => new ConscientiousReactiveAlgorithm()),
                    statisticsFileName: $"{algoName}-seed-{mapConfig.RandomSeed}-size-{mapSize}-comms-{constraintName}-robots-{robotCount}-SpawnTogether",
                    robotConstraints: robotConstraints
                )
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
            return RunSimulationUntilFinished();
        }

        private IEnumerator RunSimulationUntilFinished()
        {
            while(!_simulation.HasFinishedSim()){
                yield return null;
            }
            Assert.True(_simulation.HasFinishedSim());
        }
    }
}