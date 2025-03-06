using System;
using System.Collections;

using Maes.FaultInjections.DestroyRobots;
using Maes.Robot;
using Maes.Simulation.Exploration;
using Maes.UI;

using NUnit.Framework;

using Tests.PlayModeTests.Algorithms.Exploration;

using UnityEngine;

namespace Tests.PlayModeTests.FaultInjections.DestroyRobots.Random
{
    using MySimulationScenario = ExplorationSimulationScenario;
    using MySimulator = ExplorationSimulator;

    /*
     * This test checks that a robot are destroyed every tick from the start to the number of robots to destroy
     * This also check that no more robots are destroyed after the number of robots to destroy by running 10 more logic ticks
     */
    [TestFixture(1, 1)]
    [TestFixture(1, 0)]
    [TestFixture(2, 2)]
    [TestFixture(5, 3)]
    [TestFixture(10, 7)]
    public class DestroyRobotsRandomEveryTickFaultInjectionTest
    {
        private const float Probability = 1f;
        private const int RandomSeed = 123;
        private const int InvokeEvery = 1;
        private MySimulator _maes;
        private ExplorationSimulation _simulationBase;
        private readonly int _robotsToSpawn;
        private readonly int _robotsToDestroy;

        public DestroyRobotsRandomEveryTickFaultInjectionTest(int robotsToSpawn, int robotsToDestroy)
        {
            _robotsToSpawn = robotsToSpawn;
            _robotsToDestroy = robotsToDestroy;
        }

        [SetUp]
        public void InitializeTestingSimulator()
        {
            var testingScenario = new MySimulationScenario(RandomSeed,
                mapSpawner: StandardTestingConfiguration.EmptyCaveMapSpawner(RandomSeed),
                hasFinishedSim: _ => false,
                robotConstraints: new RobotConstraints(),
                robotSpawner: (map, spawner) => spawner.SpawnRobotsTogether(map, RandomSeed, _robotsToSpawn,
                    Vector2Int.zero, _ => new TestingAlgorithm()),
                faultInjection: new DestroyRobotsRandomFaultInjection(RandomSeed, Probability, InvokeEvery, _robotsToDestroy));

            _maes = new MySimulator();
            _maes.EnqueueScenario(testingScenario);
            _simulationBase = _maes.SimulationManager.CurrentSimulation ?? throw new InvalidOperationException("CurrentSimulation is null");
        }

        [TearDown]
        public void ClearSimulator()
        {
            _maes.Destroy();
        }

        [Test(ExpectedResult = null)]
        public IEnumerator DestroyRobotRandom_CheckRobotsDestroyFromStartToNumberOfRobotsToDestroy()
        {
            var expectedNumberOfRobotsAfterDestroyed = _robotsToSpawn - _robotsToDestroy;

            _maes.PressPlayButton();

            // Wait until the robots are destroyed
            while (_simulationBase.SimulatedLogicTicks <= _robotsToDestroy)
            {
                yield return null;
            }

            // Assert that the robots are destroyed
            Assert.AreEqual(expectedNumberOfRobotsAfterDestroyed, _simulationBase.RobotSpawner.transform.childCount);

            _maes.SimulationManager.AttemptSetPlayState(SimulationPlayState.FastAsPossible);
            // Run 10 more ticks to ensure that no more robots are destroyed
            while (_simulationBase.SimulatedLogicTicks <= _robotsToDestroy + 10)
            {
                yield return null;
            }

            // Assert that the robots are destroyed
            Assert.AreEqual(expectedNumberOfRobotsAfterDestroyed, _simulationBase.RobotSpawner.transform.childCount);
        }
    }
}