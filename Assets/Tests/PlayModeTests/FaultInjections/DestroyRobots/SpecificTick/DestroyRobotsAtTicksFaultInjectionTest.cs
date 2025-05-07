using System;
using System.Collections;
using System.Linq;

using Maes.Algorithms.Exploration;
using Maes.FaultInjections.DestroyRobots;
using Maes.Map.RobotSpawners;
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
        public sealed class TestCase
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

            public override string ToString()
            {
                return
                    $"{nameof(Name)}: {Name}, {nameof(RobotsToSpawn)}: {RobotsToSpawn}, {nameof(RobotsToDestroyAtTicks)}: {RobotsToDestroyAtTicks}";
            }
        }

        private static class TestCasesDestroyAtTicks
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
            var tracker = new DestroyTrackerTest(robotsToDestroyAtTicks, robotsToSpawn, testCaseName);

            var testingScenario = new MySimulationScenario(RandomSeed,
                mapSpawner: StandardTestingConfiguration.EmptyCaveMapSpawner(RandomSeed),
                hasFinishedSim: _ => false,
                robotConstraints: new RobotConstraints(mapKnown: true, slamRayTraceRange: 0),
                robotSpawner: (map, spawner) => spawner.SpawnRobotsTogether(map, RandomSeed, robotsToSpawn,
                    Vector2Int.zero, _ => new TestingAlgorithm((tick, _) => tracker.UpdateLogic(tick, spawner))),
                faultInjection: new DestroyRobotsAtSpecificTickFaultInjection(RandomSeed, robotsToDestroyAtTicks));

            _maes = new MySimulator(new[] { testingScenario });
            _simulationBase = _maes.SimulationManager.CurrentSimulation ?? throw new InvalidOperationException("CurrentSimulation is null");
            _maes.SimulationManager.AttemptSetPlayState(SimulationPlayState.FastAsPossible);
        }

        [TearDown]
        public void ClearSimulator()
        {
            _maes?.Destroy();
        }

        private class DestroyTrackerTest
        {
            private int[] RobotsToDestroyAtTicks { get; }
            private int RobotsToSpawn { get; }
            private string TestCaseName { get; }

            public DestroyTrackerTest(int[] robotsToDestroyAtTicks, int robotsToSpawn, string testCaseName)
            {
                RobotsToDestroyAtTicks = robotsToDestroyAtTicks;
                RobotsToSpawn = robotsToSpawn;
                TestCaseName = testCaseName;
            }

            public void UpdateLogic(int tick, RobotSpawner<IExplorationAlgorithm> spawner)
            {
                // Check that the robots are destroyed at the correct ticks
                var destroyed = RobotsToDestroyAtTicks.Count(t => t <= tick);

                var children = Enumerable.Range(0, spawner.transform.childCount)
                    .Select(spawner.transform.GetChild)
                    .Where(c => c.gameObject.activeSelf)
                    .ToList();
                var childrenNames = string.Join(", ",
                    children
                        .Select(t => t.name));
                Assert.AreEqual(RobotsToSpawn - destroyed, children.Count, $"Simulated Logic Tick: {tick}, Destroyed: {destroyed}, Count:{children.Count}, Name: {TestCaseName}, Children: {childrenNames}");
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

            var stop = robotsToDestroyAtTicks.Length == 0 ? 0 : robotsToDestroyAtTicks[^1];

            while (_simulationBase.SimulatedLogicTicks <= stop)
            {
                yield return null;
            }

            // Assert that the robots are destroyed
            Assert.AreEqual(expectedNumberOfRobotsAfterDestroyed, _simulationBase.RobotSpawner.transform.childCount);
        }
    }
}