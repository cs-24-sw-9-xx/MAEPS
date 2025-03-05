using System.Linq;

using Maes.Algorithms.Patrolling.Components;
using Maes.Map;
using Maes.Robot;

namespace Maes.Algorithms.Patrolling
{
    /// <summary>
    /// Original implementation of the Conscientious Reactive Algorithm of https://doi.org/10.1007/3-540-36483-8_11.
    /// Pseudocode can be found in another paper: https://doi.org/10.1080/01691864.2013.763722
    /// </summary>
    public sealed class ConscientiousReactiveAlgorithm : PatrollingAlgorithm
    {
        public override string AlgorithmName => "Conscientious Reactive Algorithm";

        // Set by CreateComponents
        private GoToNextVertexComponent _goToNextVertexComponent = null!;

        protected override IComponent[] CreateComponents(Robot2DController controller, PatrollingMap patrollingMap)
        {
            _goToNextVertexComponent = new GoToNextVertexComponent(NextVertex, this, controller, patrollingMap);

            return new IComponent[] { _goToNextVertexComponent };
        }

        private static Vertex NextVertex(Vertex currentVertex)
        {
            return currentVertex.Neighbors.OrderBy(x => x.LastTimeVisitedTick).First();
        }
    }
}