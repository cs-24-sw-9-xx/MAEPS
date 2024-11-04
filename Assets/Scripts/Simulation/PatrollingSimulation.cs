using System.Collections.Generic;
using UnityEngine;

using Maes.Algorithms;
using Maes.Map;
using Maes.Map.MapGen;
using Maes.Map.MapPatrollingGen;
using MAES.Map.RobotSpawners;
using Maes.Simulation;
using MAES.Simulation.SimulationScenarios;
using Maes.Statistics;
using Maes.Trackers;
using MAES.UI.SimulationInfoUIControllers;

namespace Maes
{
    public sealed class PatrollingSimulation : SimulationBase<PatrollingSimulation, PatrollingVisualizer, Tile, PatrollingTracker, PatrollingInfoUIController, IPatrollingAlgorithm, PatrollingSimulationScenario, PatrollingRobotSpawner>
    {
        public PatrollingVisualizer patrollingVisualizer;

        public PatrollingTracker PatrollingTracker;

        public override PatrollingVisualizer Visualizer => patrollingVisualizer;

        public override PatrollingTracker Tracker => PatrollingTracker;
        
        private PatrollingMap _patrollingMap;

        protected override void AfterCollisionMapGenerated(PatrollingSimulationScenario scenario)
        {
            _patrollingMap = scenario.PatrollingMapFactory(new PatrollingMapSpawner(), _collisionMap);
            
            PatrollingTracker = new PatrollingTracker(this, scenario.RobotConstraints, _patrollingMap);
            
            patrollingVisualizer.SetSimulationMap(_collisionMap, _collisionMap.ScaledOffset);
            patrollingVisualizer.SetPatrollingMap(_patrollingMap);
            
            RobotSpawner.SetPatrolling(_patrollingMap, PatrollingTracker);
        }

        public override bool HasFinishedSim()
        {
            // TODO: Implement
            return false;
        }
    }
}