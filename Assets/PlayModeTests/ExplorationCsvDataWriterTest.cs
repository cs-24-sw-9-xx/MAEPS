using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

using Maes.Robot;
using Maes.Simulation;
using Maes.Simulation.SimulationScenarios;
using Maes.Statistics.Exploration;

using NUnit.Framework;

namespace PlayModeTests
{
    public class ExplorationCsvDataWriterTest
    {
        private ExplorationSimulator _maes;
        private ExplorationSimulation _explorationSimulation;
        private const string Delimiter = ",";
        private readonly CultureInfo _culture = CultureInfo.InvariantCulture;
        private const int RandomSeed = 123;
        private static readonly string Directory = Path.Join("data", "test", $"{RandomSeed}ExplorationCsvDataWriterTestFolder");

        [SetUp]
        public void SetUp()
        {
            InitSimulator();
            if (!System.IO.Directory.Exists(Directory))
            {
                System.IO.Directory.CreateDirectory(Directory);
            }

        }

        [TearDown]
        public void TearDown()
        {
            _maes.Destroy();
            if (System.IO.Directory.Exists(Directory))
            {
                System.IO.Directory.Delete(Directory, true);
            }
        }

        private void InitSimulator()
        {
            var testingScenario = new ExplorationSimulationScenario(RandomSeed,
                mapSpawner: StandardTestingConfiguration.EmptyCaveMapSpawner(RandomSeed),
                hasFinishedSim: _ => false,
                robotConstraints: new RobotConstraints(),
                robotSpawner: (map, spawner) => spawner.SpawnRobotsInBiggestRoom(map, RandomSeed, 2,
                    _ => new TestingAlgorithm()
                    ));

            _maes = new ExplorationSimulator();
            _maes.EnqueueScenario(testingScenario);
            _explorationSimulation = _maes.SimulationManager.CurrentSimulation;
        }

        [Test]
        public void ExplorationSnapshotToCsvTest()
        {
            _explorationSimulation.ExplorationTracker.snapShots.Add(new ExplorationSnapShot(1, 0.1f, 0.1f, 1f));
            _explorationSimulation.ExplorationTracker.snapShots.Add(new ExplorationSnapShot(2, 0.2f, 0.2f, 5f));
            _explorationSimulation.ExplorationTracker.snapShots.Add(new ExplorationSnapShot(3, 0.5f, 0.3f, 10f));
            _explorationSimulation.ExplorationTracker.snapShots.Add(new ExplorationSnapShot(4, 0.7f, 0.4f, 10f));
            _explorationSimulation.ExplorationTracker.snapShots.Add(new ExplorationSnapShot(5, 0.9f, 0.5f, 5f));

            _explorationSimulation.CommunicationManager.CommunicationTracker.InterconnectionSnapShot.Add(1, true);
            _explorationSimulation.CommunicationManager.CommunicationTracker.InterconnectionSnapShot.Add(3, false);
            _explorationSimulation.CommunicationManager.CommunicationTracker.InterconnectionSnapShot.Add(4, true);

            _explorationSimulation.CommunicationManager.CommunicationTracker.BiggestClusterPercentageSnapshots.Add(1, 5f);
            _explorationSimulation.CommunicationManager.CommunicationTracker.BiggestClusterPercentageSnapshots.Add(2, 10f);
            _explorationSimulation.CommunicationManager.CommunicationTracker.BiggestClusterPercentageSnapshots.Add(5, 12f);

            var expectedExplorationSnapShots = new List<ExplorationSnapShot>
            {
                new(1, 0.1f, 0.1f, 1f, true, 5f),
                new(2, 0.2f, 0.2f, 5f, null, 10f),
                new(3, 0.5f, 0.3f, 10f, false),
                new(4, 0.7f, 0.4f, 10f, true),
                new(5, 0.9f, 0.5f, 5f, null, 12f)
            };

            var filename = Path.Join(Directory, $"ExplorationSnapshotToCsvTest{DateTime.Now.Ticks}");
            var writer = new ExplorationCsvDataWriter(_explorationSimulation, filename);
            writer.CreateCsvFile(Delimiter);

            var lines = File.ReadAllLines(filename + ".csv");
            Assert.AreEqual(expectedExplorationSnapShots.Count, lines.Length - 1); // -1 because of header
            for (var i = 0; i < expectedExplorationSnapShots.Count; i++)
            {
                var line = lines[i + 1]; // +1 because of header
                var values = line.Split(Delimiter);
                var expected = expectedExplorationSnapShots[i];

                Assert.AreEqual(expected.Tick.ToString(), values[0]);
                Assert.AreEqual(expected.Explored.ToString(_culture), values[1]);
                Assert.AreEqual(expected.Covered.ToString(_culture), values[2]);
                Assert.AreEqual(expected.AverageAgentDistance.ToString(_culture), values[3]);

                if (expected.AgentsInterconnected == null)
                {
                    Assert.AreEqual("", values[4]);
                }
                else
                {
                    var expectedValue = expected.AgentsInterconnected.Value ? "1" : "0";
                    Assert.AreEqual(expectedValue, values[4]);
                }

                Assert.AreEqual(
                    expected.BiggestClusterPercentage == null ? "" : expected.BiggestClusterPercentage.ToString(),
                    values[5]);
            }
        }
    }
}