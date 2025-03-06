using Maes.Statistics.Trackers;

using UnityEngine;
using UnityEngine.EventSystems;

namespace Maes.UI.Visualizers.Patrolling
{
    public class VertexVisualizer : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
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

        public void OnPointerClick(PointerEventData eventData)
        {
            OnVertexSelected(this);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            Tooltip.ShowTooltip_Static($"{VertexDetails.Vertex} Visits: {VertexDetails.Vertex.NumberOfVisits}");
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Tooltip.HideTooltip_Static();
        }
    }
}