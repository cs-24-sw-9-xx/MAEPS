using System;
using System.Collections;

using Maes.Algorithms.Patrolling;
using Maes.Algorithms.Patrolling.HMPPatrollingAlgorithms.ImmediateTakeover;

using NUnit.Framework;

namespace Tests.PlayModeTests.Algorithms.Patrolling.HMPPatrollingAlgorithmTests
{
    using FaultToleranceAlgorithm = Maes.Algorithms.Patrolling.HMPPatrollingAlgorithms.FaultTolerance.HMPPatrollingAlgorithm;
    using ImmediateTakeOverAlgorithm = Maes.Algorithms.Patrolling.HMPPatrollingAlgorithms.ImmediateTakeover.HMPPatrollingAlgorithm;
    using NoFaultToleranceAlgorithm = Maes.Algorithms.Patrolling.HMPPatrollingAlgorithms.NoFaultTolerance.HMPPatrollingAlgorithm;
    using RandomTakeoverAlgorithm = Maes.Algorithms.Patrolling.HMPPatrollingAlgorithms.RandomTakeover.HMPPatrollingAlgorithm;
    using SingleMeetingPointAlgorithm = Maes.Algorithms.Patrolling.HMPPatrollingAlgorithms.SingleMeetingPoint.HMPPatrollingAlgorithm;

    public class AllHMPPatrollingAlgorithm
    {
        private static readonly AlgorithmFactory[] Cases = new[]
        {
            new AlgorithmFactory(seed => new NoFaultToleranceAlgorithm(seed), nameof(NoFaultToleranceAlgorithm)),
            new AlgorithmFactory(seed => new FaultToleranceAlgorithm(seed), nameof(FaultToleranceAlgorithm)),
            new AlgorithmFactory(seed => new ImmediateTakeOverAlgorithm(PartitionComponent.TakeoverStrategy.ImmediateTakeoverStrategy, seed), nameof(ImmediateTakeOverAlgorithm)),
            new AlgorithmFactory(seed => new ImmediateTakeOverAlgorithm(PartitionComponent.TakeoverStrategy.QuasiRandomStrategy, seed), nameof(ImmediateTakeOverAlgorithm)),
            new AlgorithmFactory(seed => new RandomTakeoverAlgorithm(seed), nameof(RandomTakeoverAlgorithm)),
            new AlgorithmFactory(seed => new SingleMeetingPointAlgorithm(seed), nameof(SingleMeetingPointAlgorithm))
        };

        public static IEnumerable TestCases
        {
            get
            {
                foreach (var testCase in Cases)
                {
                    yield return new TestCaseData(testCase).Returns(null);
                }
            }
        }

        public sealed class AlgorithmFactory
        {
            public readonly Func<int, PatrollingAlgorithm> AlgorithmFactoryDelegate;
            private readonly string _algorithmName;

            public AlgorithmFactory(Func<int, PatrollingAlgorithm> algorithmFactoryDelegate, string algorithmName)
            {
                AlgorithmFactoryDelegate = algorithmFactoryDelegate;
                _algorithmName = algorithmName;
            }

            public override string ToString()
            {
                return _algorithmName;
            }
        }
    }
}