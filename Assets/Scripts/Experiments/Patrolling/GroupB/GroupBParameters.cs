using System;
using System.Collections.Generic;

using Maes.Algorithms.Patrolling;
using Maes.Algorithms.Patrolling.PartitionedRedistribution;
using Maes.FaultInjections;
using Maes.FaultInjections.DestroyRobots;
using Maes.Map.Generators;
using Maes.Robot;

using static Maes.Map.RobotSpawners.RobotSpawner<Maes.Algorithms.Patrolling.IPatrollingAlgorithm>;

namespace Maes.Experiments.Patrolling.GroupB
{
    public static class GroupBParameters
    {
        public const int StandardAmountOfCycles = 100;
        public const int StandardMapSize = 150;
        public const int StandardRobotCount = 16;
        public const int StandardPartitionCount = 4;
        public const int StandardFaultInjectionSeed = 1;

        public static readonly List<int> MapSizes = new()
        {
            150,
            200,
            250,
        };

        public static readonly List<int> RobotCounts = new()
        {
            1,
            2,
            4,
            8,
            16,
            32,
        };
        public static readonly List<int> PartitionCounts = new()
        {
            1,
            2,
            4,
        };
        public static readonly List<int> RedistributionPartitionCounts = new()
        {
            2,
            4,
        };

        public static readonly Dictionary<string, RobotConstraints> RobotConstraintsDictionary = new()
        {
            { nameof(GlobalRedistributionWithCRAlgo), GlobalRobotConstraints },
            { nameof(AdaptiveRedistributionFailureBasedCRAlgo), MaterialRobotConstraints },
            { nameof(AdaptiveRedistributionSuccessBasedCRAlgo), MaterialRobotConstraints },
            { nameof(RandomRedistributionWithCRAlgo), MaterialRobotConstraints },
        };

        // We both make building and cave maps, so 100 scenarios in total
        public const int StandardSeedCount = 10;

        public static readonly Dictionary<string, CreateAlgorithmDelegate> Algorithms = new()
        {
            { nameof(ConscientiousReactiveAlgorithm), (_) => new ConscientiousReactiveAlgorithm() },

            { nameof(RandomReactive), (seed) => new RandomReactive(seed) },
        };

        public static readonly Dictionary<string, CreateAlgorithmDelegate> PartitionedAlgorithms = new()
        {
            {nameof(AdaptiveRedistributionFailureBasedCRAlgo), (_) => new AdaptiveRedistributionFailureBasedCRAlgo()},

            {nameof(AdaptiveRedistributionSuccessBasedCRAlgo), (_) => new AdaptiveRedistributionSuccessBasedCRAlgo()},

            {nameof(GlobalRedistributionWithCRAlgo), (_) =>  new GlobalRedistributionWithCRAlgo()},

            {nameof(RandomRedistributionWithCRAlgo), (seed) => new RandomRedistributionWithCRAlgo(seed, 2)},
        };

        private static readonly Dictionary<uint, Dictionary<TileType, float>> Frequencies = new()
        {
            [2400] = new() //2.4 GHz
            {
                [TileType.Room] = 0.25f,
                [TileType.Hall] = 0.25f,
                [TileType.Wall] = 0.25f,
                [TileType.Concrete] = 15f,
                [TileType.Wood] = 6.7f,
                [TileType.Brick] = 5.5f
            }
        };

        private static RobotConstraints RobotConstraints(bool materialCommunication)
        {
            return new RobotConstraints(
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
                calculateSignalTransmissionProbability: (_, _) => true,
                robotCollisions: false,
                materialCommunication: materialCommunication);
        }

        public static RobotConstraints MaterialRobotConstraints => RobotConstraints(true);

        public static RobotConstraints GlobalRobotConstraints => RobotConstraints(false);

        /// <summary>
        /// Creates a fault injection strategy that destroys robots randomly.
        /// </summary>
        /// <param name="seed"></param>
        /// <param name="robotCount"></param>
        /// <param name="probability"></param>
        /// <param name="invokeEvery"></param>
        /// <returns></returns>
        /// <remarks>
        /// FI-random: Fault Injection random
        /// prob: Probability
        /// max: Max number of robot deaths
        /// </remarks>
        public static (string, Func<IFaultInjection>) FaultInjection(int seed = StandardFaultInjectionSeed, int robotCount = StandardRobotCount, float probability = 0.025f, int invokeEvery = 1000)
        {
            return ($"FI-random-{seed}-prob-{probability}-invoke-{invokeEvery}-max-{robotCount - 1}", () => new DestroyRobotsRandomFaultInjection(seed, probability, invokeEvery, robotCount - 1));
        }

    }
}