using System.Collections.Generic;

using Maes.Map;
using Maes.Trackers;

using UnityEngine;

namespace Maes.Statistics
{
    public class PatrollingVisualizer : Visualizer<PatrollingCell>
    {

        public GameObject VertexVisualizer = null!;
        public GameObject EdgeVisualizer = null!;

        // Set by SetPatrollingMap
        private PatrollingMap _patrollingMap = null!;

        private readonly List<GameObject> _visualizers = new();

        private readonly Dictionary<int, MeshRenderer> _vertexVisualizers = new();

        public override void SetSimulationMap(SimulationMap<PatrollingCell> simulationMap, Vector3 offset)
        {
            base.SetSimulationMap(simulationMap, Vector3.zero);

            foreach (var visualizer in _visualizers)
            {
                Destroy(visualizer);
            }
        }

        public void SetPatrollingMap(PatrollingMap patrollingMap)
        {
            _patrollingMap = patrollingMap;
            CreateVisualizers();
        }

        private void CreateVisualizers()
        {
            foreach (var vertex in _patrollingMap.Vertices)
            {
                var vertexVisualizer = Instantiate(VertexVisualizer, transform);
                // Add the vertex visualizer to the UI layer
                vertexVisualizer.layer = LayerMask.NameToLayer("UI");
                vertexVisualizer.transform.localPosition = (Vector2)vertex.Position;
                var meshRenderer = vertexVisualizer.GetComponent<MeshRenderer>();
                meshRenderer.material.color = vertex.Color;

                // Link the MonoVertex component for mouse interaction
                var monoVertex = vertexVisualizer.AddComponent<MonoVertex>();
                var collider = vertexVisualizer.AddComponent<BoxCollider2D>();

                if (monoVertex == null)
                {
                    Debug.LogError("MonoVertex component missing on VertexVisualizer prefab");
                }
                else
                {
                    monoVertex.VertexDetails = new VertexDetails(vertex);
                }

                _visualizers.Add(vertexVisualizer);
                _vertexVisualizers.Add(vertex.Id, meshRenderer);

                foreach (var otherVertex in vertex.Neighbors)
                {
                    var edgeVisualizer = Instantiate(EdgeVisualizer, transform);
                    var lineRenderer = edgeVisualizer.GetComponent<LineRenderer>();

                    var paths = _patrollingMap.Paths[(vertex.Id, otherVertex.Id)];
                    lineRenderer.positionCount = paths.Length + 1;
                    lineRenderer.SetPosition(0, ((Vector3)(Vector2)paths[0].Start) + transform.position + Vector3.back);
                    for (var i = 0; i < paths.Length; i++)
                    {
                        lineRenderer.SetPosition(i + 1, ((Vector3)(Vector2)paths[i].End) + transform.position + Vector3.back);
                    }
                    lineRenderer.material.color = vertex.Color;

                    _visualizers.Add(edgeVisualizer);
                }
            }
        }

        public void ResetWaypointsColor()
        {
            foreach (var vertex in _patrollingMap.Vertices)
            {
                _vertexVisualizers[vertex.Id].material.color = vertex.Color;
            }
        }

        public void ShowWaypointHeatMap(int currentTick)
        {
            foreach (var vertex in _patrollingMap.Vertices)
            {
                if (vertex.NumberOfVisits == 0)
                {
                    continue;
                }

                var ticksSinceLastExplored = currentTick - vertex.LastTimeVisitedTick;
                var coldness = Mathf.Min((float)ticksSinceLastExplored / (float)GlobalSettings.TicksBeforeWaypointCoverageHeatMapCold, 1.0f);
                var color = Color32.Lerp(ExplorationVisualizer.WarmColor, ExplorationVisualizer.ColdColor, coldness);
                _vertexVisualizers[vertex.Id].material.color = color;
            }
        }

        public void ShowTargetWaypoint(Vertex targetVertex)
        {
            var yellowColor = new Color(255, 255, 0, 255);
            _vertexVisualizers[targetVertex.Id].material.color = yellowColor;
        }

        public void ShowDefaultColor(Vertex vertex)
        {
            if (_vertexVisualizers.TryGetValue(vertex.Id, out var vertexObject))
            {
                vertexObject.material.color = vertex.Color;
            }
            else
            {
                Debug.LogError($"Vertex {vertex.Position} not found in visualizer");
            }
        }
    }
}