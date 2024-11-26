using System;
using System.Collections;

using Maes.Map;
using Maes.Map.MapGen;
using Maes.Robot;
using Maes.Simulation;
using Maes.Simulation.SimulationScenarios;
using Maes.UI;

using NUnit.Framework;

using UnityEngine;

namespace PlayModeTests.EstimateTickTest
{
    using MySimulationScenario = ExplorationSimulationScenario;
    using MySimulator = ExplorationSimulator;

    [TestFixture(0.5f, 5, 5, -42)]
    [TestFixture(0.5f, 10, 10, -39)]
    [TestFixture(0.5f, 20, 20, -31)]
    [TestFixture(0.5f, 15, 15, -31)]

    [TestFixture(1.0f, 5, 5, -17)]
    [TestFixture(1.0f, 10, 10, -27)]
    [TestFixture(1.0f, 20, 20, -29)]
    [TestFixture(1.0f, 15, 15, -23)]

    [TestFixture(1.5f, 5, 5, -36)]
    [TestFixture(1.5f, 10, 10, -43)]
    [TestFixture(1.5f, 15, 15, -39)]

    public class EstimateTestTurnsPath
    {
        private const int RandomSeed = 123;
        private MySimulator _maes;
        private MoveToTargetTileAlgorithm _testAlgorithm;
        private ExplorationSimulation _simulationBase;
        private SimulationMap<Tile> _map;
        private readonly RobotConstraints _robotConstraints;
        private readonly int _expectedDifference;
        private readonly Vector2Int _targetTile;
        private MonaRobot _robot;

        public EstimateTestTurnsPath(float relativeMoveSpeed, int x, int y, int expectedDifference)
        {
            _robotConstraints = new RobotConstraints(relativeMoveSpeed: relativeMoveSpeed, mapKnown: true);
            _targetTile = new Vector2Int(x, y);
            _expectedDifference = expectedDifference;
        }

        [SetUp]
        public void InitializeTestingSimulator()
        {
            const int randomSeed = 12345;
            var random = new System.Random(randomSeed);
            const int size = 75;
            var randVal = random.Next(0, 1000000);
            var mapConfig = new BuildingMapConfig(randVal, widthInTiles: size, heightInTiles: size);
            var testingScenario = new MySimulationScenario(RandomSeed,
                mapSpawner: generator => generator.GenerateMap(mapConfig),
                hasFinishedSim: _ => false,
                robotConstraints: _robotConstraints,
                robotSpawner: (map, spawner) => spawner.SpawnRobotsTogether(map, RandomSeed, 1,
                    Vector2Int.zero, _ =>
                    {
                        var algorithm = new MoveToTargetTileAlgorithm();
                        _testAlgorithm = algorithm;
                        return algorithm;
                    }));

            _maes = new MySimulator();
            _maes.EnqueueScenario(testingScenario);
            _simulationBase = _maes.SimulationManager.CurrentSimulation ?? throw new InvalidOperationException("CurrentSimulation is null");
            _map = _simulationBase.GetCollisionMap();
            _robot = _simulationBase.Robots[0];
        }

        [TearDown]
        public void ClearSimulator()
        {
            _maes.Destroy();
        }


        [Test(ExpectedResult = null)]
        public IEnumerator EstimateTicksToTile_TurnsPath()
        {
            var expectedEstimatedTicks = _robot.Controller.EstimateTimeToTarget(_targetTile);
            if (expectedEstimatedTicks == null)
            {
                Assert.Fail("Not able to make a route to the target tile");
            }

            _testAlgorithm.TargetTile = _targetTile;

            _maes.PressPlayButton();
            _maes.SimulationManager.AttemptSetPlayState(SimulationPlayState.FastAsPossible);

            while (!_testAlgorithm.TargetReached && _testAlgorithm.Tick < 10000)
            {
                yield return null;
            }

            var actualTicks = _testAlgorithm.Tick;

            Assert.AreEqual(_expectedDifference, expectedEstimatedTicks.Value - actualTicks);
        }
    }
}