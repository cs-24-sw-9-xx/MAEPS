using Maes.Map.MapGen;
using Maes.Simulation;
using Maes.Statistics;
using Maes.Trackers;
using Maes.UI;
using UnityEngine;

namespace Maes
{
    public sealed class PatrollingSimulation : SimulationBase<PatrollingSimulation, PatrollingVisualizer, Tile, PatrollingTracker, PatrollingInfoUIController>
    {
        public GameObject patrollingVisualizerPrefab;
        public PatrollingVisualizer patrollingVisualizer;

        public PatrollingTracker PatrollingTracker;

        public override PatrollingVisualizer Visualizer => patrollingVisualizer;

        public override PatrollingTracker Tracker => PatrollingTracker;
        
        protected override void AfterStart()
        {
            patrollingVisualizerPrefab = Resources.Load<GameObject>("PatrollingVisualizer");
            patrollingVisualizer = Instantiate(patrollingVisualizerPrefab).GetComponent<PatrollingVisualizer>();
        }

        public override void SetScenario(SimulationScenario<PatrollingSimulation> scenario)
        {
            base.SetScenario(scenario);

            PatrollingTracker = new PatrollingTracker(scenario.RobotConstraints);
            patrollingVisualizer.SetMap(_collisionMap, _collisionMap.ScaledOffset);
        }

        public override void OnDestory()
        {
            DestroyImmediate(patrollingVisualizer.gameObject);
        }

        public override bool HasFinishedSim()
        {
            // TODO: Implement
            return false;
        }

        public override ISimulationInfoUIController AddSimulationInfoUIController(GameObject gameObject)
        {
            var patrollingInfoUIController = gameObject.AddComponent<PatrollingInfoUIController>();
            patrollingInfoUIController.Simulation = this;

            SimInfoUIController = patrollingInfoUIController;

            return patrollingInfoUIController;
        }
    }
}