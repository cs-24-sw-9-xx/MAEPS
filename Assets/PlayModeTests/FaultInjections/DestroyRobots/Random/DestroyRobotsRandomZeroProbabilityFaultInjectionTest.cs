using System;
using System.Collections;

using Maes.FaultInjections.DestroyRobots;
using Maes.Robot;
using Maes.Simulation;
using Maes.Simulation.SimulationScenarios;
using Maes.UI;

using NUnit.Framework;

using UnityEngine;

namespace PlayModeTests.FaultInjections.DestroyRobots.Random
{
    using MySimulationScenario = ExplorationSimulationScenario;
    using MySimulator = ExplorationSimulator;

    /*
     * This test class checks no robots are destroyed when the probability of destroying a robot is 0,
     * even though the number of robots to destroy is the number of robots to spawn
     */
    [TestFixture(1)]
    [TestFixture(2)]
    [TestFixture(5)]
    [TestFixture(10)]
    public class DestroyRobotsRandomZeroProbabilityFaultInjectionTest
    {
        private const float Probability = 0;
        private const int RandomSeed = 123;
        private const int InvokeEvery = 1;
        private MySimulator _maes;
        private ExplorationSimulation _simulationBase;
        private readonly int _robotsToSpawn;

        public DestroyRobotsRandomZeroProbabilityFaultInjectionTest(int robotsToSpawn)
        {
            _robotsToSpawn = robotsToSpawn;
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
                faultInjection: new DestroyRobotsRandomFaultInjection(RandomSeed, Probability, InvokeEvery, _robotsToSpawn));

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
        public IEnumerator DestroyRobotRandom_ZeroDestroyedEnd()
        {
            var expectedRobotsAtEnd = _simulationBase.Robots.Count;

            _maes.PressPlayButton();
            _maes.SimulationManager.AttemptSetPlayState(SimulationPlayState.FastAsPossible);

            while (_simulationBase.SimulatedLogicTicks < (_robotsToSpawn + 100))
            {
                yield return null;
            }

            // Assert that no robots are destroyed
            Assert.AreEqual(expectedRobotsAtEnd, _simulationBase.RobotSpawner.transform.childCount);
        }
    }
}