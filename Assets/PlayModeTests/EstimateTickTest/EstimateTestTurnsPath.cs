using System;
using System.Collections;

using Maes.Map;
using Maes.Map.MapGen;
using Maes.Robot;
using Maes.Simulation;
using Maes.Simulation.SimulationScenarios;
using Maes.UI;
using Maes.Utilities;

using NUnit.Framework;

using UnityEngine;

namespace PlayModeTests.EstimateTickTest
{
    using MySimulationScenario = ExplorationSimulationScenario;
    using MySimulator = ExplorationSimulator;

    [TestFixture(0.5f, 5, 5, 3)]
    [TestFixture(0.5f, 10, 10, 5)]
    [TestFixture(0.5f, 20, 20, 5)]
    [TestFixture(0.5f, 15, 15, 5)]

    [TestFixture(1.0f, 5, 5, -4)]
    [TestFixture(1.0f, 10, 10, -6)]
    [TestFixture(1.0f, 20, 20, 0)]
    [TestFixture(1.0f, 15, 15, -2)]

    [TestFixture(1.5f, 5, 5, 0)]
    [TestFixture(1.5f, 10, 10, 1)]
    [TestFixture(1.5f, 15, 15, -4)]

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
                        var algorithm = new TestToTargetTileAlgorithm();
                        _testAlgorithm = algorithm;
                        return algorithm;
                    }));

            _maes = new MySimulator();
            _maes.EnqueueScenario(testingScenario);
            _simulationBase = _maes.SimulationManager.CurrentSimulation ?? throw new InvalidOperationException("CurrentSimulation is null");
            _map = _simulationBase.GetCollisionMap();
        }

        [TearDown]
        public void ClearSimulator()
        {
            _maes.Destroy();
        }


        [Test(ExpectedResult = null)]
        public IEnumerator EstimateTicksToTile_TurnsPath()
        {
            var robotCurrentPosition = _testAlgorithm.Controller.SlamMap.CoarseMap.GetCurrentPosition();
            var expectedEstimatedTicks = _simulationBase.gameObject.AddComponent<EstimateTickTimeCalculator>().EstimateTick(90, robotCurrentPosition, _targetTile, _map, _robotConstraints, RandomSeed);

            _testAlgorithm.TargetTile = _targetTile;

            _maes.PressPlayButton();
            _maes.SimulationManager.AttemptSetPlayState(SimulationPlayState.FastAsPossible);

            while (!_testAlgorithm.TargetReached && _testAlgorithm.Tick < 10000)
            {
                yield return null;
            }

            var actualTicks = _testAlgorithm.Tick;

            Assert.AreEqual(_expectedDifference, expectedEstimatedTicks - actualTicks);
        }
    }
}