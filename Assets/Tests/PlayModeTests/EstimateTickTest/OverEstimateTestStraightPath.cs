using System;
using System.Collections;

using Maes.Robot;
using Maes.Simulation.Exploration;
using Maes.UI;

using NUnit.Framework;

using UnityEngine;

namespace Tests.PlayModeTests.EstimateTickTest
{
    using MySimulationScenario = ExplorationSimulationScenario;
    using MySimulator = ExplorationSimulator;

    [TestFixture(0.5f)]
    [TestFixture(1.0f)]
    [TestFixture(1.5f)]
    public class OverEstimateTestStraightPath
    {
        private const int RandomSeed = 123;
        private const float DiffRatio = 0.25f;
        private MySimulator _maes;
        private MoveToTargetTileAlgorithm _testAlgorithm;
        private ExplorationSimulation _simulationBase;
        private MonaRobot _robot;
        private readonly RobotConstraints _robotConstraints;

        public OverEstimateTestStraightPath(float relativeMoveSpeed)
        {
            _robotConstraints = new RobotConstraints(relativeMoveSpeed: relativeMoveSpeed, mapKnown: true);
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
                        var algorithm = new MoveToTargetTileAlgorithm();
                        _testAlgorithm = algorithm;
                        return algorithm;
                    }));

            _maes = new MySimulator();
            _maes.EnqueueScenario(testingScenario);
            _simulationBase = _maes.SimulationManager.CurrentSimulation ?? throw new InvalidOperationException("CurrentSimulation is null");
            _robot = _simulationBase.Robots[0];
        }

        [TearDown]
        public void ClearSimulator()
        {
            _maes.Destroy();
        }


        [Test(ExpectedResult = null)]
        public IEnumerator EstimateTicksToTile_TestOverEstimate_StraightPath()
        {
            var robotCurrentPosition = _testAlgorithm.Controller.SlamMap.CoarseMap.GetCurrentPosition();
            var targetTile = robotCurrentPosition + new Vector2Int(10, 0);
            var expectedEstimatedTicks = _robot.Controller.OverEstimateTimeToTarget(targetTile);
            if (expectedEstimatedTicks == null)
            {
                Assert.Fail("Not able to make a route to the target tile");
            }

            _testAlgorithm.TargetTile = targetTile;

            _maes.PressPlayButton();
            _maes.SimulationManager.AttemptSetPlayState(SimulationPlayState.FastAsPossible);

            while (!_testAlgorithm.TargetReached && _testAlgorithm.Tick < 10000)
            {
                yield return null;
            }

            var actualTicks = _testAlgorithm.Tick;
            Assert.GreaterOrEqual(expectedEstimatedTicks.Value - actualTicks, 0, "The algorithm does not overestimate the time to reach the target tile");
            Debug.Log("Over estimate with " + (expectedEstimatedTicks.Value - actualTicks) + " ticks");
        }
    }
}