using Maes.Algorithms;
using Maes.Simulation;
using Maes.Statistics;

using MAES.Map.RobotSpawners;
using MAES.Simulation.SimulationScenarios;
using MAES.UI.SimulationInfoUIControllers;

namespace Maes
{
    public sealed class ExplorationSimulation : SimulationBase<ExplorationSimulation, ExplorationVisualizer, ExplorationCell, ExplorationTracker, ExplorationInfoUIController, IExplorationAlgorithm, ExplorationSimulationScenario, ExplorationRobotSpawner>
    {
        public ExplorationTracker ExplorationTracker { get; set; }

        public ExplorationVisualizer explorationVisualizer;
        public override ExplorationVisualizer Visualizer => explorationVisualizer;

        public override ExplorationTracker Tracker => ExplorationTracker;

        public override void SetScenario(ExplorationSimulationScenario scenario)
        {
            base.SetScenario(scenario);

            ExplorationTracker = new ExplorationTracker(_collisionMap, explorationVisualizer, scenario.RobotConstraints);
        }

        public override bool HasFinishedSim()
        {
            return ExplorationTracker.ExploredProportion > 0.99f;
        }

        public override void OnSimulationFinished()
        {
            if (GlobalSettings.ShouldWriteCSVResults)
            {
                CreateStatisticsFile();
            }
        }

        private void CreateStatisticsFile()
        {
            var csvWriter = new ExplorationStatisticsCSVWriter(this, $"{_scenario.StatisticsFileName}");
            csvWriter.CreateCSVFile(",");
        }
    }
}