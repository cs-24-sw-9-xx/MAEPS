using Maes.Algorithms;
using Maes.Map;
using Maes.Map.MapGen;
using Maes.Map.MapPatrollingGen;
using Maes.PatrollingAlgorithms;
using Maes.Robot;

using UnityEngine;

namespace Maes.Simulation.SimulationScenarios
{
    public delegate PatrollingMap PatrollingMapFactory(SimulationMap<Tile> map);

    public sealed class PatrollingSimulationScenario : SimulationScenario<PatrollingSimulation, IPatrollingAlgorithm>
    {
        public PatrollingMapFactory PatrollingMapFactory { get; }
        public int TotalCycles { get; }
        public bool StopAfterDiff { get; }

        public PatrollingSimulationScenario(
            int seed,
            int totalCycles,
            bool stopAfterDiff,
            MapFactory? mapSpawner = null,
            RobotFactory<IPatrollingAlgorithm>? robotSpawner = null,
            RobotConstraints? robotConstraints = null,
            string? statisticsFileName = null,
            PatrollingMapFactory? patrollingMapFactory = null
            )
            : base(seed,
                robotSpawner,
                null,
                mapSpawner,
                robotConstraints,
                statisticsFileName)
        {
            TotalCycles = totalCycles;
            StopAfterDiff = stopAfterDiff;
            PatrollingMapFactory = patrollingMapFactory ?? ((map) => WatchmanRouteSolver.MakePatrollingMap(map));
        }
    }
}