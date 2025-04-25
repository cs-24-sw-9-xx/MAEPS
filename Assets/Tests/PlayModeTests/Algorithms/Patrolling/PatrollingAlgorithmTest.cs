using System.Collections;

using Maes.Algorithms.Patrolling;
using Maes.Map.Generators;
using Maes.Simulation.Patrolling;

using NUnit.Framework;

namespace Tests.PlayModeTests.Algorithms.Patrolling
{
    using MySimulationScenario = PatrollingSimulationScenario;
    using MySimulator = PatrollingSimulator;

    public class PatrollingAlgorithmTest
    {
        private MySimulator _maes;
        private const int Seed = 1;
        private const int MaxSimulatedLogicTicks = 250000;

        private PatrollingSimulation EnqueueCaveMapScenario(PatrollingAlgorithm algorithm, int seed = Seed)
        {
            var random = new System.Random(seed);
            var mapSize = 50;

            var constraintName = "Global";
            var robotConstraints = StandardTestingConfiguration.GlobalRobotConstraints();
            var mapConfig = new CaveMapConfig(seed, widthInTiles: mapSize, heightInTiles: mapSize, brokenCollisionMap: false);
            var algoName = algorithm.AlgorithmName;
            var robotCount = 1;
            var spawningPosList = ScenarioBuilderUtilities.GenerateRandomSpawningPositions(random, mapSize, robotCount);

            _maes.EnqueueScenario(
                new MySimulationScenario(
                    seed: seed,
                    totalCycles: 4,
                    stopAfterDiff: false,
                    robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsAtPositions(
                        collisionMap: buildingConfig,
                        seed: seed,
                        numberOfRobots: robotCount,
                        spawnPositions: spawningPosList,
                        createAlgorithmDelegate: (_) => algorithm),
                    mapSpawner: generator => generator.GenerateMap(mapConfig),
                    robotConstraints: robotConstraints, statisticsFileName: $"{algoName}-seed-{mapConfig.RandomSeed}-size-{mapSize}-comms-{constraintName}-robots-{robotCount}-RandomRobotSpawn")
            );
            return _maes.SimulationManager.CurrentSimulation;
        }

        private PatrollingSimulation EnqueueBuildingMapScenario(PatrollingAlgorithm algorithm, int seed = Seed)
        {
            var random = new System.Random(seed);
            var mapSize = 50;

            var constraintName = "Global";
            var robotConstraints = StandardTestingConfiguration.GlobalRobotConstraints();
            var mapConfig = new BuildingMapConfig(seed, widthInTiles: mapSize, heightInTiles: mapSize, brokenCollisionMap: false);
            var algoName = algorithm.AlgorithmName;
            var robotCount = 1;
            var spawningPosList = ScenarioBuilderUtilities.GenerateRandomSpawningPositions(random, mapSize, robotCount);

            _maes.EnqueueScenario(
                new MySimulationScenario(
                    seed: seed,
                    totalCycles: 4,
                    stopAfterDiff: false,
                    robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsAtPositions(
                        collisionMap: buildingConfig,
                        seed: seed,
                        numberOfRobots: robotCount,
                        spawnPositions: spawningPosList,
                        createAlgorithmDelegate: (_) => algorithm),
                    mapSpawner: generator => generator.GenerateMap(mapConfig),
                    robotConstraints: robotConstraints, statisticsFileName: $"{algoName}-seed-{mapConfig.RandomSeed}-size-{mapSize}-comms-{constraintName}-robots-{robotCount}-RandomRobotSpawn",
                    patrollingMapFactory: AllWaypointConnectedGenerator.MakePatrollingMap)
            );
            return _maes.SimulationManager.CurrentSimulation;
        }

        [SetUp]
        public void Setup()
        {
            _maes = new MySimulator();
        }

        [TearDown]
        public void ClearSimulator()
        {
            _maes.Destroy();
        }

        [Test(ExpectedResult = null)]
        public IEnumerator Test_ConscientiousReactive_CaveMap()
        {
            var simulation = EnqueueCaveMapScenario(new ConscientiousReactiveAlgorithm());
            _maes.SimulationManager.AttemptSetPlayState(Maes.UI.SimulationPlayState.FastAsPossible);
            while (!simulation.HasFinishedSim() && simulation.SimulatedLogicTicks < MaxSimulatedLogicTicks)
            {
                yield return null;
            }
            Assert.True(simulation.HasFinishedSim(), $"Simulation did not finish under {MaxSimulatedLogicTicks} ticks, indicating the robot is stuck.");
        }

        [Test(ExpectedResult = null)]
        public IEnumerator Test_ConscientiousReactive_BuildingMap()
        {
            var simulation = EnqueueBuildingMapScenario(new ConscientiousReactiveAlgorithm());
            _maes.SimulationManager.AttemptSetPlayState(Maes.UI.SimulationPlayState.FastAsPossible);
            while (!simulation.HasFinishedSim() && simulation.SimulatedLogicTicks < MaxSimulatedLogicTicks)
            {
                yield return null;
            }
            Assert.True(simulation.HasFinishedSim(), $"Simulation did not finish under {MaxSimulatedLogicTicks} ticks, indicating the robot is stuck.");
        }

        [Test(ExpectedResult = null)]
        public IEnumerator Test_HeuristicConscientiousReactive_CaveMap()
        {
            var simulation = EnqueueCaveMapScenario(new HeuristicConscientiousReactiveAlgorithm(Seed));
            _maes.SimulationManager.AttemptSetPlayState(Maes.UI.SimulationPlayState.FastAsPossible);
            while (!simulation.HasFinishedSim() && simulation.SimulatedLogicTicks < MaxSimulatedLogicTicks)
            {
                yield return null;
            }
            Assert.True(simulation.HasFinishedSim(), $"Simulation did not finish under {MaxSimulatedLogicTicks} ticks, indicating the robot is stuck.");
        }

        [Test(ExpectedResult = null)]
        public IEnumerator Test_HeuristicConscientiousReactive_BuildingMap()
        {
            var simulation = EnqueueBuildingMapScenario(new HeuristicConscientiousReactiveAlgorithm(Seed));
            _maes.SimulationManager.AttemptSetPlayState(Maes.UI.SimulationPlayState.FastAsPossible);
            while (!simulation.HasFinishedSim() && simulation.SimulatedLogicTicks < MaxSimulatedLogicTicks)
            {
                yield return null;
            }
            Assert.True(simulation.HasFinishedSim(), $"Simulation did not finish under {MaxSimulatedLogicTicks} ticks, indicating the robot is stuck.");
        }



        [Test(ExpectedResult = null)]
        public IEnumerator Test_RandomReactive_CaveMap()
        {
            var simulation = EnqueueCaveMapScenario(new RandomReactive(Seed));
            _maes.SimulationManager.AttemptSetPlayState(Maes.UI.SimulationPlayState.FastAsPossible);
            while (!simulation.HasFinishedSim() && simulation.SimulatedLogicTicks < MaxSimulatedLogicTicks)
            {
                yield return null;
            }
            Assert.True(simulation.HasFinishedSim(), $"Simulation did not finish under {MaxSimulatedLogicTicks} ticks, indicating the robot is stuck.");
        }

        [Test(ExpectedResult = null)]
        public IEnumerator Test_RandomReactive_BuildingMap()
        {
            var simulation = EnqueueBuildingMapScenario(new RandomReactive(Seed));
            _maes.SimulationManager.AttemptSetPlayState(Maes.UI.SimulationPlayState.FastAsPossible);
            while (!simulation.HasFinishedSim() && simulation.SimulatedLogicTicks < MaxSimulatedLogicTicks)
            {
                yield return null;
            }
            Assert.True(simulation.HasFinishedSim(), $"Simulation did not finish under {MaxSimulatedLogicTicks} ticks, indicating the robot is stuck.");
        }

        [Test(ExpectedResult = null)]
        public IEnumerator Test_CognitiveCoordinated_CaveMap()
        {
            var simulation = EnqueueCaveMapScenario(new CognitiveCoordinated());
            _maes.SimulationManager.AttemptSetPlayState(Maes.UI.SimulationPlayState.FastAsPossible);
            while (!simulation.HasFinishedSim() && simulation.SimulatedLogicTicks < MaxSimulatedLogicTicks)
            {
                yield return null;
            }
            Assert.True(simulation.HasFinishedSim(), $"Simulation did not finish under {MaxSimulatedLogicTicks} ticks, indicating the robot is stuck.");
        }

        [Test(ExpectedResult = null)]
        public IEnumerator Test_CognitiveCoordinated_BuildingMap()
        {
            var simulation = EnqueueBuildingMapScenario(new CognitiveCoordinated());
            _maes.SimulationManager.AttemptSetPlayState(Maes.UI.SimulationPlayState.FastAsPossible);
            while (!simulation.HasFinishedSim() && simulation.SimulatedLogicTicks < MaxSimulatedLogicTicks)
            {
                yield return null;
            }
            Assert.True(simulation.HasFinishedSim(), $"Simulation did not finish under {MaxSimulatedLogicTicks} ticks, indicating the robot is stuck.");
        }
    }
}