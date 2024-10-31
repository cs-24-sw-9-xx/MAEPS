using System.Collections.Generic;
using System.Linq;

using Maes.Map;
using Maes.Robot;

using UnityEngine;

namespace Maes.Trackers
{
    public class VertexDetails : Vertex
    {
        public int NumberOfVisits { get; set; } = 0;
        public int MaxIdleness { get; private set; } = 0;
        
        public VertexDetails(float weight, Vector2Int position, Color? color = null) : base(weight, position, color)
        {
        }
        
        public VertexDetails(Vertex vertex) : base(vertex.Weight, vertex.Position, vertex.Color) { }
    }

    public class PatrollingTracker : ITracker
    {
        private readonly RobotConstraints _constraints;
        public PatrollingMap Map { get; private set; }
        
        public Dictionary<Vector2Int, VertexDetails> Vertices { get; }
        public int WorstGraphIdleness { get; private set; } = 0;
        public float TotalDistanceTraveled { get; private set; } = 0;
        public float CurrentGraphIdleness { get; private set; } = 0;

        private List<int> GraphIdlenessList { get; } = new();

        public float AverageGraphIdleness =>
            GraphIdlenessList.Count != 0 ? (float)GraphIdlenessList.Sum() / GraphIdlenessList.Count : 0;
        
        public PatrollingTracker(RobotConstraints constraints, PatrollingMap map)
        {
            _constraints = constraints;
            Vertices = map.Verticies.ToDictionary(vertex => vertex.Position, vertex => new VertexDetails(vertex));
        }

        public void OnReachedVertex(Vertex vertex)
        {
            Vertices[vertex.Position].NumberOfVisits++;
            Vertices[vertex.Position].VisitedAtTick(Time.frameCount);
        }
        
        public void LogicUpdate(IReadOnlyList<MonaRobot> robots)
        {
            
            if (_constraints.AutomaticallyUpdateSlam) {
                // Always update estimated robot position and rotation
                // regardless of whether the slam map was updated this tick
                foreach (var robot in robots) {
                    var slamMap = robot.Controller.SlamMap;
                    slamMap.UpdateApproxPosition(robot.transform.position);
                    slamMap.SetApproxRobotAngle(robot.Controller.GetForwardAngleRelativeToXAxis());
                }
            }
        }

        public void SetVisualizedRobot(MonaRobot robot)
        {
            // TODO: Implement
        }
    }
}