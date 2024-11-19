using System;
using System.Globalization;
using System.IO;

using Maes.Algorithms;
using Maes.Map.RobotSpawners;
using Maes.Simulation.SimulationScenarios;
using Maes.Statistics;
using Maes.Trackers;
using Maes.UI.SimulationInfoUIControllers;

namespace Maes.Simulation
{
    public sealed class PatrollingSimulation : SimulationBase<PatrollingSimulation, PatrollingVisualizer, PatrollingCell, PatrollingTracker, PatrollingInfoUIController, IPatrollingAlgorithm, PatrollingSimulationScenario, PatrollingRobotSpawner>
    {
        public PatrollingVisualizer patrollingVisualizer = null!;

        // Set after AfterCollisionMapGenerated is called
        public PatrollingTracker PatrollingTracker = null!;

        public override PatrollingVisualizer Visualizer => patrollingVisualizer;

        public override PatrollingTracker Tracker => PatrollingTracker;

        protected override void AfterCollisionMapGenerated(PatrollingSimulationScenario scenario)
        {
            var patrollingMap = scenario.PatrollingMapFactory(_collisionMap);

            PatrollingTracker = new PatrollingTracker(_collisionMap, patrollingVisualizer, scenario, patrollingMap);

            patrollingVisualizer.SetPatrollingMap(patrollingMap);

            RobotSpawner.SetPatrolling(patrollingMap, PatrollingTracker);
        }

        public override bool HasFinishedSim()
        {
            if (_scenario.TotalCycles != PatrollingTracker.CompletedCycles)
            {
                return false;
            }

            if (!PatrollingTracker.StopAfterDiff)
            {
                return true;
            }

            return PatrollingTracker.AverageGraphDiffLastTwoCyclesProportion <= 0.025;
        }

        protected override void CreateStatisticsFile()
        {
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
        }
    }
}