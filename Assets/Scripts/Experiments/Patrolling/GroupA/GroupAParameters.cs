// Copyright 2025 MAEPS
// 
// This file is part of MAEPS
// 
// MAEPS is free software: you can redistribute it and/or modify it under
// the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your option)
// any later version.
// 
// MAEPS is distributed in the hope that it will be useful, but WITHOUT
// ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
// or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General
// Public License for more details.
// 
// You should have received a copy of the GNU General Public License along
// with MAEPS. If not, see http://www.gnu.org/licenses/.
// 
// Contributors: 
// Henrik van Peet,
// Mads Beyer Mogensen,
// Puvikaran Santhirasegaram

using System;
using System.Collections.Generic;
using System.Linq;

using Maes.Algorithms.Patrolling;
using Maes.Algorithms.Patrolling.HeuristicConscientiousReactive;
using Maes.Map.Generators.Patrolling.Waypoints.Generators;
using Maes.Robot;
using Maes.Simulation.Patrolling;

using static Maes.Map.RobotSpawners.RobotSpawner<Maes.Algorithms.Patrolling.IPatrollingAlgorithm>;

using ImmediateTakeover = Maes.Algorithms.Patrolling.HMPPatrollingAlgorithms.ImmediateTakeover;
using NoFaultTolerance = Maes.Algorithms.Patrolling.HMPPatrollingAlgorithms.NoFaultTolerance;
using RandomTakeover = Maes.Algorithms.Patrolling.HMPPatrollingAlgorithms.RandomTakeover;

namespace Maes.Experiments.Patrolling
{
    internal static class GroupAParameters
    {
        static GroupAParameters() {
            AllAlgorithms = StandardAlgorithms
                .Concat(FaultTolerantHMPVariants)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        public static readonly Dictionary<string, Func<int, (PatrollingMapFactory?, CreateAlgorithmDelegate)>> AllAlgorithms;

        /// <summary>
        /// Supply the function with the robot count and it will return the patrolling map factory and the algorithm.
        /// </summary>
        public static readonly Dictionary<string, Func<int, (PatrollingMapFactory?, CreateAlgorithmDelegate)>> StandardAlgorithms = new()
        {
            { nameof(ConscientiousReactiveAlgorithm), (_) => (null, (_) => new ConscientiousReactiveAlgorithm()) },

            // ConscientiousReactiveAlgorithm with partitioning
            /*
            { nameof(ConscientiousReactiveAlgorithm)+ "+partitioning", (robotCount) => ((map) =>
                PartitioningGenerator.MakePatrollingMapWithSpectralBisectionPartitions(map, robotCount, CreateRobotConstraints()),
                 (_) => new ConscientiousReactiveAlgorithm()) },
                 */
    
            // The map is different for each seed, so the algorithm can just use the same seed for all maps.
            { nameof(RandomReactive), (_) => (null, (_) => new RandomReactive(1)) },

            // Algorithms that use all-waypoint-connected-maps
            { nameof(HeuristicConscientiousReactiveAlgorithm), (_) => (AllWaypointConnectedGenerator.MakePatrollingMap, (_) => new HeuristicConscientiousReactiveAlgorithm()) },
            { nameof(SingleCycleChristofides), (_) => (AllWaypointConnectedGenerator.MakePatrollingMap, (_) => new SingleCycleChristofides()) },

            // Algorithms that use all-waypoint-connected-maps and partitioning
            { "NoFaultTolerance.HMPPatrollingAlgorithm", (_) => (AllWaypointConnectedGenerator.MakePatrollingMap, (_) => new NoFaultTolerance.HMPPatrollingAlgorithm()) },
        };

        public static readonly Dictionary<string, Func<int, (PatrollingMapFactory?, CreateAlgorithmDelegate)>> FaultTolerantHMPVariants = new()
        {
            { "ImmediateTakeover.HMPPatrollingAlgorithm",
                (_) => (AllWaypointConnectedGenerator.MakePatrollingMap, (_) => new ImmediateTakeover.HMPPatrollingAlgorithm(ImmediateTakeover.PartitionComponent.TakeoverStrategy.ImmediateTakeoverStrategy)) },
            { "QuasiRandomTakeover.HMPPatrollingAlgorithm",
                (_) => (AllWaypointConnectedGenerator.MakePatrollingMap, (_) => new ImmediateTakeover.HMPPatrollingAlgorithm(ImmediateTakeover.PartitionComponent.TakeoverStrategy.QuasiRandomStrategy)) },
            { "RandomTakeover.HMPPatrollingAlgorithm",
                (_) => (AllWaypointConnectedGenerator.MakePatrollingMap, (_) => new RandomTakeover.HMPPatrollingAlgorithm()) },
        };

        public const int StandardAmountOfCycles = 100; // Should be changed to 1000 for the final experiment?
        public const int StandardMapSize = 200;
        public const int StandardRobotCount = 8;
        public const int StandardSeedCount = 100;

        public static readonly string StandardRobotConstraintName = "Standard";

        /// <summary>
        /// Creates the robot constraints for the patrolling algorithms.
        /// The default values use LOS communication.
        /// </summary>
        public static RobotConstraints CreateRobotConstraints(float communicationDistanceThroughWalls = 0f, float senseNearbyAgentsRange = 5f, bool senseNearbyAgentsBlockedByWalls = true)
        {
            return new RobotConstraints(
                senseNearbyAgentsRange: senseNearbyAgentsRange,
                senseNearbyAgentsBlockedByWalls: senseNearbyAgentsBlockedByWalls,
                slamUpdateIntervalInTicks: 1,
                mapKnown: true,
                distributeSlam: false,
                environmentTagReadRange: 100f,
                slamRayTraceRange: 0f,
                calculateSignalTransmissionProbability: (_, distanceThroughWalls) => distanceThroughWalls <= communicationDistanceThroughWalls,
                robotCollisions: false,
                materialCommunication: true);
        }

        public static IEnumerable<int> SeedGenerator(int seedCount = StandardSeedCount)
        {
            var seeds = new List<int>();
            for (var i = 0; i < seedCount; i++)
            {
                seeds.Add(i);
            }
            return seeds;
        }
    }
}