using System;
using System.Collections.Generic;

using Maes.Algorithms.Patrolling;
using Maes.Algorithms.Patrolling.PartitionedRedistribution;
using Maes.FaultInjections;
using Maes.FaultInjections.DestroyRobots;
using Maes.Map.Generators;
using Maes.Robot;
using Maes.Simulation;

using static Maes.Map.RobotSpawners.RobotSpawner<Maes.Algorithms.Patrolling.IPatrollingAlgorithm>;

namespace Maes.Experiments.Patrolling.GroupB
{
    public static class GroupBParameters
    {
        public const int StandardAmountOfCycles = 1000000;
        public const int StandardMapSize = 200;
        public const int StandardRobotCount = 16;
        public const int StandardPartitionCount = 4;
        public const int StandardFaultInjectionSeed = 1;

        public const int StandardMaxLogicTicks = 300000;

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
            2,
            4,
            8,
        };

        public static readonly Dictionary<string, RobotConstraints> RobotConstraintsDictionary = new()
        {
            { nameof(GlobalRedistributionWithCRAlgo), GlobalRobotConstraints },
            { nameof(AdaptiveRedistributionCRAlgo), MaterialRobotConstraints },
            { nameof(RandomRedistributionWithCRAlgo), MaterialRobotConstraints },
        };

        // We both make building and cave maps, so 100 scenarios in total
        public const int StandardSeedCount = 50;

        public static bool StandardHasFinished(ISimulation simulation, out SimulationEndCriteriaReason? reason)
        {
            if (simulation.HasFinishedSim())
            {
                reason = new SimulationEndCriteriaReason("Success", true);
                return true;
            }

            if (simulation.SimulatedLogicTicks > StandardMaxLogicTicks)
            {
                reason = new SimulationEndCriteriaReason("Reached max logic ticks", true);
                return true;
            }

            reason = null;
            return false;
        }

        public static readonly Dictionary<string, CreateAlgorithmDelegate> Algorithms = new()
        {
            { nameof(ConscientiousReactiveAlgorithm), (_) => new ConscientiousReactiveAlgorithm() },

            { nameof(RandomReactive), (seed) => new RandomReactive(seed) },
        };

        public static readonly Dictionary<string, CreateAlgorithmDelegate> AllPartitionedAlgorithms = new()
        {
            {nameof(GlobalRedistributionWithCRAlgo), (_) =>  new GlobalRedistributionWithCRAlgo()},

            {nameof(RandomRedistributionWithCRAlgo), (seed) => new RandomRedistributionWithCRAlgo(seed, 2)},

            { nameof(AdaptiveRedistributionCRAlgo), (_) => new AdaptiveRedistributionCRAlgo()},
        };

        public static readonly Dictionary<string, CreateAlgorithmDelegate> PartitionedAlgorithms = new()
        {
            {nameof(GlobalRedistributionWithCRAlgo), (_) =>  new GlobalRedistributionWithCRAlgo()},

            {nameof(RandomRedistributionWithCRAlgo), (seed) => new RandomRedistributionWithCRAlgo(seed, 0.5f)},
        };

        private static readonly Dictionary<uint, Dictionary<TileType, float>> Frequencies = new()
        {
            [2400] = new() //2.4 GHz
            {
                [TileType.Room] = 0.0f,
                [TileType.Hall] = 0.0f,
                [TileType.Wall] = 0.0f,
                [TileType.Concrete] = 5.5f,
                [TileType.Wood] = 5.5f,
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