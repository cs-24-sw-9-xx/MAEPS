using System.Collections.Generic;

using Maes.Algorithms.Patrolling.HMPPatrollingAlgorithms.SingleMeetingPoint;
using Maes.FaultInjections.DestroyRobots;
using Maes.Map.Generators;
using Maes.Map.Generators.Patrolling.Waypoints.Generators;
using Maes.Robot;
using Maes.Simulation.Patrolling;

using UnityEngine;

namespace Maes.Experiments.Patrolling
{
    using MySimulationScenario = PatrollingSimulationScenario;
    using MySimulator = PatrollingSimulator;
    internal class HMPPatrollingSingleMeetingPointExperiment : MonoBehaviour
    {
        private void Start()
        {
            const int robotCount = 6;
            const int seed = 123;
            const int mapSize = 100;
            const string algoName = "HMPPatrollingAlgorithm";

            const string constraintName = "local";
            var robotConstraints = new RobotConstraints(
                mapKnown: true,
                distributeSlam: false,
                slamRayTraceRange: 0f,
                robotCollisions: false,
                calculateSignalTransmissionProbability: (_, distanceThroughWalls) => distanceThroughWalls == 0f);

            var scenarios = new List<MySimulationScenario>();


            var mapConfig = new BuildingMapConfig(123, widthInTiles: mapSize, heightInTiles: mapSize, brokenCollisionMap: false);

            scenarios.Add(
                new MySimulationScenario(
                    seed: seed,
                    totalCycles: 100,
                    stopAfterDiff: false,
                    robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsTogether(
                        collisionMap: buildingConfig,
                        seed: seed,
                        numberOfRobots: robotCount,
                        suggestedStartingPoint: null,
                        createAlgorithmDelegate: _ => new HMPPatrollingAlgorithm()),
                    mapSpawner: generator => generator.GenerateMap(mapConfig),
                    robotConstraints: robotConstraints,
                    faultInjection: new DestroyRobotsAtSpecificTickFaultInjection(seed, 10000, 20000, 25000),
                    statisticsFileName: $"{algoName}-seed-{mapConfig.RandomSeed}-size-{mapSize}-comms-{constraintName}-robots-{robotCount}-SpawnTogether",
                    patrollingMapFactory: map => AllWaypointConnectedGenerator.MakePatrollingMap(map, GroupAParameters.MaxDistance))
            );

            var simulator = new MySimulator(scenarios);
            simulator.PressPlayButton(); // Instantly enter play mode
        }
    }
}