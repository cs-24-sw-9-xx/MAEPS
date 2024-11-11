using Maes.Robot;

namespace Maes.Algorithms
{
    public interface IAlgorithm
    {
        public void UpdateLogic();

        public void SetController(Robot2DController controller);

        // Returns debug info that will be shown when the robot is selected
        public string GetDebugInfo();
    }
}