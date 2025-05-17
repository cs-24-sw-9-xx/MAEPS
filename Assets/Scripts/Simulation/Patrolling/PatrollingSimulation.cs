using System.IO;

using Maes.Algorithms.Patrolling;
using Maes.Map.RobotSpawners;
using Maes.Statistics.Patrolling;
using Maes.Statistics.Trackers;
using Maes.Statistics.Writer;
using Maes.UI.SimulationInfoUIControllers;
using Maes.UI.Visualizers.Patrolling;

using UnityEngine;

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

            PatrollingTracker = new PatrollingTracker(this, _collisionMap, patrollingVisualizer, scenario, patrollingMap);

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

        public bool HasSelectedVertex()
        {
            return SelectedVertex != null;
        }

        protected override void CreateStatisticsFile()
        {
            Debug.Log("Creating statistics file");
            Directory.CreateDirectory(StatisticsFolderPath);

            var patrollingFilename = Path.Join(StatisticsFolderPath, "patrolling");
            new PatrollingCsvDataWriter(this, patrollingFilename).CreateCsvFile();

            var waypointFolderPath = Path.Join(StatisticsFolderPath, "waypoints/");
            Directory.CreateDirectory(waypointFolderPath);
            foreach (var (point, snapShots) in PatrollingTracker.WaypointSnapShots)
            {
                var waypointFilename = Path.Join(waypointFolderPath, $"waypoint_{point.x}_{point.y}");
                new CsvDataWriter<WaypointSnapShot>(snapShots, waypointFilename).CreateCsvFileNoPrepare();
            }
        }

        public void SetSelectedVertex(VertexVisualizer? newSelectedVertex)
        {
            SelectedVertex = newSelectedVertex;
            Tracker.SetVisualizedVertex(newSelectedVertex);
        }

    }
}