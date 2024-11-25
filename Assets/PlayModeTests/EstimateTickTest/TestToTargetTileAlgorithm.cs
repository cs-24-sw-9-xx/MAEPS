using Maes.Algorithms;
using Maes.Robot;

using UnityEngine;

namespace PlayModeTests.EstimateTickTest
{
    public class MoveToTargetTileAlgorithm : IExplorationAlgorithm
    {
        public int Tick { get; private set; }
        public Robot2DController Controller = null!;
        public Vector2Int TargetTile;
        public bool TargetReached;

        public void UpdateLogic()
        {
            if (!IsDestinationReached(TargetTile))
            {
                Controller.PathAndMoveTo(TargetTile);
                Tick++;
                return;
            }

            TargetReached = true;
        }

        public void SetController(Robot2DController controller)
        {
            Controller = controller;
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