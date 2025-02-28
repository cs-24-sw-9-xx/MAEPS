using Maes.Algorithms;
using Maes.FaultInjections;
using Maes.Map;
using Maes.Map.MapGen;
using Maes.Map.Partitioning;
using Maes.Robot;

namespace Maes.Simulation.SimulationScenarios
{
    public delegate PatrollingMap PatrollingMapFactory(SimulationMap<Tile> map);

    public sealed class PatrollingSimulationScenario : SimulationScenario<PatrollingSimulation, IPatrollingAlgorithm>
    {
        public PatrollingMapFactory PatrollingMapFactory { get; }
        public int TotalCycles { get; }
        public bool StopAfterDiff { get; }

        public PatrollingSimulationScenario(int seed,
            int totalCycles,
            bool stopAfterDiff,
            RobotFactory<IPatrollingAlgorithm> robotSpawner,
            MapFactory? mapSpawner = null,
            RobotConstraints? robotConstraints = null,
            string? statisticsFileName = null,
            PatrollingMapFactory? patrollingMapFactory = null,
            IFaultInjection? faultInjection = null,
            bool showIslands = false)
            : base(seed,
                robotSpawner,
                null,
                mapSpawner,
                robotConstraints,
                statisticsFileName,
                faultInjection)
        {
            TotalCycles = totalCycles;
            StopAfterDiff = stopAfterDiff;
            PatrollingMapFactory = patrollingMapFactory ?? ((map) => PartitioningGen.MakePatrollingMapWithSpectralBisectionPartitions(map, showIslands, 4));
        }
    }
}