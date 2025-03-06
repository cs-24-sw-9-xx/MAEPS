using Maes.Algorithms;
using Maes.FaultInjections;
using Maes.Map;
using Maes.Map.MapGen;
using Maes.Map.MapPatrollingGen;
using Maes.Robot;

namespace Maes.Simulation.SimulationScenarios
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
            bool showIslands = false,
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
            PatrollingMapFactory = patrollingMapFactory ?? ((map) => GreedyWaypointGenerator.MakePatrollingMap(map, showIslands));
        }
    }
}