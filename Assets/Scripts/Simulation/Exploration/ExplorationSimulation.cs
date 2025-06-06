using Maes.Algorithms.Exploration;
using Maes.Algorithms.Exploration.TheNextFrontier;
using Maes.Map.RobotSpawners;
using Maes.Statistics.Trackers;
using Maes.UI.SimulationInfoUIControllers;
using Maes.UI.Visualizers.Exploration;

namespace Maes.Simulation.Exploration
{
    public sealed class ExplorationSimulation : SimulationBase<ExplorationSimulation, ExplorationVisualizer, ExplorationTracker, ExplorationInfoUIController, IExplorationAlgorithm, ExplorationSimulationScenario, ExplorationRobotSpawner>
    {
        // Set by SetScenario
        public ExplorationTracker ExplorationTracker { get; private set; } = null!;

        public ExplorationVisualizer explorationVisualizer = null!;
        public override ExplorationVisualizer Visualizer => explorationVisualizer;

        public override ExplorationTracker Tracker => ExplorationTracker;

        public override void SetScenario(ExplorationSimulationScenario scenario)
        {
            base.SetScenario(scenario);

            ExplorationTracker = new ExplorationTracker(this, _collisionMap, explorationVisualizer, scenario.RobotConstraints, scenario);
        }

        public override bool HasFinishedSim()
        {
            return ExplorationTracker.ExploredProportion > 0.99f || SimulatedLogicTicks > 3600 * 10;
        }

        /// <summary>
        /// Tests specifically if The Next Frontier is no longer doing any work.
        /// </summary>
        public bool TnfBotsOutOfFrontiers()
        {
            foreach (var monaRobot in Robots)
            {
                if (!(monaRobot.Algorithm as TnfExplorationAlgorithm)?.IsOutOfFrontiers() ?? true)
                {
                    return false;
                }
            }

            return true;
        }
    }
}