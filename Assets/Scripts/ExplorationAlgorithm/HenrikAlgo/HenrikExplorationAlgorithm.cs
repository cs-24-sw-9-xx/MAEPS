// Copyright 2024
// Contributors: Henneboy
// This algorithm is not optimal:
// - Robots can get stuck when they collide with eachother
// - Robots do not share their targets/intentions, i.e. they don't coordinate.
//   - Proposed fix: When finding the nearest unexplored tile, the robot could exclude tiles which are close to other robots.
// - The anti-wall collision 'CollisionCorrector()' is primitive.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Maes.Map;
using Maes.Robot;
using Maes.Robot.Task;
using UnityEngine;

namespace Maes.ExplorationAlgorithm.HenrikAlgo
{
    public class HenrikExplorationAlgorithm : IExplorationAlgorithm
    {
        private IRobotController _robotController;
        private Vector2Int? _targetTile = null;
        private uint _ticksSinceHeartbeat = 0;
        public HenrikExplorationAlgorithm()
        {
        }

        public HenrikExplorationAlgorithm(Robot2DController robotControllerController)
        {
            _robotController = robotControllerController;
        }

        public void SetController(Robot2DController controller)
        {
            this._robotController = controller;
        }

        public void UpdateLogic()
        {
            ShareSlamMap();

            var status = _robotController.GetStatus();
            if (status == RobotStatus.Idle)
            {
                CollisionCorrector();
                _targetTile = _robotController.GetSlamMap().CoarseMap.GetNearestTileFloodFill(_robotController.GetSlamMap().CoarseMap.GetCurrentPosition(), SlamMap.SlamTileStatus.Unseen);
            }
            if (_targetTile != null)
            {
                _robotController.PathAndMoveTo(_targetTile.Value);
                if (_robotController.GetSlamMap().CoarseMap.IsTileExplored(_targetTile.Value))
                {
                    _targetTile = null;
                    _robotController.StopCurrentTask();
                    return;
                }
                return;
            }
        }

        private void ShareSlamMap()
        {
            if (_ticksSinceHeartbeat == 10)
            {
                Debug.Log("Sent slam");
                var ownHeartbeat = new HeartbeatMessage(_robotController.GetSlamMap());
                _ticksSinceHeartbeat = 0;
                _robotController.Broadcast(ownHeartbeat);
            }
            var receivedHeartbeat = new Queue<HeartbeatMessage>(_robotController.ReceiveBroadcast().OfType<HeartbeatMessage>());
            if (receivedHeartbeat.Count > 1)
            {
                Debug.Log("Recieved slam");
                var combinedMessage = receivedHeartbeat.Dequeue();
                foreach (var message in receivedHeartbeat)
                {
                    combinedMessage = combinedMessage.Combine(message);
                }
            }
            _ticksSinceHeartbeat++;
        }

        private void CollisionCorrector()
        {
            if (_robotController.IsCurrentlyColliding())
            {
                _robotController.Move(0.2f, true);
                _robotController.Rotate(45);
                _robotController.Move(0.2f, true);
            }
        }

        public string GetDebugInfo()
        {
            var info = new StringBuilder();
            info.AppendLine($"Target: {_targetTile}.");
            info.AppendLine($"Current position: {_robotController.GetSlamMap().CoarseMap.GetCurrentPosition()}");
            info.AppendLine($"Status: {_robotController.GetStatus()}.");

            return info.ToString();
        }
    }

    public class HeartbeatMessage
    {
        internal SlamMap map;

        public HeartbeatMessage(SlamMap map)
        {
            this.map = map;
        }

        public HeartbeatMessage Combine(HeartbeatMessage otherMessage)
        {
            if (otherMessage is HeartbeatMessage heartbeatMessage)
            {
                List<SlamMap> maps = new() { heartbeatMessage.map, map };
                SlamMap.Synchronize(maps); //layers of pass by reference, map in controller is updated with the info from message
                return this;
            }
            return null;
        }

        public HeartbeatMessage Process() //Combine all, then process, but not really anything to process for heartbeat
        {
            return this;
        }
    }
}