using System;
using System.Collections;

using Maes.FaultInjections.DestroyRobots;
using Maes.Robot;
using Maes.Simulation.Exploration;
using Maes.UI;

using NUnit.Framework;

using Tests.PlayModeTests.Algorithms.Exploration;

using UnityEngine;

namespace Tests.PlayModeTests.FaultInjections.DestroyRobots.SpecificTick
{
    using MySimulationScenario = ExplorationSimulationScenario;
    using MySimulator = ExplorationSimulator;

    /*
     * Test that robots are destroyed at specific ticks
     */
    [TestFixture]
    public class DestroyRobotsAtTicksFaultInjectionTest
    {
        public class TestCase
        {
            public readonly string Name;
            public readonly int RobotsToSpawn;
            public readonly int[] RobotsToDestroyAtTicks;
            public TestCase(string name, int robotsToSpawn, int[] robotsToDestroyAtTicks)
            {
                Name = name;
                RobotsToSpawn = robotsToSpawn;
                RobotsToDestroyAtTicks = robotsToDestroyAtTicks;
            }
        }

        private class TestCasesDestroyAtTicks
        {
            private static readonly TestCase[] Cases =
            {
                new("10 robots no ticks", 10, Array.Empty<int>()),
                new("1 robot - Destroy at tick 0", 1, new []{1}),
                new("1 robots - Destroy at tick 1", 1, new []{1}),
                new("2 robots - Destroy at ticks 5", 2, new []{5}),
                new("12 robots - Destroy at ticks 5,10", 12, new []{5, 8}),
                new("12 robots - Destroy at ticks 2,3,7,10", 12, new []{2, 3, 7, 8, 10}),
            };
            public static IEnumerable TestCases
            {
                get
                {
                    foreach (var testCase in Cases)
                    {
                        yield return new TestCaseData(testCase).Returns(null);
                    }
                }
            }
        }

        private const int RandomSeed = 123;
        private MySimulator _maes;
        private ExplorationSimulation _simulationBase;

        private void InitializeTestingSimulator(int robotsToSpawn, int[] robotsToDestroyAtTicks, string testCaseName)
        {
            var testingScenario = new MySimulationScenario(RandomSeed,
                mapSpawner: StandardTestingConfiguration.EmptyCaveMapSpawner(RandomSeed),
                hasFinishedSim: _ => false,
                robotConstraints: new RobotConstraints(mapKnown: true, slamRayTraceRange: 0),
                robotSpawner: (map, spawner) => spawner.SpawnRobotsTogether(map, RandomSeed, robotsToSpawn,
                    Vector2Int.zero, _ => new TestingAlgorithm()),
                faultInjection: new DestroyRobotsAtSpecificTickFaultInjection(RandomSeed, robotsToDestroyAtTicks));

            _maes = new MySimulator();
            _maes.EnqueueScenario(testingScenario);
            _simulationBase = _maes.SimulationManager.CurrentSimulation ?? throw new InvalidOperationException("CurrentSimulation is null");

            var tracker = new DestroyTrackerTest(_simulationBase, robotsToDestroyAtTicks, robotsToSpawn, testCaseName);

            foreach (var robot in _simulationBase.Robots)
            {
                ((TestingAlgorithm)robot.Algorithm).UpdateFunction = (tick, _) =>
                {
                    tracker.UpdateLogic(tick, _simulationBase.RobotSpawner.transform.childCount);
                };
            }
        }

        [TearDown]
        public void ClearSimulator()
        {
            _maes?.Destroy();
        }

        private class DestroyTrackerTest
        {
            private ExplorationSimulation Simulation { get; }
            private int[] RobotsToDestroyAtTicks { get; }
            private int RobotsToSpawn { get; }
            private string TestCaseName { get; }
            private int Tick { get; set; } = -1;
            private readonly int _index;

            public DestroyTrackerTest(ExplorationSimulation simulation, int[] robotsToDestroyAtTicks, int robotsToSpawn, string testCaseName)
            {
                Simulation = simulation;
                RobotsToDestroyAtTicks = robotsToDestroyAtTicks;
                RobotsToSpawn = robotsToSpawn;
                TestCaseName = testCaseName;
            }

            public void UpdateLogic(int tick, int robotCount)
            {
                if (Tick >= tick)
                {
                    return;
                }

                // Check that the robots are destroyed at the correct ticks
                var destroyed = 0;
                var recentTick = 0;
                foreach (var destroyAtTick in RobotsToDestroyAtTicks)
                {
                    if (destroyAtTick <= tick)
                    {
                        destroyed++;
                        recentTick = destroyAtTick;
                    }
                }

                if (recentTick + 5 < tick) // +5 because allows some time for the robots to be destroyed
                {
                    Assert.AreEqual(RobotsToSpawn - destroyed, robotCount, $"Simulated Logic Tick: {tick}, Destroyed: {destroyed}, Count:{Simulation.RobotSpawner.transform.childCount}, Name: {TestCaseName}");
                }

                Tick = tick;
            }
        }

        [Test(ExpectedResult = null)]
        [TestCaseSource(typeof(TestCasesDestroyAtTicks), nameof(TestCasesDestroyAtTicks.TestCases))]
        public IEnumerator DestroyRobotAtSpecificTicks(TestCase testCase)
        {
            var robotsToSpawn = testCase.RobotsToSpawn;
            var robotsToDestroyAtTicks = testCase.RobotsToDestroyAtTicks;

            InitializeTestingSimulator(robotsToSpawn, robotsToDestroyAtTicks, testCase.Name);

            var expectedNumberOfRobotsAfterDestroyed = robotsToSpawn - robotsToDestroyAtTicks.Length;

            _maes.PressPlayButton();
            _maes.SimulationManager.AttemptSetPlayState(SimulationPlayState.Play); // HACK to fix flakeyness

            var stop = robotsToDestroyAtTicks.Length == 0 ? 0 : robotsToDestroyAtTicks[^1] + 2;

            while (_simulationBase.SimulatedLogicTicks <= stop + 10)
            {
                yield return null;
            }

            // Assert that the robots are destroyed
            Assert.AreEqual(expectedNumberOfRobotsAfterDestroyed, _simulationBase.RobotSpawner.transform.childCount, $"expectedNumberOfRobotsAfterDestroyed: {expectedNumberOfRobotsAfterDestroyed}, Count:{_simulationBase.RobotSpawner.transform.childCount}, Name: {testCase.Name}");
        }
    }
}