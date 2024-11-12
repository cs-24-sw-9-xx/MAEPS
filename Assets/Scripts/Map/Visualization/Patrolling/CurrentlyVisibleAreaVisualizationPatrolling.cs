using Maes.Map.Visualization.Common;
using Maes.Robot;
using Maes.Statistics;

namespace Maes.Map.Visualization.Patrolling
{
    internal class CurrentlyVisibleAreaVisualizationPatrollingMode : CurrentlyVisibleAreaVisualization<PatrollingCell, PatrollingVisualizer>, IPatrollingVisualizationMode
    {

        public CurrentlyVisibleAreaVisualizationPatrollingMode(SimulationMap<PatrollingCell> map, Robot2DController selectedRobot) : base(map, selectedRobot) { }
    }
}