using System;
using System.Collections;

using Maes.Map.Generators;
using Maes.Robot;
using Maes.Simulation.Exploration;
using Maes.UI;

using NUnit.Framework;

using UnityEngine;

namespace Tests.PlayModeTests.EstimateTickTest
{
    using MySimulationScenario = ExplorationSimulationScenario;
    using MySimulator = ExplorationSimulator;

    [TestFixture(0.5f, 5, 5)]
    [TestFixture(0.5f, 10, 10)]
    [TestFixture(0.5f, 20, 20)]
    [TestFixture(0.5f, 15, 15)]

    [TestFixture(1.0f, 5, 5)]
    [TestFixture(1.0f, 10, 10)]
    [TestFixture(1.0f, 20, 20)]
    [TestFixture(1.0f, 15, 15)]

    [TestFixture(1.5f, 5, 5)]
    [TestFixture(1.5f, 10, 10)]
    [TestFixture(1.5f, 15, 15)]

    public class EstimateTestTurnsPath
    {
        private const int RandomSeed = 123;
        private const float DiffRatio = 0.23f;
        private MySimulator _maes;
        private MoveToTargetTileAlgorithm _testAlgorithm;
        private ExplorationSimulation _simulationBase;
        private readonly RobotConstraints _robotConstraints;
        private readonly Vector2Int _targetTile;
        private MonaRobot _robot;

        public EstimateTestTurnsPath(float relativeMoveSpeed, int x, int y)
        {
            _robotConstraints = new RobotConstraints(relativeMoveSpeed: relativeMoveSpeed, mapKnown: true, slamRayTraceRange: 0);
            _targetTile = new Vector2Int(x, y);
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
                hasFinishedSim: MySimulationScenario.InfallibleToFallibleSimulationEndCriteria(_ => false),
                robotConstraints: _robotConstraints,
                robotSpawner: (map, spawner) => spawner.SpawnRobotsTogether(map, RandomSeed, 1,
                    Vector2Int.zero, _ =>
                    {
                        var algorithm = new MoveToTargetTileAlgorithm(_targetTile);
                        _testAlgorithm = algorithm;
                        return algorithm;
                    }));

            _maes = new MySimulator(new[] { testingScenario });
            _simulationBase = _maes.SimulationManager.CurrentSimulation ?? throw new InvalidOperationException("CurrentSimulation is null");
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
            if (_testAlgorithm.ExpectedEstimatedTicks == null)
            {
                Assert.Fail("Not able to make a route to the target tile");
            }

            _maes.PressPlayButton();
            _maes.SimulationManager.AttemptSetPlayState(SimulationPlayState.FastAsPossible);

            while (!_testAlgorithm.TargetReached && _testAlgorithm.Tick < 10000)
            {
                yield return null;
            }
            Assert.Less(_testAlgorithm.Tick, 10000, "The algorithm didn't reach the target tile before timeout");

            var actualTicks = _testAlgorithm.Tick;

            var diff = Mathf.Abs((float)(actualTicks - _testAlgorithm.ExpectedEstimatedTicks.Value) / _testAlgorithm.ExpectedEstimatedTicks.Value);
            Assert.LessOrEqual(diff, DiffRatio);
        }
    }
}