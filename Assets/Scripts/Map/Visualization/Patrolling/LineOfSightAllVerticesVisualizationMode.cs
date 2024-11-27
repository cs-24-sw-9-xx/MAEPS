using Maes.Statistics;

using UnityEngine;

namespace Maes.Map.Visualization.Patrolling
{
    public class LineOfSightAllVerticesVisualizationMode : IPatrollingVisualizationMode
    {
        private PatrollingVisualizer Visualizer { get; }

        public LineOfSightAllVerticesVisualizationMode(PatrollingVisualizer visualizer)
        {
            Visualizer = visualizer;
            visualizer.SetAllColors(CellIndexToColor);
        }

        public void UpdateVisualization(PatrollingVisualizer visualizer, int currentTick)
        {
            // Nothing to update since the visualization does not change
        }

        private Color32 CellIndexToColor(int cellIndex)
        {
            return Visualizer.LineOfSightVertices.AllVerticesVisibleTiles.Contains(cellIndex)
                ? PatrollingVisualizer.PatrollingAreaColor
                : PatrollingVisualizer.StandardCellColor;
        }
    }
}