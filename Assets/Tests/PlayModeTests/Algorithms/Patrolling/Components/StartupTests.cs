using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Maes.Algorithms.Patrolling;
using Maes.Algorithms.Patrolling.Components;
using Maes.Map;
using Maes.Robot;
using Maes.Simulation.Patrolling;
using Maes.UI;
using Maes.Utilities;

using NUnit.Framework;

using UnityEngine;

namespace Tests.PlayModeTests.Algorithms.Patrolling.Components
{
    public class StartupTests : MonoBehaviour
    {
        private PatrollingSimulator _simulator;

        private List<TestingAlgorithm> _algorithms;

        [SetUp]
        public void Setup()
        {
            _simulator = new PatrollingSimulator();
            _algorithms = new List<TestingAlgorithm>();
        }

        [TearDown]
        public void TearDown()
        {
            _simulator.Destroy();
            _algorithms.Clear();
        }

        [Test(ExpectedResult = null)]
        public IEnumerator TestComponent()
        {
            EnqueueScenario(EditModeTests.Utilities.Utilities.CreateEmptyBitmap(16, 16), new Vector2Int(1, 8), new Vector2Int(15, 8));
            _simulator.SimulationManager.AttemptSetPlayState(SimulationPlayState.FastAsPossible);

            // This waits an unknown amount of ticks
            yield return null;

            // Wait for the messages to be sent
            while (_simulator.SimulationManager.CurrentSimulation!.SimulatedLogicTicks < 3)
            {
                yield return null;
            }

            var robot0 = _algorithms[0];
            var robot1 = _algorithms[1];

            Assert.That(robot0.RobotController.Id, Is.EqualTo(0));

            Assert.That(robot0.StartupComponent.DiscoveredRobots, Has.Count.EqualTo(2));
            Assert.That(robot0.StartupComponent.DiscoveredRobots, Does.Contain(0));
            Assert.That(robot0.StartupComponent.DiscoveredRobots, Does.Contain(1));

            Assert.That(robot1.StartupComponent.DiscoveredRobots, Has.Count.EqualTo(2));
            Assert.That(robot1.StartupComponent.DiscoveredRobots, Does.Contain(0));
            Assert.That(robot1.StartupComponent.DiscoveredRobots, Does.Contain(1));

            Assert.That(robot0.StartupComponent.Message, Is.EqualTo("42"));
            Assert.That(robot1.StartupComponent.Message, Is.EqualTo("42"));
        }


        private void EnqueueScenario(Bitmap bitmap, params Vector2Int[] robotPositions)
        {
            var tilemap = Utilities.BitmapToTilemap(bitmap);

            var robotSpawnPositions = robotPositions.ToList();

            _simulator.EnqueueScenario(new PatrollingSimulationScenario(
                seed: 123,
                totalCycles: 4,
                stopAfterDiff: false,
                robotSpawner: (map, spawner) => spawner.SpawnRobotsAtPositions(robotSpawnPositions, map, 123,
                    robotSpawnPositions.Count,
                    _ =>
                    {
                        var algorithm = new TestingAlgorithm();
                        _algorithms.Add(algorithm);

                        return algorithm;
                    }, dependOnBrokenBehavior: false),
                mapSpawner: mapSpawner => mapSpawner.GenerateMap(tilemap, 123, brokenCollisionMap: false),
                CreateRobotConstraints(),
                patrollingMapFactory: map => new PatrollingMap(new[] { new Vertex(0, new Vector2Int(4, 4)) }, map)
            ));
        }

        private static RobotConstraints CreateRobotConstraints()
        {
            var robotConstraints = StandardTestingConfiguration.GlobalRobotConstraints();

            return new RobotConstraints(
                senseNearbyAgentsRange: robotConstraints.SenseNearbyAgentsRange,
                senseNearbyAgentsBlockedByWalls: robotConstraints.SenseNearbyAgentsBlockedByWalls,
                automaticallyUpdateSlam: robotConstraints.AutomaticallyUpdateSlam,
                slamUpdateIntervalInTicks: robotConstraints.SlamUpdateIntervalInTicks,
                slamSynchronizeIntervalInTicks: robotConstraints.SlamSynchronizeIntervalInTicks,
                slamPositionInaccuracy: 0,
                mapKnown: robotConstraints.MapKnown,
                distributeSlam: robotConstraints.DistributeSlam,
                environmentTagReadRange: robotConstraints.EnvironmentTagReadRange,
                slamRayTraceRange: robotConstraints.SlamRayTraceRange,
                slamRayTraceCount: robotConstraints.SlamRayTraceCount,
                relativeMoveSpeed: robotConstraints.RelativeMoveSpeed,
                agentRelativeSize: robotConstraints.AgentRelativeSize,
                calculateSignalTransmissionProbability: (_, _) => true,
                materialCommunication: robotConstraints.MaterialCommunication,
                frequency: robotConstraints.Frequency,
                transmitPower: robotConstraints.TransmitPower,
                receiverSensitivity: robotConstraints.ReceiverSensitivity,
                attenuationDictionary: robotConstraints.AttenuationDictionary);
        }

        private class TestingAlgorithm : PatrollingAlgorithm
        {
            public override string AlgorithmName { get; } = "StartupTests";

            public StartupComponent<string> StartupComponent { get; private set; }

            public IRobotController RobotController { get; private set; }

            protected override IComponent[] CreateComponents(IRobotController controller, PatrollingMap patrollingMap)
            {
                RobotController = controller;
                StartupComponent = new StartupComponent<string>(controller, MessageFactory);

                return new IComponent[] { StartupComponent };
            }

            private static string MessageFactory(HashSet<int> _)
            {
                return "42";
            }
        }
    }
}