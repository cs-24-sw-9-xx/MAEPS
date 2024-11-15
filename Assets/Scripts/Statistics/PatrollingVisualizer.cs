using System.Collections.Generic;

using Maes.Map;

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

        private readonly Dictionary<int, GameObject> _vertexVisualizers = new();

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
                vertexVisualizer.transform.localPosition = (Vector2)vertex.Position;
                var meshRenderer = vertexVisualizer.GetComponent<MeshRenderer>();
                meshRenderer.material.color = vertex.Color;
                _visualizers.Add(vertexVisualizer);
                _vertexVisualizers.Add(vertex.Id, vertexVisualizer);

                foreach (var otherVertex in vertex.Neighbors)
                {
                    var edgeVisualizer = Instantiate(EdgeVisualizer, transform);
                    var lineRenderer = edgeVisualizer.GetComponent<LineRenderer>();
                    lineRenderer.SetPosition(0, ((Vector3)(Vector2)vertex.Position) + transform.position + Vector3.back);
                    lineRenderer.SetPosition(1, ((Vector3)(Vector2)otherVertex.Position) + transform.position + Vector3.back);
                    lineRenderer.material.color = vertex.Color;

                    _visualizers.Add(edgeVisualizer);
                }
            }

            //TODO: Add a better way to debug point generation, its is currently outcommented
            // if (_patrollingMap.DebugPoints != null)
            // {
            //     foreach (var corner in _patrollingMap.DebugPoints)
            //     {
            //         var vertexVisualizer = GameObject.Instantiate(VertexVisualizer, transform);
            //         vertexVisualizer.transform.localPosition = ((Vector3)(Vector2)corner.Position) + new Vector3(0, 0, -3);
            //         var meshRenderer = vertexVisualizer.GetComponent<MeshRenderer>();
            //         meshRenderer.material.color = Color.cyan;
            //         _visualizers.Add(vertexVisualizer);
            //         _vertexVisualizers.Add(corner, vertexVisualizer);
            //     }
            // }
        }

        public void ResetWaypointsColor()
        {
            foreach (var vertex in _patrollingMap.Vertices)
            {
                _vertexVisualizers[vertex.Id].GetComponent<MeshRenderer>().material.color = vertex.Color;
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
                _vertexVisualizers[vertex.Id].GetComponent<MeshRenderer>().material.color = color;
            }
        }

        public void ShowTargetWaypoint(Vertex targetVertex)
        {
            var yellowColor = new Color(255, 255, 0, 255);
            _vertexVisualizers[targetVertex.Id].GetComponent<MeshRenderer>().material.color = yellowColor;
        }

        public void ShowDefaultColor(Vertex vertex)
        {
            if (_vertexVisualizers.TryGetValue(vertex.Id, out var vertexObject))
            {
                vertexObject.GetComponent<MeshRenderer>().material.color = vertex.Color;
            }
            else
            {
                Debug.LogError($"Vertex {vertex.Position} not found in visualizer");
            }
        }
    }
}