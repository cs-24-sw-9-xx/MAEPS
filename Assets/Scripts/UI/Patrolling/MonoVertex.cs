using Maes.Trackers;
using Maes.UI;

using UnityEngine;

public class MonoVertex : MonoBehaviour
{
    public VertexDetails VertexDetails { get; set; } = null!;

    public void OnMouseEnter()
    {
        Tooltip.ShowTooltip_Static($"Visits: {VertexDetails.Vertex.NumberOfVisits}");
    }
    public void OnMouseExit()
    {
        Tooltip.HideTooltip_Static();
    }
}