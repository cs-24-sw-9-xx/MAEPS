using Maes.Algorithms.Patrolling;
using Maes.Map.Generators;
using Maes.Robot;
using Maes.Statistics.Trackers;

using UnityEngine;

namespace Maes.Map.RobotSpawners
{
    public class PatrollingRobotSpawner : RobotSpawner<IPatrollingAlgorithm>
    {
        // HACK!
        private PatrollingTracker _tracker = null!;
        private PatrollingMap _patrollingMap = null!;

        public void SetPatrolling(PatrollingMap map, PatrollingTracker tracker)
        {
            _patrollingMap = map;
            _tracker = tracker;
        }

        protected override MonaRobot CreateRobot(float x, float y, float relativeSize, int robotId, IPatrollingAlgorithm algorithm,
            SimulationMap<Tile> collisionMap, int seed, Color32 color)
        {
            var robot = base.CreateRobot(x, y, relativeSize, robotId, algorithm, collisionMap, seed, color);

            algorithm.SetGlobalPatrollingMap(_patrollingMap);
            algorithm.SetPatrollingMap(_patrollingMap.Clone());
            algorithm.SubscribeOnReachVertex(_tracker.OnReachedVertex);

            return robot;
        }
    }
}