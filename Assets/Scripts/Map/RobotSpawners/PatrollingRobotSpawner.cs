using Maes.Algorithms;
using Maes.Map;
using Maes.Map.MapGen;
using Maes.Robot;

namespace MAES.Map.RobotSpawners
{
    public class PatrollingRobotSpawner : RobotSpawner<IPatrollingAlgorithm>
    {
        private PatrollingMap _patrollingMap;
        
        public void SetPatrollingMap(PatrollingMap map)
        {
            _patrollingMap = map;
        }
        
        protected override MonaRobot CreateRobot(float x, float y, float relativeSize, int robotId, IPatrollingAlgorithm algorithm,
            SimulationMap<Tile> collisionMap, int seed)
        {
            var robot = base.CreateRobot(x, y, relativeSize, robotId, algorithm, collisionMap, seed);

            algorithm.SetPatrollingMap((PatrollingMap)_patrollingMap.Clone());
            
            return robot;
        }
    }
}