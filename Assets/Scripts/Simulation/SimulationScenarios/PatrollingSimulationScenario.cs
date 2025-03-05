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

        public int Partitions { get; set; }

        /// <summary>
        /// Initializes a new instance of the PatrollingSimulationScenario class with specified simulation parameters and configuration options.
        /// </summary>
        /// <param name="seed">A seed value for random number generation used in simulation initialization.</param>
        /// <param name="totalCycles">The total number of simulation cycles the scenario will execute.</param>
        /// <param name="stopAfterDiff">Indicates whether the simulation should terminate after detecting a difference in simulation state.</param>
        /// <param name="robotSpawner">A factory for creating robots that implement the IPatrollingAlgorithm interface.</param>
        /// <param name="mapSpawner">An optional factory for creating simulation maps.</param>
        /// <param name="robotConstraints">Optional constraints to apply on robots within the simulation.</param>
        /// <param name="statisticsFileName">An optional file name for logging simulation statistics.</param>
        /// <param name="patrollingMapFactory">
        /// An optional delegate for generating a patrolling map from a simulation map. If not provided, a default implementation using GreedyWaypointGenerator—and influenced by the showIslands flag—is used.
        /// </param>
        /// <param name="faultInjection">An optional service for injecting faults during the simulation.</param>
        /// <param name="showIslands">Determines whether islands are included in the generated patrolling map.</param>
        /// <param name="partitions">Specifies the number of partitions for the simulation; defaults to 1.</param>
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