using Maes.Algorithms;
using Maes.Map.MapGen;
using MAES.Map.RobotSpawners;
using Maes.Simulation;
using MAES.Simulation.SimulationScenarios;
using Maes.Statistics;
using Maes.Trackers;
using Maes.UI;

using MAES.UI.SimulationInfoUIControllers;

using UnityEngine;

namespace Maes
{
    public sealed class PatrollingSimulation : SimulationBase<PatrollingSimulation, PatrollingVisualizer, Tile, PatrollingTracker, PatrollingInfoUIController, IPatrollingAlgorithm, PatrollingSimulationScenario>
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
            
            RobotSpawner = gameObject.AddComponent<PatrollingRobotSpawner>();
        }

        public override void SetScenario(PatrollingSimulationScenario scenario)
        {
            base.SetScenario(scenario);

            PatrollingTracker = new PatrollingTracker(scenario.RobotConstraints);
            patrollingVisualizer.SetMap(_collisionMap, _collisionMap.ScaledOffset);
        }

        public override void OnDestory()
        {
            DestroyImmediate(patrollingVisualizer.gameObject);
            DestroyImmediate(RobotSpawner.gameObject);
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