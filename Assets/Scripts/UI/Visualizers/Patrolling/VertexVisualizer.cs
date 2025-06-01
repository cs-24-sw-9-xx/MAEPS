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
            ShowDefaultWaypointColor();
        }

        public void ShowDefaultWaypointColor()
        {
            SetWaypointColor(VertexDetails.Vertex.Color);
        }

        public void SetWaypointColor(Color32 color)
        {
            if (meshFilter.sharedMesh != null)
            {
                Destroy(meshFilter.sharedMesh);
            }

            meshFilter.sharedMesh = VertexColorMeshVisualizer.GenerateMeshSingleColor(color);
        }

        public void SetWaypointColor(Color32[] colors)
        {
            if (colors.Length == 1)
            {
                SetWaypointColor(colors[0]);
                return;
            }

            if (meshFilter.sharedMesh != null)
            {
                Destroy(meshFilter.sharedMesh);
            }

            meshFilter.sharedMesh = VertexColorMeshVisualizer.GenerateMeshMultipleColor(colors.Distinct().ToArray());
        }

        private void OnDestroy()
        {
            Destroy(meshFilter.sharedMesh);
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