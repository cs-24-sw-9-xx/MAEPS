using Maes.Trackers;

using UnityEngine;

namespace Maes.UI.Patrolling
{
    public class VertexVisualizer : MonoBehaviour
    {
        public MeshRenderer meshRenderer = null!;
        public VertexDetails VertexDetails { get; private set; } = null!;

        public delegate void OnVertexSelectedDelegate(VertexVisualizer vertex);

        public OnVertexSelectedDelegate OnVertexSelected = _ => { };

        public void SetVertexDetails(VertexDetails vertexDetails)
        {
            VertexDetails = vertexDetails;
            meshRenderer.material.color = vertexDetails.Vertex.Color;
        }

        public void SetWaypointColor(Color color)
        {
            meshRenderer.material.color = color;
        }

        public void ShowDefaultWaypointColor()
        {
            meshRenderer.material.color = VertexDetails.Vertex.Color;
        }

        public void OnMouseDown()
        {
            OnVertexSelected(this);
        }

        public void OnMouseEnter()
        {
            Tooltip.ShowTooltip_Static($"{VertexDetails.Vertex} Visits: {VertexDetails.Vertex.NumberOfVisits}");
        }
        public void OnMouseExit()
        {
            Tooltip.HideTooltip_Static();
        }
    }
}