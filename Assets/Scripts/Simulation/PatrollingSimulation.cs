using System;
using System.Globalization;
using System.IO;

using Maes.Algorithms;
using Maes.Map.RobotSpawners;
using Maes.Simulation.SimulationScenarios;
using Maes.Statistics;
using Maes.Statistics.Patrolling;
using Maes.Statistics.Writer;
using Maes.Trackers;
using Maes.UI.Patrolling;
using Maes.UI.SimulationInfoUIControllers;

using UnityEngine;

namespace Maes.Simulation
{
    public sealed class PatrollingSimulation : SimulationBase<PatrollingSimulation, PatrollingVisualizer, PatrollingCell, PatrollingTracker, PatrollingInfoUIController, IPatrollingAlgorithm, PatrollingSimulationScenario, PatrollingRobotSpawner>
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
                return PatrollingTracker.HaveToggledSecondStoppingCriteria && (PatrollingTracker.AverageGraphDiffLastTwoCyclesProportion <= StoppingCriteriaDifference);
            }

            return true;
        }
        private bool _hasWrittenStats;

        public override void OnSimulationFinished()
        {
            if (GlobalSettings.ShouldWriteCsvResults && !_hasWrittenStats)
            {
                CreateStatisticsFile();
                _hasWrittenStats = true;
            }
        }

        protected override void CreateStatisticsFile()
        {
            Debug.Log("Creating statistics file");
            var folderPath =
                $"{GlobalSettings.StatisticsOutPutPath}{_scenario.StatisticsFileName}{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss", CultureInfo.InvariantCulture)}";
            Directory.CreateDirectory(folderPath);

            var patrollingFilename = Path.Join(folderPath, "patrolling");
            new PatrollingCsvDataWriter(this, patrollingFilename).CreateCsvFile();

            foreach (var (point, snapShots) in PatrollingTracker.WaypointSnapShots)
            {
                var waypointFilename = Path.Join(folderPath, $"waypoint_{point.x}_{point.y}");
                new CsvDataWriter<WaypointSnapShot>(snapShots, waypointFilename).CreateCsvFileNoPrepare();
            }
            SaveChart(folderPath);
        }

        private void SaveChart(string folderPath)
        {
            if (Tracker.Chart is null)
            {
                return;
            }

            Debug.Log("Saving chart...");
            var path = Path.Join(folderPath, "chart.png");

            if (!Tracker.Chart.gameObject.activeSelf)
            {
                Tracker.Chart.gameObject.SetActive(true);
            }

            Tracker.Zoom.start = 0;
            Tracker.Zoom.end = 100;
            Tracker.Zoom.enable = false;
            Tracker.Chart.RefreshDataZoom();
            Tracker.Chart.SetAllDirty();
            Tracker.Chart.RefreshAllComponent();
            Tracker.Chart.RefreshChart();
            Tracker.Chart.SaveAsImage("png", path);
        }

        public void SetSelectedVertex(VertexVisualizer? newSelectedVertex)
        {
            SelectedVertex = newSelectedVertex;
            Tracker.SetVisualizedVertex(newSelectedVertex);
        }
    }
}