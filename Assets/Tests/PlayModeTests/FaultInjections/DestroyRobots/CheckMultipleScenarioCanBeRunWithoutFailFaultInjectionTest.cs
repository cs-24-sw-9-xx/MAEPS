using System.Collections;

using Maes.FaultInjections.DestroyRobots;
using Maes.Robot;
using Maes.Simulation.Exploration;

using NUnit.Framework;

using Tests.PlayModeTests.Algorithms.Exploration;

using UnityEngine;

namespace Tests.PlayModeTests.FaultInjections.DestroyRobots
{
    using MySimulationScenario = ExplorationSimulationScenario;
    using MySimulator = ExplorationSimulator;

    /*
     * This test checks that multiple scenarios can be run without fail when enabling fault injection.
     * We are destroying robots from the simulation, it might have affect the rest of the scenarios since we are destorying before the scenario is finished.
     */
    [TestFixture(5, 3, 1)]
    [TestFixture(2, 1, 2)]
    [TestFixture(1, 0, 3)]
    [TestFixture(1, 1, 5)]
    [TestFixture(2, 2, 7)]
    public class CheckMultipleScenarioCanBeRunWithoutFailFaultInjectionTest
    {
        private const float Probability = 0.7f;
        private const int RandomSeed = 123;
        private const int InvokeEvery = 1;
        private MySimulator _maes;
        private readonly ExplorationSimulation _simulationBase;
        private readonly int _robotsToSpawn;
        private readonly int _robotsToDestroy;
        private readonly int _scenarioToRun;

        public CheckMultipleScenarioCanBeRunWithoutFailFaultInjectionTest(int robotsToSpawn, int robotsToDestroy, int scenarioToRun)
        {
            _robotsToSpawn = robotsToSpawn;
            _robotsToDestroy = robotsToDestroy;
            _scenarioToRun = scenarioToRun;
        }

        [SetUp]
        public void InitializeTestingSimulator()
        {
            _maes = new MySimulator();
            for (var i = 0; i < _scenarioToRun; i++)
            {
                var testingScenario = new MySimulationScenario(RandomSeed,
                    mapSpawner: StandardTestingConfiguration.EmptyCaveMapSpawner(RandomSeed),
                    hasFinishedSim: sim => sim.SimulatedLogicTicks > _robotsToSpawn,
                    robotConstraints: new RobotConstraints(),
                    robotSpawner: (map, spawner) => spawner.SpawnRobotsTogether(map, RandomSeed, _robotsToSpawn,
                        Vector2Int.zero, _ => new TestingAlgorithm()),
                    faultInjection: new DestroyRobotsRandomFaultInjection(RandomSeed, Probability, InvokeEvery, _robotsToDestroy));
                _maes.EnqueueScenario(testingScenario);
            }
        }

        [TearDown]
        public void ClearSimulator()
        {
            _maes.Destroy();
        }

        [Test(ExpectedResult = null)]
        public IEnumerator DestroyRobotRandom_CheckRobotsDestroyFromStartToNumberOfRobotsToDestroy()
        {
            _maes.PressPlayButton();

            // Wait until the robots are destroyed
            while (ShouldContinue())
            {
                yield return null;
            }

            // Assert that the robots are destroyed
            Assert.AreEqual(0, _maes.SimulationManager.Scenarios.Count);
        }

        private bool ShouldContinue()
        {
            if (_maes.SimulationManager.Scenarios.Count > 0)
            {
                return true;
            }

            var sim = _maes.SimulationManager.CurrentSimulation;
            return sim != null && sim.SimulatedLogicTicks <= _robotsToSpawn;
        }
    }
}