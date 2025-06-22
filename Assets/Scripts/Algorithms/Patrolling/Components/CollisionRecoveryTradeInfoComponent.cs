using System;
using System.Collections.Generic;
using System.Linq;

using Maes.Map;
using Maes.Robot;
using Maes.Robot.Tasks;

using UnityEngine;

using Random = UnityEngine.Random;

namespace Maes.Algorithms.Patrolling.Components
{
    public class CollisionRecoveryTradeInfoComponent : IComponent
    {
        private IRobotController _controller;
        
        private readonly IMovementComponent _movementComponent;
        
        private readonly IPatrollingAlgorithm _algorithm;
        
        private readonly PatrollingMap _map;

        
        public int PreUpdateOrder => -100;
        public int PostUpdateOrder => -100;


        public CollisionRecoveryTradeInfoComponent(IRobotController controller, IPatrollingAlgorithm algorithm, PatrollingMap map, IMovementComponent movementComponent)
        {
            _controller = controller;
            _movementComponent = movementComponent;
            _algorithm = algorithm;
            _map = map;
        }
        
        public IEnumerable<ComponentWaitForCondition> PreUpdateLogic()
        {
            while (true)
            {
                if (_controller.IsCurrentlyColliding)
                {
                    _controller.Broadcast(new CollisionMessage(
                        _controller.Id,
                        _algorithm.LogicTicks,
                        _controller.AssignedPartition,
                        _map.Vertices.Where(x => x.Partition == _controller.AssignedPartition).Select(v => (v.Id, v.LastTimeVisitedTick)).ToList(),
                        _movementComponent.ApproachingVertex.Id));
                    yield return ComponentWaitForCondition.WaitForLogicTicks(1, false);
                    
                    
                    var messages = _controller.ReceiveBroadcast();
                    Vertex target = null!;
                    foreach (var message in messages)
                    {
                        if (message is not CollisionMessage collisionMessage)
                        {
                            continue;
                        }

                        _controller.AssignedPartition = collisionMessage.PartitionId;
                        foreach (var vertexInfo in collisionMessage.VerticesInfo)
                        {
                            var vertex = _map.Vertices.Single(v => v.Id == vertexInfo.id);
                            vertex.LastTimeVisitedTick = Math.Max(vertexInfo.lastTimeVisistedTick, vertex.LastTimeVisitedTick);
                        }
                        target = _map.Vertices.Single(v => v.Id == collisionMessage.TargetId);
                        Debug.Log($"Robot {_controller.Id} changed target from {_movementComponent.ApproachingVertex} to {target} from robot {collisionMessage.RobotId}");
                        if (target == _movementComponent.ApproachingVertex)
                        {
                            target = target.Neighbors.ElementAt(Random.Range(0, target.Neighbors.Count));
                            Debug.Log($"Robots moving to same vertex, picking random neighbor instead: {target}");
                        }
                    }
                    

                    if (messages.Any(m => m is CollisionMessage))
                    {
                        yield return ComponentWaitForCondition.WaitForRobotStatus(RobotStatus.Idle, false);
                        _controller.Rotate(45);
                        yield return ComponentWaitForCondition.WaitForRobotStatus(RobotStatus.Idle, false);
                        _controller.Move(0.75f, reverse: true);
                        yield return ComponentWaitForCondition.WaitForRobotStatus(RobotStatus.Idle, false);
                        _movementComponent.AbortCurrentTask(new AbortingTask(target));
                    }
                    else
                    {
                        yield return ComponentWaitForCondition.WaitForRobotStatus(RobotStatus.Idle, false);
                        _controller.Move(0.5f, reverse: true);
                        yield return ComponentWaitForCondition.WaitForRobotStatus(RobotStatus.Idle, false);
                        if (_controller.IsCurrentlyColliding)
                        {
                            _controller.Rotate(45);
                            yield return ComponentWaitForCondition.WaitForRobotStatus(RobotStatus.Idle, false);
                            _controller.Move(0.5f, reverse: false);
                            yield return ComponentWaitForCondition.WaitForRobotStatus(RobotStatus.Idle, false);
                        }
                        
                        _movementComponent.AbortCurrentTask(new AbortingTask(_movementComponent.ApproachingVertex));
                    }
                }
                yield return ComponentWaitForCondition.WaitForLogicTicks(1, true);
            }
        }
        
        private RelativePosition GetRelativePositionTo(Vector2Int position)
        {
            return _controller.SlamMap.CoarseMap.GetTileCenterRelativePosition(position, dependOnBrokenBehaviour: false);
        }
    }
    
    public sealed class CollisionMessage
    {
        public int RobotId { get; }
        public int LogicTick { get; }
        
        public int PartitionId { get; }
        
        public List<(int id, int lastTimeVisistedTick)> VerticesInfo { get; }
        
        public int TargetId { get; }

        public CollisionMessage(int robotId, int logicTick, int partitionId, List<(int, int)> verticesInfo, int targetId)
        {
            RobotId = robotId;
            LogicTick = logicTick;
            PartitionId = partitionId;
            VerticesInfo = verticesInfo;
            TargetId = targetId;
        }
    }
}