using Maes.Map.MapGen;
using Maes.Simulation;
using Maes.Statistics;
using Maes.Trackers;
using Maes.UI;
using MAES.UI.RestartRemakeContollers;
using UnityEngine;
using UnityEngine.Serialization;

namespace Maes
{
    public sealed class PatrollingSimulation : SimulationBase<PatrollingSimulation, PatrollingVisualizer, Tile, PatrollingTracker, PatrollingInfoUIController>
    {
        public PatrollingVisualizer patrollingVisualizer;

        public PatrollingTracker PatrollingTracker;

        public override PatrollingVisualizer Visualizer => patrollingVisualizer;

        public override PatrollingTracker Tracker => PatrollingTracker;

        public override void SetScenario(SimulationScenario<PatrollingSimulation> scenario)
        {
            base.SetScenario(scenario);

            PatrollingTracker = new PatrollingTracker();
            patrollingVisualizer.SetMap(_collisionMap, _collisionMap.ScaledOffset);
        }

        public override bool HasFinishedSim()
        {
            // TODO: Implement
            return false;
        }

        public override ISimulationInfoUIController AddSimulationInfoUIController(GameObject gameObject)
        {
            throw new System.NotImplementedException();
        }
    }
}