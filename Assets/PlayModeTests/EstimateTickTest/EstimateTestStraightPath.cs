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

    [TestFixture(0.5f, -1)]
    [TestFixture(1.0f, 2)]
    [TestFixture(1.5f, -2)]
    public class EstimateTestStraightPath
    {
        private const int RandomSeed = 123;
        private MySimulator _maes;
        private MoveToTargetTileAlgorithm _testAlgorithm;
        private ExplorationSimulation _simulationBase;
        private SimulationMap<Tile> _map;
        private readonly RobotConstraints _robotConstraints;
        private readonly int _expectedDifference;

        public EstimateTestStraightPath(float relativeMoveSpeed, int expectedDifference)
        {
            _robotConstraints = new RobotConstraints(relativeMoveSpeed: relativeMoveSpeed, mapKnown: true);
            _expectedDifference = expectedDifference;
        }

        [SetUp]
        public void InitializeTestingSimulator()
        {
            var testingScenario = new MySimulationScenario(RandomSeed,
                mapSpawner: StandardTestingConfiguration.EmptyCaveMapSpawner(RandomSeed),
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
        public IEnumerator EstimateTicksToTile_StraightPath()
        {
            var robotCurrentPosition = _testAlgorithm.Controller.SlamMap.CoarseMap.GetCurrentPosition();
            var targetTile = robotCurrentPosition + new Vector2Int(10, 0);
            var expectedEstimatedTicks = _simulationBase.gameObject.AddComponent<EstimateTickTimeCalculator>().EstimateTick(90, robotCurrentPosition, targetTile, _map, _robotConstraints, RandomSeed);

            _testAlgorithm.TargetTile = targetTile;

            _maes.PressPlayButton();
            _maes.SimulationManager.AttemptSetPlayState(SimulationPlayState.FastAsPossible);

            while (!_testAlgorithm.TargetReached && _testAlgorithm.Tick < 10000)
            {
                yield return null;
            }

            var actualTicks = _testAlgorithm.Tick;

            Assert.AreEqual(expectedEstimatedTicks - actualTicks, _expectedDifference);
        }
    }
}