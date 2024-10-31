using Maes.Map;

namespace Maes.Algorithms
{
    public interface IPatrollingAlgorithm : IAlgorithm {
        void SetPatrollingMap(PatrollingMap map);
    }
}