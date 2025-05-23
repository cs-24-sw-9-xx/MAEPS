using System;
using System.Collections.Generic;

using Maes.Algorithms.Patrolling;
using Maes.Algorithms.Patrolling.PartitionedRedistribution;
using Maes.FaultInjections;
using Maes.FaultInjections.DestroyRobots;
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

        // We both make building and cave maps, so 100 scenarios in total
        public const int StandardSeedCount = 50;

        public static readonly Dictionary<string, CreateAlgorithmDelegate> Algorithms = new()
        {
            { nameof(ConscientiousReactiveAlgorithm), (_) => new ConscientiousReactiveAlgorithm() },

            { nameof(RandomReactive), (seed) => new RandomReactive(seed) },

            {nameof(AdaptiveRedistributionFailureBasedCRAlgo), (_) => new AdaptiveRedistributionFailureBasedCRAlgo()},

            {nameof(AdaptiveRedistributionSuccessBasedCRAlgo), (_) => new AdaptiveRedistributionSuccessBasedCRAlgo()},

            {nameof(GlobalRedistributionWithCRAlgo), (_) =>  new GlobalRedistributionWithCRAlgo()},

            {nameof(RandomRedistributionWithCRAlgo), (seed) => new RandomRedistributionWithCRAlgo(seed, 2)},
        };

        private static RobotConstraints RobotConstraints(RobotConstraints.SignalTransmissionSuccessCalculator successCalculator)
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
                calculateSignalTransmissionProbability: successCalculator,
                robotCollisions: false,
                materialCommunication: false);
        }

        public static RobotConstraints MaterialRobotConstraints => RobotConstraints(successCalculator: (_, distanceThroughWalls) => distanceThroughWalls <= 3);

        public static RobotConstraints GlobalRobotConstraints => RobotConstraints(successCalculator: (_, _) => true);


        public static (string, Func<IFaultInjection>) FaultInjection(int seed, int robotCount = StandardRobotCount, float probability = 0.01f, int invokeEvery = 1000)
        {
            return ($"FI-random-{seed}-prob-{probability}-invoke-{invokeEvery}-max-{robotCount - 1}", () => new DestroyRobotsRandomFaultInjection(seed, probability, invokeEvery, robotCount - 1));
        }

    }
}