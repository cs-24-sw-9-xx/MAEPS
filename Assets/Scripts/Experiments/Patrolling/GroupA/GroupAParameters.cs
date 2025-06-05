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

using FaultTolerance = Maes.Algorithms.Patrolling.HMPPatrollingAlgorithms.FaultTolerance;
using ImmediateTakeover = Maes.Algorithms.Patrolling.HMPPatrollingAlgorithms.ImmediateTakeover;
using PartitionedHeuristicConscientiousReactive = Maes.Algorithms.Patrolling.HMPPatrollingAlgorithms.PartitionedHeuristicConscientiousReactive;
using RandomTakeover = Maes.Algorithms.Patrolling.HMPPatrollingAlgorithms.RandomTakeover;
using SingleMeetingPoint = Maes.Algorithms.Patrolling.HMPPatrollingAlgorithms.SingleMeetingPoint;

namespace Maes.Experiments.Patrolling
{
    using AlgorithmsDictionary = Dictionary<string, Func<int, (PatrollingMapFactory, CreateAlgorithmDelegate)>>;
    using IReadOnlyAlgorithmsDictionary = IReadOnlyDictionary<string, Func<int, (PatrollingMapFactory, CreateAlgorithmDelegate)>>;
    internal static class GroupAParameters
    {
        static GroupAParameters()
        {
            AllAlgorithms = ReactiveAlgorithms
                .Concat(PartitionedAlgorithms)
                .Concat(FaultTolerantHMPVariants)
                .Concat(CyclicAlgorithms)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        public static readonly IReadOnlyAlgorithmsDictionary AllAlgorithms;

        public static readonly IReadOnlyAlgorithmsDictionary
            ReactiveAlgorithms = new AlgorithmsDictionary
            {
                { nameof(ConscientiousReactiveAlgorithm), _ => (map => ReverseNearestNeighborGenerator.MakePatrollingMap(map, MaxDistance), _ => new ConscientiousReactiveAlgorithm()) },
                { nameof(RandomReactive), _ => (map => ReverseNearestNeighborGenerator.MakePatrollingMap(map, MaxDistance), seed => new RandomReactive(seed)) },
                { nameof(HeuristicConscientiousReactiveAlgorithm), _ => (map => AllWaypointConnectedGenerator.MakePatrollingMap(map, MaxDistance), _ => new HeuristicConscientiousReactiveAlgorithm()) }
            };

        public static readonly IReadOnlyAlgorithmsDictionary CyclicAlgorithms = new AlgorithmsDictionary
        {
            {
                nameof(SingleCycleChristofides), _ => (map => AllWaypointConnectedGenerator.MakePatrollingMap(map, MaxDistance), _ => new SingleCycleChristofides())
            }
        };

        public static readonly IReadOnlyAlgorithmsDictionary PartitionedAlgorithms = new AlgorithmsDictionary
        {
            {
                "Partitioned.HeuristicConscientiousReactiveAlgorithm", _ => (map => AllWaypointConnectedGenerator.MakePatrollingMap(map, MaxDistance), _ => new PartitionedHeuristicConscientiousReactive.HMPPatrollingAlgorithm())
            }
        };

        public static readonly IReadOnlyAlgorithmsDictionary FaultTolerantHMPVariants = new AlgorithmsDictionary
        {
            { "ImmediateTakeover.HMPPatrollingAlgorithm",
                _ => (map => AllWaypointConnectedGenerator.MakePatrollingMap(map, MaxDistance), _ => new ImmediateTakeover.HMPPatrollingAlgorithm(ImmediateTakeover.PartitionComponent.TakeoverStrategy.ImmediateTakeoverStrategy)) },
            { "QuasiRandomTakeover.HMPPatrollingAlgorithm",
                _ => (map => AllWaypointConnectedGenerator.MakePatrollingMap(map, MaxDistance), _ => new ImmediateTakeover.HMPPatrollingAlgorithm(ImmediateTakeover.PartitionComponent.TakeoverStrategy.QuasiRandomStrategy)) },
            { "RandomTakeover.HMPPatrollingAlgorithm",
                _ => (map => AllWaypointConnectedGenerator.MakePatrollingMap(map, MaxDistance), _ => new RandomTakeover.HMPPatrollingAlgorithm()) },
            { "SingleMeetingPoint.HMPPatrollingAlgorithm",
                _ => (map => AllWaypointConnectedGenerator.MakePatrollingMap(map, MaxDistance), _ => new SingleMeetingPoint.HMPPatrollingAlgorithm()) },
            { "FaultTolerance.HMPPatrollingAlgorithm",
                _ => (map => AllWaypointConnectedGenerator.MakePatrollingMap(map, MaxDistance), _ => new FaultTolerance.HMPPatrollingAlgorithm()) }
        };

        public const int StandardAmountOfCycles = 100; // Should be changed to 1000 for the final experiment?
        public const int StandardMapSize = 200;
        public const int StandardRobotCount = 8;
        public const int StandardSeedCount = 100;
        public const float MaxDistance = 25f;

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

        public static IEnumerable<int> SeedGenerator(int seedCount = StandardSeedCount, int startSeed = 0)
        {
            if (seedCount < 1)
            {
                throw new ArgumentException("Seed count must be at least 1.", nameof(seedCount));
            }

            return Enumerable.Range(startSeed, seedCount);
        }
    }
}