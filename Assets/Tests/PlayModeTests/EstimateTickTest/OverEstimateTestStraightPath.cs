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
            _robotConstraints = new RobotConstraints(relativeMoveSpeed: relativeMoveSpeed, mapKnown: true, slamRayTraceRange: 0);
        }

        public void InitializeTestingSimulator(Vector2Int parameter, bool isOffset)
        {
            var testingScenario = new MySimulationScenario(RandomSeed,
                mapSpawner: StandardTestingConfiguration.EmptyCaveMapSpawner(RandomSeed),
                hasFinishedSim: MySimulationScenario.InfallibleToFallibleSimulationEndCriteria(_ => false),
                robotConstraints: _robotConstraints,
                robotSpawner: (map, spawner) => spawner.SpawnRobotsTogether(map, RandomSeed, 1,
                    Vector2Int.zero, _ =>
                    {
                        var algorithm = new MoveToTargetTileAlgorithm(parameter, isOffset);
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
        public IEnumerator EstimateTicksToTile_TestOverEstimate_StraightPath()
        {
            var offset = new Vector2Int(10, 0);
            InitializeTestingSimulator(offset, true);

            _maes.PressPlayButton();
            _maes.SimulationManager.AttemptSetPlayState(SimulationPlayState.FastAsPossible);

            while (!_testAlgorithm.TargetReached && _testAlgorithm.Tick < 10000)
            {
                yield return null;
            }

            if (_testAlgorithm.ExpectedEstimatedTicks == null)
            {
                Assert.Fail("Not able to make a route to the target tile");
            }
            Assert.Less(_testAlgorithm.Tick, 10000, "The algorithm didn't reach the target tile before timeout");

            var actualTicks = _testAlgorithm.Tick;
            Assert.GreaterOrEqual(_testAlgorithm.ExpectedEstimatedTicks.Value - actualTicks, 0, "The algorithm does not overestimate the time to reach the target tile");
            Debug.Log("Over estimate with " + (_testAlgorithm.ExpectedEstimatedTicks.Value - actualTicks) + " ticks");
        }
    }
}