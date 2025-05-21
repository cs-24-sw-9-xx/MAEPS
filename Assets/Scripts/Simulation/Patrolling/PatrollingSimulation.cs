using Maes.Algorithms.Patrolling;
using Maes.Map.RobotSpawners;
using Maes.Statistics.Trackers;
using Maes.UI.SimulationInfoUIControllers;
using Maes.UI.Visualizers.Patrolling;

namespace Maes.Simulation.Patrolling
{
    public sealed class PatrollingSimulation : SimulationBase<PatrollingSimulation, PatrollingVisualizer, PatrollingTracker, PatrollingInfoUIController, IPatrollingAlgorithm, PatrollingSimulationScenario, PatrollingRobotSpawner>
    {
        private const float StoppingCriteriaDifference = 0.025f;
        public PatrollingVisualizer patrollingVisualizer = null!;

        // Set after AfterCollisionMapGenerated is called
        public PatrollingTracker PatrollingTracker = null!;

        public override PatrollingVisualizer Visualizer => patrollingVisualizer;

        public override PatrollingTracker Tracker => PatrollingTracker;

        public VertexVisualizer? SelectedVertex { get; private set; }

        protected override void AfterCollisionMapGenerated(PatrollingSimulationScenario scenario)
        {
            var patrollingMap = scenario.PatrollingMapFactory(_collisionMap);

            PatrollingTracker = new PatrollingTracker(this, _collisionMap, patrollingVisualizer, scenario, patrollingMap, StatisticsFolderPath);

            foreach (var (_, vertexVisualizer) in patrollingVisualizer.VertexVisualizers)
            {
                vertexVisualizer.OnVertexSelected = SetSelectedVertex;
            }

            RobotSpawner.SetPatrolling(patrollingMap, PatrollingTracker);
        }

        public override bool HasFinishedSim()
        {
            if (_scenario.TotalCycles > PatrollingTracker.CurrentCycle)
            {
                return _scenario.StopAfterDiff && (PatrollingTracker.AverageGraphDiffLastTwoCyclesProportion <= StoppingCriteriaDifference);
            }

            return true;
        }

        public void SetSelectedVertex(VertexVisualizer? newSelectedVertex)
        {
            SelectedVertex = newSelectedVertex;
            Tracker.SetVisualizedVertex(newSelectedVertex);
        }
    }
}