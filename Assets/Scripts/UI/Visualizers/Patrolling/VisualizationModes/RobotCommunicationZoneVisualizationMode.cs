using Maes.Robot;

namespace Maes.UI.Visualizers.Patrolling.VisualizationModes
{
    internal class RobotCommunicationZoneVisualizer : IPatrollingVisualizationMode
    {
        private readonly MonaRobot _robot;

        public RobotCommunicationZoneVisualizer(MonaRobot robot)
        {
            _robot = robot;
        }

        public void UpdateVisualization(PatrollingVisualizer visualizer, int currentTick)
        {
            var position = _robot.Controller.SlamMap.GetCurrentPosition();

        }
    }
}