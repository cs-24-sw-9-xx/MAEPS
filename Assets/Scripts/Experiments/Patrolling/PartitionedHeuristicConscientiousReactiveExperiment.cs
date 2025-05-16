using System.Collections.Generic;

using Maes.Map.Generators;
using Maes.Map.Generators.Patrolling.Partitioning;
using Maes.Map.Generators.Patrolling.Waypoints.Generators;
using Maes.Robot;
using Maes.Simulation.Patrolling;

using UnityEngine;

namespace Maes.Experiments.Patrolling
{
    internal class PartitionedHeuristicConscientiousReactiveExperiment : MonoBehaviour
    {
        private void Start()
        {
            const int robotCount = 6;
            const int totalCycles = 4;
            const int seed = 123;
            const int mapSize = 100;
            const string algoName = "PHCR";

            var mapConfig = new BuildingMapConfig(seed, widthInTiles: mapSize, heightInTiles: mapSize, brokenCollisionMap: false);

            const string constraintName = "local";
            var robotConstraints = new RobotConstraints(
                senseNearbyAgentsRange: 5f,
                senseNearbyAgentsBlockedByWalls: true,
                automaticallyUpdateSlam: true,
                slamUpdateIntervalInTicks: 1,
                slamSynchronizeIntervalInTicks: 10,
                slamPositionInaccuracy: 0.2f,
                mapKnown: true,
                distributeSlam: false,
                environmentTagReadRange: 100f,
                slamRayTraceRange: 7f,
                relativeMoveSpeed: 1f,
                agentRelativeSize: 0.6f,
                calculateSignalTransmissionProbability: (_, _) => true);

            var scenarios = new List<PatrollingSimulationScenario>
            {
                new(
                    seed: seed,
                    totalCycles: totalCycles,
                    stopAfterDiff: false,
                    robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsTogether(
                        collisionMap: buildingConfig,
                        seed: seed,
                        numberOfRobots: robotCount,
                        suggestedStartingPoint: null,
                        createAlgorithmDelegate: _ => new PartitionedHeuristicConscientiousReactive(new AdapterToPartitionGenerator(SpectralBisectionPartitioningGenerator.Generator))),
                    mapSpawner: generator => generator.GenerateMap(mapConfig),
                    robotConstraints: robotConstraints,
                    statisticsFileName: $"{algoName}-seed-{mapConfig.RandomSeed}-size-{mapSize}-comms-{constraintName}-robots-{robotCount}-totalCycles-{totalCycles}-SpawnTogether",
                    patrollingMapFactory: AllWaypointConnectedGenerator.MakePatrollingMap)
            };


            var simulator = new PatrollingSimulator(scenarios);
            simulator.PressPlayButton(); // Instantly enter play mode
        }
    }
}