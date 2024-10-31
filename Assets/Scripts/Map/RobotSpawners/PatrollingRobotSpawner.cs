using JetBrains.Annotations;

using Maes.Algorithms;
using Maes.ExplorationAlgorithm;
using Maes.Map;
using Maes.Map.MapGen;
using Maes.Robot;

namespace MAES.Map.RobotSpawners
{
    public class PatrollingRobotSpawner : RobotSpawner<IPatrollingAlgorithm>
    {
        [CanBeNull] private PatrollingMap _patrollingMap;
        protected override MonaRobot CreateRobot(float x, float y, float relativeSize, int robotId, IPatrollingAlgorithm algorithm,
            SimulationMap<Tile> collisionMap, int seed)
        {
            var robot = base.CreateRobot(x, y, relativeSize, robotId, algorithm, collisionMap, seed);

            _patrollingMap ??= new PatrollingMap(collisionMap);

            algorithm.SetPatrollingMap((PatrollingMap)_patrollingMap.Clone());
            
            return robot;
        }
    }
}