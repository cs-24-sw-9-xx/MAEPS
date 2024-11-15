using Maes.Map;

namespace Maes.Algorithms
{
    public delegate void OnReachVertex(int vertexId, int atTick);

    public interface IPatrollingAlgorithm : IAlgorithm
    {
        string AlgorithmName { get; }

        Vertex TargetVertex { get; }

        void SetPatrollingMap(PatrollingMap map);

        void SubscribeOnReachVertex(OnReachVertex onReachVertex);
    }
}