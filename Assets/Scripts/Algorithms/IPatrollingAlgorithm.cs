using Maes.Map;

namespace Maes.Algorithms
{
    public delegate void OnReachVertex(int vertexId, int atTick);

    public interface IPatrollingAlgorithm : IAlgorithm
    {
        string AlgorithmName { get; }

        Vertex TargetVertex { get; }

        void SetPatrollingMap(PatrollingMap map);

        /// <summary>
        /// A shared map. Should not be used in distributed algorithms.
        /// </summary>
        /// <param name="globalMap">The shared patrolling map.</param>
        void SetGlobalPatrollingMap(PatrollingMap globalMap);

        void SubscribeOnReachVertex(OnReachVertex onReachVertex);
    }
}