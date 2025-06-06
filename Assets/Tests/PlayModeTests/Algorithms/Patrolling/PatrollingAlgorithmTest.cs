using System.Collections;

using Maes.Algorithms.Patrolling;
using Maes.Algorithms.Patrolling.HeuristicConscientiousReactive;
using Maes.Map.Generators;
using Maes.Map.Generators.Patrolling.Waypoints.Generators;
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
        private const int RobotCount = 1;

        private PatrollingSimulation EnqueueCaveMapScenario(PatrollingAlgorithm algorithm, int seed = Seed)
        {
            var random = new System.Random(seed);
            var mapSize = 50;

            var constraintName = "Global";
            var robotConstraints = StandardTestingConfiguration.GlobalRobotConstraints();
            var mapConfig = new CaveMapConfig(seed, widthInTiles: mapSize, heightInTiles: mapSize, brokenCollisionMap: false);
            var algoName = algorithm.AlgorithmName;
            var spawningPosList = ScenarioBuilderUtilities.GenerateRandomSpawningPositions(random, mapSize, RobotCount);

            var scenarios = new[] {(
                new MySimulationScenario(
                    seed: seed,
                    totalCycles: 4,
                    stopAfterDiff: false,
                    robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsAtPositions(
                        collisionMap: buildingConfig,
                        seed: seed,
                        numberOfRobots: RobotCount,
                        spawnPositions: spawningPosList,
                        createAlgorithmDelegate: (_) => algorithm),
                    mapSpawner: generator => generator.GenerateMap(mapConfig),
                    robotConstraints: robotConstraints, statisticsFileName: $"{algoName}-seed-{mapConfig.RandomSeed}-size-{mapSize}-comms-{constraintName}-robots-{RobotCount}-RandomRobotSpawn",
                    patrollingMapFactory: map => AllWaypointConnectedGenerator.MakePatrollingMap(map))
            )};

            _maes = new MySimulator(scenarios);

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
            var spawningPosList = ScenarioBuilderUtilities.GenerateRandomSpawningPositions(random, mapSize, RobotCount);

            var scenarios = new[] {(
                new MySimulationScenario(
                    seed: seed,
                    totalCycles: 4,
                    stopAfterDiff: false,
                    robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsAtPositions(
                        collisionMap: buildingConfig,
                        seed: seed,
                        numberOfRobots: RobotCount,
                        spawnPositions: spawningPosList,
                        createAlgorithmDelegate: (_) => algorithm),
                    mapSpawner: generator => generator.GenerateMap(mapConfig),
                    robotConstraints: robotConstraints, statisticsFileName: $"{algoName}-seed-{mapConfig.RandomSeed}-size-{mapSize}-comms-{constraintName}-robots-{RobotCount}-RandomRobotSpawn",
                    patrollingMapFactory: map => AllWaypointConnectedGenerator.MakePatrollingMap(map))
            )};

            _maes = new MySimulator(scenarios);

            return _maes.SimulationManager.CurrentSimulation;
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
            var simulation = EnqueueCaveMapScenario(new CognitiveCoordinatedGlobal());
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
            var simulation = EnqueueBuildingMapScenario(new CognitiveCoordinatedGlobal());
            _maes.SimulationManager.AttemptSetPlayState(Maes.UI.SimulationPlayState.FastAsPossible);
            while (!simulation.HasFinishedSim() && simulation.SimulatedLogicTicks < MaxSimulatedLogicTicks)
            {
                yield return null;
            }
            Assert.True(simulation.HasFinishedSim(), $"Simulation did not finish under {MaxSimulatedLogicTicks} ticks, indicating the robot is stuck.");
        }

        [Test(ExpectedResult = null)]
        public IEnumerator Test_CognitiveCoordinatedVirtualStigmergy_CaveMap()
        {
            var simulation = EnqueueCaveMapScenario(new CognitiveCoordinatedVirtualStigmergy(RobotCount));
            _maes.SimulationManager.AttemptSetPlayState(Maes.UI.SimulationPlayState.FastAsPossible);
            while (!simulation.HasFinishedSim() && simulation.SimulatedLogicTicks < MaxSimulatedLogicTicks)
            {
                yield return null;
            }
            Assert.True(simulation.HasFinishedSim(), $"Simulation did not finish under {MaxSimulatedLogicTicks} ticks, indicating the robot is stuck.");
        }

        [Test(ExpectedResult = null)]
        public IEnumerator Test_CognitiveCoordinatedVirtualStigmergy_BuildingMap()
        {
            var simulation = EnqueueBuildingMapScenario(new CognitiveCoordinatedVirtualStigmergy(RobotCount));
            _maes.SimulationManager.AttemptSetPlayState(Maes.UI.SimulationPlayState.FastAsPossible);
            while (!simulation.HasFinishedSim() && simulation.SimulatedLogicTicks < MaxSimulatedLogicTicks)
            {
                yield return null;
            }
            Assert.True(simulation.HasFinishedSim(), $"Simulation did not finish under {MaxSimulatedLogicTicks} ticks, indicating the robot is stuck.");
        }

        [Test(ExpectedResult = null)]
        public IEnumerator Test_SingleCycleTSP_CaveMap()
        {
            var simulation = EnqueueCaveMapScenario(new SingleCycleTSP());
            _maes.SimulationManager.AttemptSetPlayState(Maes.UI.SimulationPlayState.FastAsPossible);
            while (!simulation.HasFinishedSim() && simulation.SimulatedLogicTicks < MaxSimulatedLogicTicks)
            {
                yield return null;
            }
            Assert.True(simulation.HasFinishedSim(), $"Simulation did not finish under {MaxSimulatedLogicTicks} ticks, indicating the robot is stuck.");
        }

        [Test(ExpectedResult = null)]
        public IEnumerator Test_SingleCycleTSP_BuildingMap()

        {
            var simulation = EnqueueCaveMapScenario(new SingleCycleTSP());
            _maes.SimulationManager.AttemptSetPlayState(Maes.UI.SimulationPlayState.FastAsPossible);
            while (!simulation.HasFinishedSim() && simulation.SimulatedLogicTicks < MaxSimulatedLogicTicks)
            {
                yield return null;
            }
            Assert.True(simulation.HasFinishedSim(), $"Simulation did not finish under {MaxSimulatedLogicTicks} ticks, indicating the robot is stuck.");
        }

        [Test(ExpectedResult = null)]
        public IEnumerator Test_SingleCycleChristofides_CaveMap()
        {
            var simulation = EnqueueCaveMapScenario(new SingleCycleChristofides());
            _maes.SimulationManager.AttemptSetPlayState(Maes.UI.SimulationPlayState.FastAsPossible);
            while (!simulation.HasFinishedSim() && simulation.SimulatedLogicTicks < MaxSimulatedLogicTicks)
            {
                yield return null;
            }
            Assert.True(simulation.HasFinishedSim(), $"Simulation did not finish under {MaxSimulatedLogicTicks} ticks, indicating the robot is stuck.");
        }

        [Test(ExpectedResult = null)]
        public IEnumerator Test_SingleCycleChristofides_BuildingMap()

        {
            var simulation = EnqueueCaveMapScenario(new SingleCycleChristofides());
            _maes.SimulationManager.AttemptSetPlayState(Maes.UI.SimulationPlayState.FastAsPossible);
            while (!simulation.HasFinishedSim() && simulation.SimulatedLogicTicks < MaxSimulatedLogicTicks)
            {
                yield return null;
            }
            Assert.True(simulation.HasFinishedSim(), $"Simulation did not finish under {MaxSimulatedLogicTicks} ticks, indicating the robot is stuck.");
        }
    }
}