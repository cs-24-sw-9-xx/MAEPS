using JetBrains.Annotations;

using Maes.Algorithms;
using Maes.ExplorationAlgorithm;
using Maes.Map;
using Maes.Map.MapGen;
using Maes.Robot;

using UnityEngine;

namespace MAES.Map.RobotSpawners
{
    public class PatrollingRobotSpawner : RobotSpawner<IPatrollingAlgorithm>
    {
        public PatrollingMap PatrollingMap;
        protected override MonaRobot CreateRobot(float x, float y, float relativeSize, int robotId, IPatrollingAlgorithm algorithm,
            SimulationMap<Tile> collisionMap, int seed)
        {
            var robot = base.CreateRobot(x, y, relativeSize, robotId, algorithm, collisionMap, seed);

            algorithm.SetPatrollingMap((PatrollingMap)PatrollingMap.Clone());
            
            return robot;
        }
    }
}