using System.Collections.Generic;

using Maes.Map;

namespace Maes.Algorithms.Patrolling
{
    public delegate void OnReachVertex(int vertexId, int atTick);

    public interface IPatrollingAlgorithm : IAlgorithm
    {
        string AlgorithmName { get; }

        /// <summary>
        /// Only to be used for visualization.
        /// </summary>
        Vertex TargetVertex { get; }

        /// <summary>
        /// Specifies vertices that should be colored using the robot's color.
        /// </summary>
        HashSet<int> ColorVertices { get; }

        void SetPatrollingMap(PatrollingMap map);

        /// <summary>
        /// A shared map. Should not be used in distributed algorithms.
        /// </summary>
        /// <param name="globalMap">The shared patrolling map.</param>
        void SetGlobalPatrollingMap(PatrollingMap globalMap);

        void SubscribeOnReachVertex(OnReachVertex onReachVertex);
    }
}