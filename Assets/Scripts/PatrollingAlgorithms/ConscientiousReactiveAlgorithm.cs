using System.Linq;

using Maes.Map;

namespace Maes.PatrollingAlgorithms
{
    public class ConscientiousReactiveAlgorithm : PatrollingAlgorithm
    {
        public override string AlgorithmName => "Conscientious Reactive Algorithm";

        protected override Vertex NextVertex()
        {
            return TargetVertex.Neighbors.OrderBy(x => x.LastTimeVisitedTick).First();
        }
    }
}