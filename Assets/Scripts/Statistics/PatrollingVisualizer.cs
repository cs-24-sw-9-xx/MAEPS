
using System.Collections.Generic;
using Maes.Map;
using Maes.Map.MapGen;
using Maes.Visualizer;
using UnityEngine;

namespace Maes.Statistics {
    public class PatrollingVisualizer : MonoBehaviour, IVisualizer<PatrollingCell> {

        public GameObject VertexVisualizer;

        public GameObject EdgeVisualizer;

        private PatrollingMap _patrollingMap;

        private List<GameObject> _visualizers = new List<GameObject>();

        private Dictionary<Vertex, GameObject> _vertexVisualizers = new Dictionary<Vertex, GameObject>();

        public void SetSimulationMap(SimulationMap<PatrollingCell> simulationMap, Vector3 offset)
        {
            // We have to offset this for some reason ¯\_(ツ)_/¯
            transform.position = simulationMap.ScaledOffset;
            foreach (var visualizer in _visualizers) {
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
            foreach (var vertex in _patrollingMap.Verticies)
            {
                var vertexVisualizer = GameObject.Instantiate(VertexVisualizer, transform);
                vertexVisualizer.transform.localPosition = (Vector2)vertex.Position;
                var meshRenderer = vertexVisualizer.GetComponent<MeshRenderer>();
                meshRenderer.material.color = vertex.Color;
                _visualizers.Add(vertexVisualizer);
                _vertexVisualizers.Add(vertex, vertexVisualizer);

                foreach (var otherVertex in vertex.Neighbors) {
                    var edgeVisualizer = GameObject.Instantiate(EdgeVisualizer, transform);
                    var lineRenderer = edgeVisualizer.GetComponent<LineRenderer>();
                    lineRenderer.SetPosition(0, ((Vector3)(Vector2)vertex.Position) + transform.position + Vector3.back);
                    lineRenderer.SetPosition(1, ((Vector3)(Vector2)otherVertex.Position) + transform.position + Vector3.back);
                    lineRenderer.material.color = vertex.Color;

                    _visualizers.Add(edgeVisualizer);
                }
            }
        }

        public void ShowWaypointHeatMap(int currentTick){

            foreach (var vertex in _patrollingMap.Verticies)
            {
                if(vertex.NumberOfVisits == 0) continue;
                var ticksSinceLastExplored = currentTick - vertex.LastTimeVisitedTick;
                float coldness = Mathf.Min((float) ticksSinceLastExplored / (float) GlobalSettings.TicksBeforeWaypointCoverageHeatMapCold, 1.0f);
                var color = Color32.Lerp(ExplorationVisualizer.WarmColor, ExplorationVisualizer.ColdColor, coldness);
                _vertexVisualizers[vertex].GetComponent<MeshRenderer>().material.color = color;
            }
        }
    }
}
