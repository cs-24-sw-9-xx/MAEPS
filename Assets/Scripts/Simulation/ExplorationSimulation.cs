using Maes.Algorithms;
using MAES.Map.RobotSpawners;
using Maes.Simulation;
using MAES.Simulation.SimulationScenarios;
using Maes.Statistics;
using Maes.UI;

using MAES.UI.SimulationInfoUIControllers;

using UnityEngine;

namespace Maes
{
    public sealed class ExplorationSimulation : SimulationBase<ExplorationSimulation, ExplorationVisualizer, ExplorationCell, ExplorationTracker, ExplorationInfoUIController, IExplorationAlgorithm, ExplorationSimulationScenario, ExplorationRobotSpawner>
    {
        public ExplorationTracker ExplorationTracker { get; set; }

        public ExplorationVisualizer explorationVisualizer;

        public GameObject explorationVisualizerPrefab;
        public override ExplorationVisualizer Visualizer => explorationVisualizer;

        public override ExplorationTracker Tracker => ExplorationTracker;

        protected override void AfterStart()
        {
            explorationVisualizerPrefab = Resources.Load<GameObject>("ExplorationVisualizer");
            explorationVisualizer = Instantiate(explorationVisualizerPrefab).GetComponent<ExplorationVisualizer>();
            
            RobotSpawner = gameObject.AddComponent<ExplorationRobotSpawner>();
        }

        public override void SetScenario(ExplorationSimulationScenario scenario)
        {
            base.SetScenario(scenario);
            
            ExplorationTracker = new ExplorationTracker(_collisionMap, explorationVisualizer, scenario.RobotConstraints);
        }

        public override bool HasFinishedSim()
        {
            return ExplorationTracker.ExploredProportion > 0.99f;
        }

        public override ISimulationInfoUIController AddSimulationInfoUIController(GameObject gameObject)
        {
            var explorationInfoUIController = gameObject.AddComponent<ExplorationInfoUIController>();
            explorationInfoUIController.Simulation = this;

            SimInfoUIController = explorationInfoUIController;

            return explorationInfoUIController;
        }

        public override void OnSimulationFinished()
        {
            if (GlobalSettings.ShouldWriteCSVResults)
            {
                CreateStatisticsFile();
            }
        }

        public override void OnDestory()
        {
            DestroyImmediate(explorationVisualizer.gameObject);
            DestroyImmediate(RobotSpawner.gameObject);
        }

        private void CreateStatisticsFile() {
            var csvWriter = new ExplorationStatisticsCSVWriter(this,$"{_scenario.StatisticsFileName}");
            csvWriter.CreateCSVFile(",");
        }
    }
}