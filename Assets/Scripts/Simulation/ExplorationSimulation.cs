using Maes.Simulation;
using Maes.Statistics;
using Maes.UI;
using MAES.UI.RestartRemakeContollers;
using UnityEngine;
using UnityEngine.Serialization;

namespace Maes
{
    public sealed class ExplorationSimulation : SimulationBase<ExplorationSimulation, ExplorationVisualizer, ExplorationCell, ExplorationTracker, ExplorationInfoUIController>
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
        }

        public override void SetScenario(SimulationScenario<ExplorationSimulation> scenario)
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
        }

        private void CreateStatisticsFile()
        {
            var csvWriter = new ExplorationStatisticsCSVWriter(this, $"{_scenario.StatisticsFileName}");
            csvWriter.CreateCSVFile(",");
        }
    }
}