using System.Collections.Generic;

using Maes.Robot;

namespace Maes.Algorithms.Components
{
    public interface ICollisionRecovery<in TAlgorithm>
        where TAlgorithm : IAlgorithm
    {
        IEnumerable<WaitForCondition> CheckAndRecoverFromCollision();
        void SetController(Robot2DController controller);
        void SetAlgorithm(TAlgorithm controller);
    }
}