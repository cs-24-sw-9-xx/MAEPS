// Copyright 2024
// Contributors: Henneboy
// This algorithm is not optimal:
// - Robots can get stuck when they collide with eachother
// - Robots do not share their targets/intentions, i.e. they don't coordinate.
//   - Proposed fix: When finding the nearest unexplored tile, the robot could exclude tiles which are close to other robots.
// - The anti-wall collision 'CollisionCorrector()' is primitive.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Maes.Map;
using Maes.Robot;
using Maes.Robot.Task;

using UnityEngine;

namespace Maes.Algorithms.Exploration.HenrikAlgo
{
    public class HenrikExplorationAlgorithm : IExplorationAlgorithm
    {
        // HACK
        private IRobotController _robotController = null!;
        private Vector2Int? _targetTile;
        private uint _ticksSinceHeartbeat;

        private int _logicTicks;

        public void SetController(Robot2DController controller)
        {
            _robotController = controller;
        }

        public IEnumerable<WaitForCondition> PreUpdateLogic()
        {
            while (true)
            {
                yield return WaitForCondition.ContinueUpdateLogic();
            }
        }

        public IEnumerable<WaitForCondition> UpdateLogic()
        {
            while (true)
            {
                _logicTicks++;
                ShareSlamMap();
                if (_robotController.GetStatus() == RobotStatus.Idle)
                {
                    _targetTile = _robotController.GetSlamMap().CoarseMap.GetNearestTileFloodFill(_robotController.GetSlamMap().CoarseMap.GetCurrentPosition(), SlamMap.SlamTileStatus.Unseen);
                }
                if (_targetTile != null)
                {
                    _robotController.PathAndMoveTo(_targetTile.Value);
                    if (_robotController.GetSlamMap().CoarseMap.IsTileExplored(_targetTile.Value))
                    {
                        _targetTile = null;
                        _robotController.StopCurrentTask();
                    }
                }

                yield return WaitForCondition.WaitForLogicTicks(1);
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
            var receivedHeartbeats = _robotController.ReceiveBroadcast().OfType<HeartbeatMessage>().ToArray();
            if (receivedHeartbeats.Length > 1)
            {
                Debug.Log("Received slam");
                var combinedMessage = receivedHeartbeats[0];
                foreach (var message in receivedHeartbeats.AsSpan(1))
                {
                    combinedMessage = combinedMessage.Combine(message, _logicTicks);
                }
            }
            _ticksSinceHeartbeat++;
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
        private readonly SlamMap _map;

        public HeartbeatMessage(SlamMap map)
        {
            _map = map;
        }

        public HeartbeatMessage Combine(HeartbeatMessage heartbeatMessage, int tick)
        {
            List<SlamMap> maps = new() { heartbeatMessage._map, _map };
            SlamMap.Synchronize(maps, tick); //layers of pass by reference, map in controller is updated with the info from message
            return this;
        }

        public HeartbeatMessage Process() //Combine all, then process, but not really anything to process for heartbeat
        {
            return this;
        }
    }
}