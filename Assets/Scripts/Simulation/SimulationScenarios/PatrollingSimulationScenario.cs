using Maes;
using Maes.Algorithms;
using Maes.PatrollingAlgorithm.ConscientiousReactive;
using Maes.Robot;

using UnityEngine;

namespace MAES.Simulation.SimulationScenarios
{
    public sealed class PatrollingSimulationScenario : SimulationScenario<PatrollingSimulation, IPatrollingAlgorithm>
    {
        public PatrollingSimulationScenario(
            int seed,
            SimulationEndCriteriaDelegate<PatrollingSimulation>
                hasFinishedSim=null, 
            MapFactory mapSpawner=null,
            RobotFactory<IPatrollingAlgorithm> robotSpawner=null,
            RobotConstraints? robotConstraints=null,
            string statisticsFileName=null) 
            : base(seed,
                robotSpawner ?? ((map, spawner) => spawner.SpawnRobotsTogether(map, seed, 1, Vector2Int.zero, (robotSeed) => new ConscientiousReactiveAlgorithm())),
                hasFinishedSim, 
                mapSpawner,
                robotConstraints,
                statisticsFileName)
        {
        }
    }
}