using Maes.Statistics;

using UnityEngine;

namespace Maes.Map.Visualization.Patrolling
{
    public class LineOfSightVertexVisualizationMode : IPatrollingVisualizationMode
    {
        private PatrollingVisualizer Visualizer { get; }
        private int SelectedVertexId { get; }

        public LineOfSightVertexVisualizationMode(PatrollingVisualizer visualizer, int selectedVertexId)
        {
            Visualizer = visualizer;
            SelectedVertexId = selectedVertexId;
            visualizer.SetAllColors(CellIndexToColor);
        }
        public void UpdateVisualization(PatrollingVisualizer visualizer, int currentTick)
        {
            // Nothing to update since the visualization does not change
        }

        private Color32 CellIndexToColor(int cellIndex)
        {
            return Visualizer.LineOfSightVertices.VerticesVisibleTiles[SelectedVertexId].Contains(cellIndex)
                ? PatrollingVisualizer.PatrollingAreaColor
                : PatrollingVisualizer.StandardCellColor;
        }
    }
}