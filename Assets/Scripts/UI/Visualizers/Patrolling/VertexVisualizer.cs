using System.Linq;

using Maes.Statistics.Trackers;
using Maes.Utilities;

using UnityEngine;
using UnityEngine.EventSystems;

namespace Maes.UI.Visualizers.Patrolling
{
    [RequireComponent(typeof(MeshRenderer))]
    public class VertexVisualizer : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public MeshFilter meshFilter = null!;
        public VertexDetails VertexDetails { get; private set; } = null!;

        public delegate void OnVertexSelectedDelegate(VertexVisualizer vertex);

        public OnVertexSelectedDelegate OnVertexSelected = _ => { };

        public void SetVertexDetails(VertexDetails vertexDetails)
        {
            VertexDetails = vertexDetails;
            SetWaypointColor(vertexDetails.Vertex.Color);
        }

        public void SetWaypointColor(Color32 color)
        {
            meshFilter.mesh = VertexColorMeshVisualizer.GenerateMeshSingleColor(color);
        }

        public void ShowDefaultWaypointColor()
        {
            SetWaypointColor(VertexDetails.Vertex.Color);
        }

        public void SetWaypointColor(Color32[] colors)
        {
            if (colors.Length == 1)
            {
                SetWaypointColor(colors[0]);
            }

            meshFilter.mesh = VertexColorMeshVisualizer.GenerateMeshMultipleColor(colors.Distinct().ToArray());
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