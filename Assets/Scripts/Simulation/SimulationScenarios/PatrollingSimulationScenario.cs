using Maes;
using Maes.Algorithms;
using Maes.Map;
using Maes.Map.MapGen;
using Maes.Map.MapPatrollingGen;

using MAES.PatrollingAlgorithms;

using Maes.Robot;

using UnityEngine;

namespace MAES.Simulation.SimulationScenarios
{
    public delegate PatrollingMap PatrollingMapFactory(PatrollingMapSpawner generator, SimulationMap<Tile> map);
    
    public sealed class PatrollingSimulationScenario : SimulationScenario<PatrollingSimulation, IPatrollingAlgorithm>
    {
        
        public PatrollingMapFactory PatrollingMapFactory { get; }
        public PatrollingSimulationScenario(
            int seed,
            SimulationEndCriteriaDelegate<PatrollingSimulation> hasFinishedSim=null,
            MapFactory mapSpawner=null,
            RobotFactory<IPatrollingAlgorithm> robotSpawner=null,
            RobotConstraints? robotConstraints=null,
            string statisticsFileName=null,
            PatrollingMapFactory patrollingMapFactory=null
            ) 
            : base(seed,
                robotSpawner ?? ((map, spawner) => spawner.SpawnRobotsTogether(map, seed, 1, Vector2Int.zero, (robotSeed) => new ConscientiousReactiveAlgorithm())),
                hasFinishedSim, 
                mapSpawner,
                robotConstraints,
                statisticsFileName)
        {
            PatrollingMapFactory = patrollingMapFactory ?? ((generator, map) => generator.GeneratePatrollingMapRetanglesBased(map));
        }
    }
}