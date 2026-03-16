using Maes.Algorithms.Patrolling.Components;
using Maes.Map;
using Maes.Robot;

namespace Maes.Algorithms.Patrolling.PartitionedRedistribution
{
    public class PBPS : PatrollingAlgorithm
    {
        public override string AlgorithmName { get; }
        protected override IComponent[] CreateComponents(IRobotController controller, PatrollingMap patrollingMap)
        {
            throw new System.NotImplementedException();
        }
    }
}