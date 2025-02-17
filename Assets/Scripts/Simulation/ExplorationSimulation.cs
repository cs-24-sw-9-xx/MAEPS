using System.Linq;

using Maes.Algorithms;
using Maes.Algorithms.Exploration.TheNextFrontier;
using Maes.Map.RobotSpawners;
using Maes.Simulation.SimulationScenarios;
using Maes.Statistics;
using Maes.Statistics.Exploration;
using Maes.UI.SimulationInfoUIControllers;

namespace Maes.Simulation
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

            ExplorationTracker = new ExplorationTracker(this, _collisionMap, explorationVisualizer, scenario.RobotConstraints);
        }

        public override bool HasFinishedSim()
        {
            return ExplorationTracker.ExploredProportion > 0.99f || SimulatedLogicTicks > 3600 * 10;
        }

        protected override void CreateStatisticsFile()
        {
            var resultForFileName = "e??-c??";
            if (ExplorationTracker.SnapShots.Any())
            {
                resultForFileName = $"e{ExplorationTracker.SnapShots[^1].Explored}-c{ExplorationTracker.SnapShots[^1].Covered}";
            }

            var path = $"{GlobalSettings.StatisticsOutPutPath}{_scenario.StatisticsFileName}_{resultForFileName}";
            new ExplorationCsvDataWriter(this, path).CreateCsvFile();
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