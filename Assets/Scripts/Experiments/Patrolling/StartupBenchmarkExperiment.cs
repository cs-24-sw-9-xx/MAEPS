
// Mads Beyer Mogensen,

using System.Collections.Generic;

using Maes.Algorithms.Patrolling;
using Maes.Map.Generators;
using Maes.Robot;
using Maes.Simulation.Patrolling;

using UnityEngine;

namespace Maes.Experiments.Patrolling
{
    using MySimulationScenario = PatrollingSimulationScenario;
    using MySimulator = PatrollingSimulator;

    internal class StartupBenchmarkExperiment : MonoBehaviour
    {
        private const int Seed = 1;
        private const int RobotCount = 1;
        private void Start()
        {
            var robotConstraint = new RobotConstraints(
                senseNearbyAgentsRange: 5f,
                slamUpdateIntervalInTicks: 1,
                mapKnown: true,
                distributeSlam: false,
                environmentTagReadRange: 100f,
                slamRayTraceRange: 7f,
                materialCommunication: false);


            var constraintName = "LOS";
            var algoName = nameof(RandomReactive);
            // release mode, and no security checks
            var mapSizes = new List<int>
            {
                50,  // 0,7 second
                // 100, // 5 seconds
                // 150, // 24 seconds
                // 200, // 68 seconds
                // 250, // 177 seconds
                // 300, // 445 seconds
                // 400,
                // 500,
            };
            foreach (var mapSize in mapSizes)
            {
                var stopwatch = new System.Diagnostics.Stopwatch();
                stopwatch.Start();

                var mapConfig = new BuildingMapConfig(1, widthInTiles: mapSize, heightInTiles: mapSize, brokenCollisionMap: false);
                var scenarios = new MySimulationScenario[]
                {
                new(
                    seed: Seed,
                    totalCycles: 0,
                    stopAfterDiff: false,
                    robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsApart(
                        collisionMap: buildingConfig,
                        seed: Seed,
                        numberOfRobots: RobotCount,
                        createAlgorithmDelegate: (_) => new RandomReactive(1)),
                    mapSpawner: generator => generator.GenerateMap(mapConfig),
                    robotConstraints: robotConstraint,
                    statisticsFileName:
                    $"{algoName}-seed-{mapConfig.RandomSeed}-size-{mapSize}-comms-{constraintName}-robots-{RobotCount}-SpawnTogether"),
                };

                var simulator = new MySimulator(scenarios);

                simulator.PressPlayButton(); // Instantly enter play mode
                stopwatch.Stop();
                Debug.Log($"Map size: {mapSize}, Time taken: {stopwatch.ElapsedMilliseconds} ms");
            }
        }
    }
}