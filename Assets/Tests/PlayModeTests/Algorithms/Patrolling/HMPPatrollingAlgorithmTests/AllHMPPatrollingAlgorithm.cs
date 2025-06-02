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
            new AlgorithmFactory(() => new NoFaultToleranceAlgorithm(), nameof(NoFaultToleranceAlgorithm)),
            new AlgorithmFactory(() => new FaultToleranceAlgorithm(), nameof(FaultToleranceAlgorithm)),
            new AlgorithmFactory(() => new ImmediateTakeOverAlgorithm(PartitionComponent.TakeoverStrategy.ImmediateTakeoverStrategy), nameof(ImmediateTakeOverAlgorithm)),
            new AlgorithmFactory(() => new ImmediateTakeOverAlgorithm(PartitionComponent.TakeoverStrategy.QuasiRandomStrategy), nameof(ImmediateTakeOverAlgorithm)),
            new AlgorithmFactory(() => new RandomTakeoverAlgorithm(), nameof(RandomTakeoverAlgorithm)),
            new AlgorithmFactory(() => new SingleMeetingPointAlgorithm(), nameof(SingleMeetingPointAlgorithm))
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
            public readonly Func<PatrollingAlgorithm> AlgorithmFactoryDelegate;
            private readonly string _algorithmName;

            public AlgorithmFactory(Func<PatrollingAlgorithm> algorithmFactoryDelegate, string algorithmName)
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