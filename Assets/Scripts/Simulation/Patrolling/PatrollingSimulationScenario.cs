using Maes.Algorithms.Patrolling;
using Maes.FaultInjections;
using Maes.Map;
using Maes.Map.Generators;
using Maes.Map.Generators.Patrolling.Partitioning;
using Maes.Robot;

namespace Maes.Simulation.Patrolling
{
    public delegate PatrollingMap PatrollingMapFactory(SimulationMap<Tile> map);

    public sealed class PatrollingSimulationScenario : SimulationScenario<PatrollingSimulation, IPatrollingAlgorithm>
    {
        public PatrollingMapFactory PatrollingMapFactory { get; }
        public int TotalCycles { get; }
        public bool StopAfterDiff { get; }
        public int Partitions { get; }

        public PatrollingSimulationScenario(int seed,
            int totalCycles,
            bool stopAfterDiff,
            RobotFactory<IPatrollingAlgorithm> robotSpawner,
            MapFactory? mapSpawner = null,
            RobotConstraints? robotConstraints = null,
            string? statisticsFileName = null,
            PatrollingMapFactory? patrollingMapFactory = null,
            IFaultInjection? faultInjection = null,
            int partitions = 1)
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
            Partitions = partitions;
            PatrollingMapFactory = patrollingMapFactory ?? ((map) => PartitioningGenerator.MakePatrollingMapWithSpectralBisectionPartitions(map, partitions, robotConstraints));
        }
    }
}