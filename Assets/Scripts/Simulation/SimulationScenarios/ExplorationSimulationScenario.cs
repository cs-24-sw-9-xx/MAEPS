using Maes;
using Maes.Algorithms;
using Maes.ExplorationAlgorithm.RandomBallisticWalk;
using Maes.Robot;

using UnityEngine;

namespace MAES.Simulation.SimulationScenarios
{
    public sealed class ExplorationSimulationScenario : SimulationScenario<ExplorationSimulation, IExplorationAlgorithm>
    {
        public ExplorationSimulationScenario(
            int seed,
            SimulationEndCriteriaDelegate<ExplorationSimulation>
                hasFinishedSim = null,
            MapFactory mapSpawner = null,
            RobotFactory<IExplorationAlgorithm> robotSpawner = null,
            RobotConstraints? robotConstraints = null,
            string statisticsFileName = null)
            : base(seed,
                robotSpawner ?? ((map, spawner) => spawner.SpawnRobotsTogether(map, seed, 1, Vector2Int.zero, (robotSeed) => new RandomExplorationAlgorithm(robotSeed))),
                hasFinishedSim,
                mapSpawner,
                robotConstraints,
                statisticsFileName)
        {
        }
    }
}