using Maes.Algorithms;
using Maes.Map;
using Maes.Robot;

namespace MAES.PatrollingAlgorithm
{
    public abstract class PatrollingAlgorithm : IPatrollingAlgorithm
    {
        public abstract void SetPatrollingMap(PatrollingMap map);
        public abstract void SetController(Robot2DController controller);
        public abstract void UpdateLogic();
        public abstract string GetDebugInfo();
    }
}