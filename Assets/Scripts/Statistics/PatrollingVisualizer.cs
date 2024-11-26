using System.Collections.Generic;

using Maes.Map;
using Maes.Robot;
using Maes.Trackers;
using Maes.UI.Patrolling;

using UnityEngine;

namespace Maes.Statistics
{
    public class PatrollingVisualizer : Visualizer<PatrollingCell>
    {
        public GameObject VertexVisualizer = null!;
        public GameObject EdgeVisualizer = null!;

        private readonly List<GameObject> _visualizerObjects = new();

        private readonly Dictionary<int, VertexVisualizer> _vertexVisualizers = new();

        public override void SetSimulationMap(SimulationMap<PatrollingCell> simulationMap, Vector3 offset)
        {
            base.SetSimulationMap(simulationMap, Vector3.zero);

            foreach (var visualizer in _visualizerObjects)
            {
                Destroy(visualizer);
            }
        }

        public void CreateVisualizers(Dictionary<int, VertexDetails> vertexDetails, PatrollingMap patrollingMap)
        {
            foreach (var (_, vertexDetail) in vertexDetails)
            {
                var vertex = vertexDetail.Vertex;

                var vertexVisualizerObject = Instantiate(VertexVisualizer, transform);
                vertexVisualizerObject.transform.localPosition = (Vector2)vertex.Position;

                var vertexVisualizer = vertexVisualizerObject.GetComponent<VertexVisualizer>();
                vertexVisualizer.SetVertexDetails(vertexDetail);

                _visualizerObjects.Add(vertexVisualizerObject);
                _vertexVisualizers.Add(vertex.Id, vertexVisualizer);

                foreach (var otherVertex in vertex.Neighbors)
                {
                    var edgeVisualizer = Instantiate(EdgeVisualizer, transform);
                    var lineRenderer = edgeVisualizer.GetComponent<LineRenderer>();

                    var paths = patrollingMap.Paths[(vertex.Id, otherVertex.Id)];
                    lineRenderer.positionCount = paths.Length + 1;
                    lineRenderer.SetPosition(0, ((Vector3)(Vector2)paths[0].Start) + transform.position + Vector3.back);
                    for (var i = 0; i < paths.Length; i++)
                    {
                        lineRenderer.SetPosition(i + 1, ((Vector3)(Vector2)paths[i].End) + transform.position + Vector3.back);
                    }
                    lineRenderer.material.color = vertex.Color;

                    _visualizerObjects.Add(edgeVisualizer);
                }
            }
        }

        public void ResetWaypointsColor()
        {
            foreach (var (_, vertex) in _vertexVisualizers)
            {
                vertex.ShowDefaultWaypointColor();
            }
        }

        public void ResetRobotHighlighting(IEnumerable<MonaRobot> robots, MonaRobot? selectedRobot)
        {
            foreach (var robot in robots)
            {
                robot.outLine.enabled = false;
            }
            if (selectedRobot != null)
            {
                selectedRobot.outLine.enabled = true;
            }
        }

        public void ShowWaypointHeatMap(int currentTick)
        {
            foreach (var (_, vertexVisualizer) in _vertexVisualizers)
            {
                var vertex = vertexVisualizer.VertexDetails.Vertex;

                if (vertex.NumberOfVisits == 0)
                {
                    continue;
                }

                var ticksSinceLastExplored = currentTick - vertex.LastTimeVisitedTick;
                var coldness = Mathf.Min((float)ticksSinceLastExplored / (float)GlobalSettings.TicksBeforeWaypointCoverageHeatMapCold, 1.0f);
                var color = Color32.Lerp(ExplorationVisualizer.WarmColor, ExplorationVisualizer.ColdColor, coldness);
                vertexVisualizer.SetWaypointColor(color);
            }
        }

        public void ShowTargetWaypoint(Vertex targetVertex)
        {
            var yellowColor = new Color(255, 255, 0, 255);
            _vertexVisualizers[targetVertex.Id].SetWaypointColor(yellowColor);
        }

        public void ShowRobotsHighlighting(IEnumerable<MonaRobot> robots)
        {
            foreach (var robot in robots)
            {
                robot.outLine.enabled = true;
            }
        }

        public void ShowDefaultColor(Vertex vertex)
        {
            if (_vertexVisualizers.TryGetValue(vertex.Id, out var vertexObject))
            {
                vertexObject.ShowDefaultWaypointColor();
            }
            else
            {
                Debug.LogError($"Vertex {vertex.Position} not found in visualizer");
            }
        }
    }
}