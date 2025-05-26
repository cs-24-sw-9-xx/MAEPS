using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Maes.Algorithms.Patrolling;
using Maes.Algorithms.Patrolling.HMPPatrollingAlgorithms.FaultToleranceV2;
using Maes.Algorithms.Patrolling.HMPPatrollingAlgorithms.FaultToleranceV2.MeetingPoints;
using Maes.Algorithms.Patrolling.HMPPatrollingAlgorithms.FaultToleranceV2.TrackInfos;
using Maes.Map.Generators.Patrolling.Waypoints.Connectors;
using Maes.Robot;
using Maes.Simulation.Patrolling;

using NUnit.Framework;

using Tests.EditModeTests.Utilities.MapInterpreter.MapBuilder;

using UnityEngine;

namespace Tests.PlayModeTests.Algorithms.Patrolling.HMPPatrollingAlgorithmTests

namespace Tests.PlayModeTests.Algorithms.Patrolling.HMPPatrollingAlgorithmTests.HMPFaults
{
    public class HMPFaultV2OneRobotDestroy : MonoBehaviour
    {
        private PatrollingSimulator _simulator;
        private readonly RobotConstraints _robotConstraints = new(
            mapKnown: true,
            distributeSlam: false,
            slamRayTraceRange: 0f,
            robotCollisions: false,
            calculateSignalTransmissionProbability: (_, distanceThroughWalls) => distanceThroughWalls == 0f);


        [TearDown]
        public void ClearSimulator()
        {
            _simulator.Destroy();
        }

        private PatrollingSimulation EnqueueScenario(string mapString, int robotCount, int totalCycles, int spawnAtVertexId, int seed = 1)
        {
            var (simulationMap, patrollingMap, verticesByPartitionId) = new PartitionSimulationMapBuilder(mapString, AllConnectedWaypointConnector.ConnectVertices, '\n').Build();

            const string algoName = "HMPPatrollingAlgorithmFault";
            var mapWidth = simulationMap.WidthInTiles;

            var spawnAtVertexPosition = patrollingMap.Vertices[spawnAtVertexId].Position;

            var scenario =
                new PatrollingSimulationScenario(
                    seed: seed,
                    totalCycles: totalCycles,
                    stopAfterDiff: false,
                    robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsTogether(
                        collisionMap: buildingConfig,
                        seed: seed,
                        numberOfRobots: robotCount,
                        suggestedStartingPoint: spawnAtVertexPosition,
                        createAlgorithmDelegate: (_) => new HMPPatrollingAlgorithm()),
                    mapSpawner: _ => simulationMap,
                    patrollingMapFactory: _ => patrollingMap,
                    robotConstraints: _robotConstraints,
                    statisticsFileName: $"{algoName}-seed-{seed}-size-{mapWidth}-robots-{robotCount}-RandomRobotSpawn")
            ;

            _simulator = new PatrollingSimulator(new[] { scenario });

            return _simulator.SimulationManager.CurrentSimulation;
        }

        [Test(ExpectedResult = null)]
        public IEnumerator Test_DestroyOneRobot_TakeOverTheDestroyRobot()
        {
            var mapString = MeetAtTheMeetingPointTest.GetStringMapFromFile("TestMap2.txt");
            const int spawnAtVertexId = 6;
            const int robotCount = 2;
            const int totalCycles = 500;

            var simulation = EnqueueScenario(mapString, robotCount, totalCycles, spawnAtVertexId);
            var meetingTracker = new MeetingTracker(simulation.Robots);

            _simulator.SimulationManager.AttemptSetPlayState(Maes.UI.SimulationPlayState.FastAsPossible);

            // Waiting for the robots has their partition information
            while (simulation.SimulatedLogicTicks <= 3)
            {
                yield return null;
            }

            var meetingPoints = meetingTracker.GetMeetingPoints(simulation.Robots);

            // TODO: Make a better stop condition
            while (!simulation.HasFinishedSim())
            {
                yield return null;
            }

            meetingTracker.AssertMeetingHasBeenHeld(meetingPoints, meetingTimes);
            Assert.AreEqual(expectedNumberOfExchangeInfoAtMeetingTrackInfos, meetingTracker.NumberOfExchangeInfoAtMeetingTrackInfos, "Number of exchanged information is not equal to 2.");
        }
    }

    public class MeetingTracker
    {
        public MeetingTracker(IReadOnlyList<MonaRobot> robots)
        {
            foreach (var robot in robots)
            {
                SubscribeToMeetingEvent(robot);
            }
        }

        // Dictionary to track the meeting that has been held at the meeting point and the robots that attended at the meeting
        private readonly Dictionary<(int Id, int MeetingAtTick), List<ExchangeInfoAtMeetingTrackInfo>> _heldMeetingAtMeetingPoint = new();
        public int NumberOfExchangeInfoAtMeetingTrackInfos { get; private set; } = 0;

        private void TrackMeeting(ITrackInfo trackInfo)
        {
            var key = (trackInfo.Meeting.Vertex.Id, trackInfo.Meeting.MeetingAtTick);
            if (!_heldMeetingAtMeetingPoint.TryGetValue(key, out var attendedRobots))
            {
                attendedRobots = new List<ExchangeInfoAtMeetingTrackInfo>();
                _heldMeetingAtMeetingPoint[key] = attendedRobots;
            }

            attendedRobots.Add(trackInfo);
        }

        private void SubscribeToMeetingEvent(MonaRobot robot)
        {
            if (robot.Algorithm is HMPPatrollingAlgorithm algorithm)
            {
                algorithm.SubscribeOnTrackInfo(trackInfo =>
                {
                    TrackMeeting(meetingTrackInfo);
                    NumberOfExchangeInfoAtMeetingTrackInfos++;
                });
            }
        }

        public void AssertMeetingHasBeenHeld(IReadOnlyCollection<MeetingPoint> meetingPoints, int numberOfTimesHeld)
        {
            foreach (var meetingPoint in meetingPoints)
            {
                Assert.IsTrue(_heldMeetingAtMeetingPoint.TryGetValue(meetingPoint, out var attendedRobotsAtTick),
                    $"There has never been any meetings at {meetingPoint}");

                var i = 0;
                foreach (var (_, exchangeInfoAtMeetingTrackInfos) in attendedRobotsAtTick.OrderBy(kvp => kvp.Key))
                {
                    // Check if the meeting was held at the correct tick or before the actual meeting tick
                    var expectedMeetingWithInTick = meetingPoint.InitialMeetingAtTick + meetingPoint.MeetingAtEveryTick * i;
                    foreach (var info in exchangeInfoAtMeetingTrackInfos)
                    {
                        Assert.LessOrEqual(info.ExchangeAtTick, expectedMeetingWithInTick,
                            $"robot id {info.RobotId} has exchanged its information later then the meeting time");
                    }

                    Assert.AreEqual(meetingPoint.RobotIds.Count, exchangeInfoAtMeetingTrackInfos.Count,
                        "The number of robots that attended the meeting is not equal to the number of robots that should have attended");

                    // Check if the robots that attended the meeting are the same as the ones that should have attended
                    CollectionAssert.AreEquivalent(exchangeInfoAtMeetingTrackInfos.Select(info => info.RobotId),
                        meetingPoint.RobotIds, "the robots that attended the meeting are not the same as the ones that should have attended");

                    i++;
                }

                Assert.AreEqual(numberOfTimesHeld, attendedRobotsAtTick.Count, "The number of times the meeting was held is not equal to the number of times it should have been held");
            }
        }

        public HashSet<MeetingPoint> GetMeetingPoints(IEnumerable<MonaRobot> robots)
        {
            var meetingPoints = new HashSet<MeetingPoint>();
            foreach (var robot in robots)
            {
                var algorithm = robot.Algorithm as HMPPatrollingAlgorithm;
                Assert.IsNotNull(algorithm, "Algorithm is not of type HMPPatrollingAlgorithm.");

                foreach (var meetingPoint in algorithm.PartitionInfo.MeetingPoints)
                {
                    meetingPoints.Add(meetingPoint);
                }
            }

            return meetingPoints;
        }
    }
}