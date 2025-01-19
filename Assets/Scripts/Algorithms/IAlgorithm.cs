using Maes.Robot;

namespace Maes.Algorithms
{
    public interface IAlgorithm
    {
        void UpdateLogic();

        void SetController(Robot2DController controller);

        // Returns debug info that will be shown when the robot is selected
        string GetDebugInfo();
    }
}