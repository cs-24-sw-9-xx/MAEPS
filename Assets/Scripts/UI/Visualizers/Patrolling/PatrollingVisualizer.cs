using System.Collections.Generic;

using Maes.Map;
using Maes.Map.Generators;
using Maes.Robot;
using Maes.Statistics;
using Maes.Statistics.Trackers;
using Maes.UI.Visualizers.Exploration;

using UnityEngine;

namespace Maes.UI.Visualizers.Patrolling
{
    public class PatrollingVisualizer : Visualizer
    {
        public static readonly Color32 PatrollingAreaColor = new(255, 120, 0, 255);
        public static readonly Color32 CommunicationColor = new(0, 255, 255, 255);

        public GameObject VertexVisualizer = null!;
        public GameObject EdgeVisualizer = null!;

        private readonly List<GameObject> _visualizerObjects = new();

        public Dictionary<int, VertexVisualizer> VertexVisualizers { get; } = new();

        public CommunicationZoneVertices CommunicationZoneVertices { get; private set; } = null!;

        public void SetCommunicationZoneVertices(SimulationMap<Tile> simulationMap, PatrollingMap patrollingMap, CommunicationManager communicationManager)
        {
            CommunicationZoneVertices = new CommunicationZoneVertices(simulationMap, patrollingMap, communicationManager);
        }

        public override void SetSimulationMap(SimulationMap<Cell> newMap)
        {
            transform.position = newMap.ScaledOffset;

            base.SetSimulationMap(newMap);

            foreach (var visualizer in _visualizerObjects)
            {
                Destroy(visualizer);
            }
        }

        public void CreateVisualizers(VertexDetails[] vertexDetails, PatrollingMap patrollingMap)
        {
            foreach (var vertexDetail in vertexDetails)
            {
                var vertex = vertexDetail.Vertex;

                var vertexVisualizerObject = Instantiate(VertexVisualizer, transform);
                vertexVisualizerObject.transform.localPosition = (Vector2)vertex.Position;

                var vertexVisualizer = vertexVisualizerObject.GetComponent<VertexVisualizer>();
                vertexVisualizer.SetVertexDetails(vertexDetail);

                _visualizerObjects.Add(vertexVisualizerObject);
                VertexVisualizers.Add(vertex.Id, vertexVisualizer);

                foreach (var otherVertex in vertex.Neighbors)
                {
                    var edgeVisualizer = Instantiate(EdgeVisualizer, transform);
                    var lineRenderer = edgeVisualizer.GetComponent<LineRenderer>();

                    var paths = patrollingMap.Paths[(vertex.Id, otherVertex.Id)];
                    lineRenderer.positionCount = paths.Count + 1;
                    lineRenderer.SetPosition(0, ((Vector3)(Vector2)paths[0].Start) + transform.position + Vector3.back);
                    for (var i = 0; i < paths.Count; i++)
                    {
                        lineRenderer.SetPosition(i + 1, ((Vector3)(Vector2)paths[i].End) + transform.position + Vector3.back);
                    }
                    lineRenderer.startColor = vertex.Color;
                    lineRenderer.endColor = otherVertex.Color;

                    _visualizerObjects.Add(edgeVisualizer);
                }
            }
        }

        public void ResetWaypointsColor()
        {
            foreach (var (_, vertex) in VertexVisualizers)
            {
                vertex.ShowDefaultWaypointColor();
            }
        }

        public void ResetRobotHighlighting(IEnumerable<MonaRobot> robots, MonaRobot? selectedRobot)
        {
            foreach (var robot in robots)
            {
                robot.HideOutline();
            }
            if (selectedRobot != null)
            {
                selectedRobot.ShowOutline();
            }
        }

        public void ShowWaypointHeatMap(int currentTick)
        {
            foreach (var (_, vertexVisualizer) in VertexVisualizers)
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
            VertexVisualizers[targetVertex.Id].SetWaypointColor(yellowColor);
        }

        public void ShowRobotsHighlighting(IEnumerable<MonaRobot> robots)
        {
            foreach (var robot in robots)
            {
                robot.ShowOutline();
            }
        }

        public void ShowDefaultColor(Vertex vertex)
        {
            if (VertexVisualizers.TryGetValue(vertex.Id, out var vertexObject))
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