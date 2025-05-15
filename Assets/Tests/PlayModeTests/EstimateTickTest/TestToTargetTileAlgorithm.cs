using System.Collections.Generic;

using Maes.Algorithms;
using Maes.Algorithms.Exploration;
using Maes.Robot;

using UnityEngine;

namespace Tests.PlayModeTests.EstimateTickTest
{
    public class MoveToTargetTileAlgorithm : IExplorationAlgorithm
    {
        public MoveToTargetTileAlgorithm(Vector2Int parameter, bool isOffset = false)
        {
            if (isOffset)
            {
                _offset = parameter;
            }
            else
            {
                _targetTile = parameter;
            }
            _useOffset = isOffset;
        }

        private readonly bool _useOffset;
        private Vector2Int _offset;
        private Vector2Int _targetTile;
        private Vector2Int _startPosition;
        public int Tick { get; private set; }
        public Robot2DController Controller = null!;
        public Vector2Int TargetTile => _useOffset ? _startPosition + _offset : _targetTile;
        public bool TargetReached;
        public float? ExpectedEstimatedTicks;

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
                if (!IsDestinationReached(TargetTile) && !TargetReached)
                {
                    Controller.PathAndMoveTo(TargetTile);
                    Tick++;
                }
                else
                {
                    TargetReached = true;
                }

                yield return WaitForCondition.WaitForLogicTicks(1);
            }
        }

        public void SetController(Robot2DController controller)
        {
            Controller = controller;
            _startPosition = controller.SlamMap.CoarseMap.GetCurrentPosition();
            ExpectedEstimatedTicks = Controller.OverEstimateTimeToTarget(TargetTile);
        }

        public string GetDebugInfo()
        {
            return "Target: " + TargetTile + "\n" +
                   "Tick: " + Tick + "\n" +
                   "Target reached: " + TargetReached + "\n" +
                   "Rotation" + Controller.Transform.rotation.eulerAngles + "\n" +
                   "Angle: " + Controller.GetForwardAngleRelativeToXAxis() + "\n" +
                   "Distance to target" + Controller.SlamMap.CoarseMap.GetTileCenterRelativePosition(TargetTile).Distance;
        }

        private bool IsDestinationReached(Vector2Int tile)
        {
            return Controller.SlamMap.CoarseMap.GetTileCenterRelativePosition(tile).Distance < 0.25f;
        }
    }
}