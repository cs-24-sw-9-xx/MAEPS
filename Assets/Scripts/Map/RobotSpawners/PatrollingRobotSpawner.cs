using Maes.Algorithms;
using Maes.Map;
using Maes.Map.MapGen;
using Maes.Robot;
using Maes.Trackers;

namespace MAES.Map.RobotSpawners
{
    public class PatrollingRobotSpawner : RobotSpawner<IPatrollingAlgorithm>
    {
        private PatrollingTracker _tracker;
        private PatrollingMap _patrollingMap;

        public void SetPatrolling(PatrollingMap map, PatrollingTracker tracker)
        {
            _patrollingMap = map;
            _tracker = tracker;
        }

        protected override MonaRobot CreateRobot(float x, float y, float relativeSize, int robotId, IPatrollingAlgorithm algorithm,
            SimulationMap<Tile> collisionMap, int seed)
        {
            var robot = base.CreateRobot(x, y, relativeSize, robotId, algorithm, collisionMap, seed);

            algorithm.SetPatrollingMap((PatrollingMap)_patrollingMap.Clone());
            algorithm.SubscribeOnReachVertex(_tracker.OnReachedVertex);

            return robot;
        }
    }
}