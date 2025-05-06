using System.Collections;

using Maes.Map;
using Maes.Robot;
using Maes.Simulation.Exploration;
using Maes.UI;

using NUnit.Framework;

using Tests.PlayModeTests.EstimateTickTest;

using UnityEngine;

namespace Tests.PlayModeTests
{
    [TestFixture(0.5f)]
    [TestFixture(1.0f)]
    [TestFixture(1.5f)]
    public class TestTickTracker
    {
        private const int RandomSeed = 123;
        private ExplorationSimulator _maes;
        private MoveToTargetTileAlgorithm _testAlgorithm;
        private readonly RobotConstraints _robotConstraints;

        public TestTickTracker(float relativeMoveSpeed)
        {
            _robotConstraints = new RobotConstraints(relativeMoveSpeed: relativeMoveSpeed, mapKnown: true, slamRayTraceRange: 0);
        }

        private void InitializeTestingSimulator(Vector2Int parameter, bool isOffset)
        {
            var testingScenario = new ExplorationSimulationScenario(RandomSeed,
                mapSpawner: StandardTestingConfiguration.EmptyCaveMapSpawner(RandomSeed),
                hasFinishedSim: ExplorationSimulationScenario.InfallibleToFallibleSimulationEndCriteria(_ => false),
                robotConstraints: _robotConstraints,
                robotSpawner: (map, spawner) => spawner.SpawnRobotsTogether(map, RandomSeed, 1,
                    Vector2Int.zero, _ =>
                    {
                        var algorithm = new MoveToTargetTileAlgorithm(parameter, isOffset);
                        _testAlgorithm = algorithm;
                        return algorithm;
                    }));

            _maes = new ExplorationSimulator(new[] { testingScenario });
        }

        [TearDown]
        public void ClearSimulator()
        {
            _maes.Destroy();
        }


        [Test(ExpectedResult = null)]
        public IEnumerator EstimateTicksToTile_StraightPath()
        {
            var offset = new Vector2Int(10, 0);
            InitializeTestingSimulator(offset, true);

            _maes.PressPlayButton();
            _maes.SimulationManager.AttemptSetPlayState(SimulationPlayState.FastAsPossible);

            while (!_testAlgorithm.TargetReached && _testAlgorithm.Tick < 10000)
            {
                yield return null;
            }

            var expectedEstimatedTicks = _testAlgorithm.Tick;
            var actualTicks = _testAlgorithm.TicksTracker.GetTicks(new Vertex(0, _testAlgorithm.StartPosition), new Vertex(1, _testAlgorithm.TargetTile));
            Assert.AreEqual(expectedEstimatedTicks, actualTicks);
        }
    }
}