using Maes.Algorithms;
using Maes.Map;
using Maes.Map.MapGen;
using Maes.Robot;

using UnityEngine;

namespace Maes.Utilities
{
    public class EstimateTickTimeCalculator : MonoBehaviour
    {
        public int EstimateTick(float angleRelativeToXAxis, Vector2Int start, Vector2Int targetTile, SimulationMap<Tile> collisionMap,
            RobotConstraints robotConstraints, int seed)
        {
            var prefab = Resources.Load<GameObject>("MaesRobot2D");
            var robotGameObject = Instantiate(prefab);
            var robot = robotGameObject.GetComponent<MonaRobot>();

            var relativeSize = robotConstraints.AgentRelativeSize;
            robot.transform.localScale = new Vector3(
                0.495f * relativeSize,
                0.495f * relativeSize,
                0.495f * relativeSize
            );

            robot.outLine.enabled = false;

            const float rtOffset = 0.01f; // Offset is used, since being exactly at integer value positions can cause issues with ray tracing
            const float marchingSquareOffset = 0.5f; // Offset to put robots back on coarsemap tiles instead of marching squares.
            robot.transform.position = new Vector3(start.x + rtOffset + collisionMap.ScaledOffset.x + marchingSquareOffset,
                start.y + rtOffset + collisionMap.ScaledOffset.y + marchingSquareOffset);
            robot.transform.rotation = Quaternion.Euler(0, 0, angleRelativeToXAxis - 90);

            robot.id = 1;
            var alg = new MoveToTargetTileAlgorithm { TargetTile = targetTile };
            robot.Algorithm = alg;
            robot.Controller.CommunicationManager = new CommunicationManager(collisionMap, robotConstraints, new DebuggingVisualizer());
            robot.Controller.SlamMap = new SlamMap(collisionMap, robotConstraints, seed);
            robot.Controller.Constraints = robotConstraints;
            robot.Algorithm.SetController(robot.Controller);


            Physics2D.simulationMode = SimulationMode2D.Script;

            var physicsTicksSinceUpdate = 0;
            while (!alg.TargetReached)
            {
                robot.PhysicsUpdate();
                physicsTicksSinceUpdate++;
                Physics2D.Simulate(GlobalSettings.PhysicsTickDeltaSeconds);
                if (physicsTicksSinceUpdate < GlobalSettings.PhysicsTicksPerLogicUpdate)
                {
                    continue;
                }

                var slamMap = robot.Controller.SlamMap;
                slamMap.UpdateApproxPosition(robot.transform.position);
                slamMap.SetApproxRobotAngle(robot.Controller.GetForwardAngleRelativeToXAxis());
                robot.LogicUpdate();
                physicsTicksSinceUpdate = 0;
            }

            Destroy(robotGameObject);

            return alg.Tick;
        }
    }

    public class MoveToTargetTileAlgorithm : IAlgorithm
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