using System.Collections.Generic;

using Maes.Robot;

namespace Maes.Algorithms
{
    public interface IAlgorithm
    {
        IEnumerable<WaitForCondition> PreUpdateLogic();

        IEnumerable<WaitForCondition> UpdateLogic();

        void SetController(Robot2DController controller);

        // Returns debug info that will be shown when the robot is selected
        string GetDebugInfo();
    }
}