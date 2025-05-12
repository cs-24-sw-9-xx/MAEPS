using Maes.Algorithms.Exploration;
using Maes.Algorithms.Exploration.RandomBallisticWalk;
using Maes.FaultInjections;
using Maes.Robot;

using UnityEngine;

namespace Maes.Simulation.Exploration
{
    public sealed class ExplorationSimulationScenario : SimulationScenario<ExplorationSimulation, IExplorationAlgorithm>
    {
        public ExplorationSimulationScenario(
            int seed,
            SimulationEndCriteriaDelegate<ExplorationSimulation>?
                hasFinishedSim = null,
            MapFactory? mapSpawner = null,
            RobotFactory<IExplorationAlgorithm>? robotSpawner = null,
            RobotConstraints? robotConstraints = null,
            string? statisticsFileName = null,
            IFaultInjection? faultInjection = null,
            int maxLogicTicks = DefaultMaxLogicTicks)
            : base(seed,
                robotSpawner ?? ((map, spawner) => spawner.SpawnRobotsTogether(map, seed, 1, Vector2Int.zero, robotSeed => new RandomExplorationAlgorithm(robotSeed))),
                hasFinishedSim,
                mapSpawner,
                robotConstraints,
                statisticsFileName,
                faultInjection,
                maxLogicTicks)
        {
        }
    }
}