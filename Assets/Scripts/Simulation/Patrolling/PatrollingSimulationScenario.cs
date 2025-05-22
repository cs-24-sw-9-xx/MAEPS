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

        public bool SaveWaypointData { get; }

        public PatrollingSimulationScenario(int seed,
            int totalCycles,
            bool stopAfterDiff,
            RobotFactory<IPatrollingAlgorithm> robotSpawner,
            SimulationEndCriteriaDelegate<PatrollingSimulation>? hasFinishedSim = null,
            MapFactory? mapSpawner = null,
            RobotConstraints? robotConstraints = null,
            string? statisticsFileName = null,
            PatrollingMapFactory? patrollingMapFactory = null,
            IFaultInjection? faultInjection = null,
            int maxLogicTicks = DefaultMaxLogicTicks,
            int partitions = 1,
            bool saveWaypointData = false)
            : base(seed,
                robotSpawner,
                hasFinishedSim,
                mapSpawner,
                robotConstraints,
                statisticsFileName,
                faultInjection,
                maxLogicTicks)
        {
            TotalCycles = totalCycles;
            StopAfterDiff = stopAfterDiff;
            Partitions = partitions;
            SaveWaypointData = saveWaypointData;
            PatrollingMapFactory = patrollingMapFactory ?? ((map) =>
                PartitioningGenerator.MakePatrollingMapWithSpectralBisectionPartitions(map, partitions, RobotConstraints));
        }
    }
}