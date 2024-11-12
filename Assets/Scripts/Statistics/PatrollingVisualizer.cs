
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

        private readonly Dictionary<Vertex, GameObject> _vertexVisualizers = new();

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
                var vertexVisualizer = GameObject.Instantiate(VertexVisualizer, transform);
                vertexVisualizer.transform.localPosition = (Vector2)vertex.Position;
                var meshRenderer = vertexVisualizer.GetComponent<MeshRenderer>();
                meshRenderer.material.color = vertex.Color;
                _visualizers.Add(vertexVisualizer);
                _vertexVisualizers.Add(vertex, vertexVisualizer);

                foreach (var otherVertex in vertex.Neighbors)
                {
                    var edgeVisualizer = GameObject.Instantiate(EdgeVisualizer, transform);
                    var lineRenderer = edgeVisualizer.GetComponent<LineRenderer>();
                    lineRenderer.SetPosition(0, ((Vector3)(Vector2)vertex.Position) + transform.position + Vector3.back);
                    lineRenderer.SetPosition(1, ((Vector3)(Vector2)otherVertex.Position) + transform.position + Vector3.back);
                    lineRenderer.material.color = vertex.Color;

                    _visualizers.Add(edgeVisualizer);
                }
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
                _vertexVisualizers[vertex].GetComponent<MeshRenderer>().material.color = color;
            }
        }
    }
}