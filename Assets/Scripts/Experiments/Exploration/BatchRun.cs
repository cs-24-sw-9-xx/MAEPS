// Copyright 2025 MAES
// 
// This file is part of MAES
// 
// MAES is free software: you can redistribute it and/or modify it under
// the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your option)
// any later version.
// 
// MAES is distributed in the hope that it will be useful, but WITHOUT
// ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
// or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General
// Public License for more details.
// 
// You should have received a copy of the GNU General Public License along
// with MAES. If not, see http://www.gnu.org/licenses/.
// 
// Contributors: Michele Albano
// 
// Original repository: https://github.com/DEIS-Tools/MAES/

using Maes.Algorithms.Exploration;
using Maes.Algorithms.Exploration.Greed;
using Maes.Algorithms.Exploration.Minotaur;
using Maes.Algorithms.Exploration.GreedG;
using Maes.Map.Generators;
using Maes.Map.RobotSpawners;
using Maes.Robot;
using Maes.Simulation.Exploration;
using Maes.Algorithms.Exploration.TheNextFrontier;
using Maes.Algorithms.Exploration.RandomBallisticWalk;
using Maes.Algorithms.Exploration.SSB;
using Maes.Algorithms.Exploration.Voronoi;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Scripting;
using System.Linq;
using Maes.Utilities;
using Maes.UI;

namespace Maes.Experiments.Exploration
{
    using MySimulationScenario = ExplorationSimulationScenario;
    using MySimulator = ExplorationSimulator;
    using RobotSpawner = RobotSpawner<IExplorationAlgorithm>;
    [Preserve]
    internal class BatchRun : MonoBehaviour
    {
        private readonly bool _silent = false;
        private void ZeroLogString()
        {
            if (!_silent)
            {
                string docPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
                using (var outputFile = new StreamWriter(Path.Combine(docPath, "WriteLines.txt")))
                {
                    outputFile.WriteLine("" + docPath);
                }
            }
        }
        private void WeAreDone()
        {
            Application.Quit(0);
        }

        private void LogString(string message)
        {
            if (!_silent)
            {
                string docPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
                using (StreamWriter outputFile = new StreamWriter(Path.Combine(docPath, "WriteLines.txt"), true))
                {
                    outputFile.WriteLine(message);
                }
            }
        }
        private Dictionary<string, RobotConstraints> GetRobotConstraints()
        {
            var constraintsDict = new Dictionary<string, RobotConstraints>
            {
                ["Global"] = new RobotConstraints(
                senseNearbyAgentsRange: 5f,
                senseNearbyAgentsBlockedByWalls: true,
                automaticallyUpdateSlam: true,
                slamUpdateIntervalInTicks: 1,
                slamSynchronizeIntervalInTicks: 10,
                slamPositionInaccuracy: 0.2f,
                distributeSlam: false,
                environmentTagReadRange: 4.0f,
                slamRayTraceRange: 7f,
                relativeMoveSpeed: 1f,
                agentRelativeSize: 0.6f,
                calculateSignalTransmissionProbability: (_, _) => true),

                ["Material"] = new RobotConstraints(
                senseNearbyAgentsRange: 5f,
                senseNearbyAgentsBlockedByWalls: true,
                automaticallyUpdateSlam: true,
                slamUpdateIntervalInTicks: 1,
                slamSynchronizeIntervalInTicks: 10,
                slamPositionInaccuracy: 0.2f,
                distributeSlam: false,
                environmentTagReadRange: 4.0f,
                slamRayTraceRange: 7f,
                relativeMoveSpeed: 1f,
                agentRelativeSize: 0.6f,
                materialCommunication: true
            ),

                ["LOS"] = new RobotConstraints(
                senseNearbyAgentsRange: 5f,
                senseNearbyAgentsBlockedByWalls: true,
                automaticallyUpdateSlam: true,
                slamUpdateIntervalInTicks: 1,
                slamSynchronizeIntervalInTicks: 10,
                slamPositionInaccuracy: 0.2f,
                distributeSlam: false,
                environmentTagReadRange: 4.0f,
                slamRayTraceRange: 7f,
                relativeMoveSpeed: 1f,
                agentRelativeSize: 0.6f,
                calculateSignalTransmissionProbability: (_, distanceThroughWalls) => distanceThroughWalls <= 0),

                ["None"] = new RobotConstraints(
                senseNearbyAgentsRange: 5f,
                senseNearbyAgentsBlockedByWalls: true,
                automaticallyUpdateSlam: true,
                slamUpdateIntervalInTicks: 1,
                slamSynchronizeIntervalInTicks: 10,
                slamPositionInaccuracy: 0.2f,
                distributeSlam: false,
                environmentTagReadRange: 4.0f,
                slamRayTraceRange: 7f,
                relativeMoveSpeed: 1f,
                agentRelativeSize: 0.6f,
                calculateSignalTransmissionProbability: (_, _) => false)
            };

            return constraintsDict;
        }

        private Dictionary<string, RobotSpawner.CreateAlgorithmDelegate> GetAlgorithms(RobotConstraints constraints)
        {
            var algorithms = new Dictionary<string, RobotSpawner.CreateAlgorithmDelegate>
                {
                    // BrickAndMortar commentato
                    { "tnf", seed => new TnfExplorationAlgorithm(1, 10, seed) },
                    { "fftnf", seed => new FfTnfExplorationAlgorithm(1, 10, seed) },
                    { "minotaur", seed => new MinotaurAlgorithm(constraints, 2) },
                    { "greed", seed => new GreedAlgorithm() },
                    { "ballistic", seed => new RandomExplorationAlgorithm(seed) },
                    { "spiral", seed => new SsbAlgorithm() },
                    { "voronoi", seed => new VoronoiExplorationAlgorithm(seed, 1) },
                    { "greedG_simple", seed => new GreedGAlgorithm(0) },
                    { "greedG_util", seed => new GreedGAlgorithm(1) },
                    { "greedG_EuclidSimple", seed => new GreedGAlgorithm(2) },
                    { "greedG_EuclidPath", seed => new GreedGAlgorithm(3) },
                    { "greedG_ManhattanSimple", seed => new GreedGAlgorithm(4) },
                    { "greedG_ManhattanPath", seed => new GreedGAlgorithm(5) }
            };
            return algorithms;
        }

        private class ExperimentConfig
        {
            public string communication = "Material";
            public int seed = 123456;
            public int mapsNumber = 100;
            public string algorithm = "greed";
            public int robotsNumber = 2;
            public string mapType = "cave";
            public int mapSize = 50;
        };

        private void PrintUsage()
        {
            LogString(@"
Parameters:
    --communication [Global,Material,LOS,None]
    --seed INT
    --mapsNumber INT
    --algorithm [tnf,fftnf,minotaur,greed,ballistic,spiral,voronoi]
    --robotsNumber INT
    --mapType [cave,building]
    --mapSize INT
            ");
            WeAreDone();
        }

        private ExperimentConfig ParseCmdLine()
        {
            var config = new ExperimentConfig();
            string[] args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                //LogString(i + " - " + args[i]);
                if (args[i] == "--communication")
                {
                    config.communication = args[i + 1];
                    if (
                        (args[i + 1] != "Global") &&
                        (args[i + 1] != "Material") &&
                        (args[i + 1] != "LOS") &&
                        (args[i + 1] != "None")
                    )
                    {
                        PrintUsage();
                    }
                    i++;
                    continue;
                }
                if (args[i] == "--mapType")
                {
                    config.mapType = args[i + 1];
                    if (
                        (args[i + 1] != "cave") &&
                        (args[i + 1] != "building")
                    )
                    {
                        PrintUsage();
                    }
                    i++;
                    continue;
                }
                if (args[i] == "--mapSize")
                {
                    try
                    {
                        config.mapSize = System.Int32.Parse(args[i + 1]);
                    }
                    catch (System.FormatException)
                    {
                        PrintUsage();
                    }
                    i++;
                    continue;
                }
                if (args[i] == "--seed")
                {
                    try
                    {
                        config.seed = System.Int32.Parse(args[i + 1]);
                    }
                    catch (System.FormatException)
                    {
                        PrintUsage();
                    }
                    i++;
                    continue;
                }
                if (args[i] == "--robotsNumber")
                {
                    try
                    {
                        config.robotsNumber = System.Int32.Parse(args[i + 1]);
                    }
                    catch (System.FormatException)
                    {
                        PrintUsage();
                    }
                    i++;
                    continue;
                }
                if (args[i] == "--mapsNumber")
                {
                    try
                    {
                        config.mapsNumber = System.Int32.Parse(args[i + 1]);
                    }
                    catch (System.FormatException)
                    {
                        PrintUsage();
                    }
                    i++;
                    continue;
                }
                if (args[i] == "--algorithm")
                {
                    config.algorithm = args[i + 1];
                    if (
                        //(args[i] != "bricknmortar") &&
                        (args[i + 1] != "tnf") &&
                        (args[i + 1] != "fftnf") &&
                        (args[i + 1] != "minotaur") &&
                        (args[i + 1] != "greed") &&
                        (args[i + 1] != "ballistic") &&
                        (args[i + 1] != "spiral") &&
                        (args[i + 1] != "voronoi") &&
                        (args[i + 1] != "greedG_EuclidPath") &&
                        (args[i + 1] != "greedG_util") &&
                        (args[i + 1] != "greedG_simple")
                    )
                    {
                        PrintUsage();
                    }
                    i++;
                    continue;
                }
            }

            //LogString("Experiments Configuration:\n"+ Newtonsoft.Json.JsonConvert.SerializeObject(config));

            return config;
        }

        private ExperimentConfig ParseCmdLine2()
        {
            var config = new ExperimentConfig();
            config.seed = 123456;
            config.mapsNumber = 1;
            config.communication = "Material";
            config.mapType = "building";
            config.mapSize = 50;

            config.robotsNumber = 4;
            config.algorithm = "minotaur";
            return config;
        }
        private IMapConfig CreateMapConfig(int seed, string mapType, int mapSize)
        {
            if (mapType == "building")
            {
                return new BuildingMapConfig(seed, widthInTiles: mapSize, heightInTiles: mapSize);
            }
            else
            {
                return new CaveMapConfig(seed, widthInTiles: mapSize, heightInTiles: mapSize);
            }
        }


        private MySimulationScenario CreateSimulationScenario(
            ExperimentConfig config,
            int random_map_seed,
            int random_algo_seed,
            int size,
            bool spawnTogether,
            string statisticsFileName,
            int maxTime = 450)
        {
            var random_map = new System.Random(random_map_seed);
            var random_algo = new System.Random(random_algo_seed);
            var mapConfig = CreateMapConfig(random_map.Next(0, 1000000), config.mapType, size);
            var simulationScenarioSeed = random_map.Next(0, 1000000);
            RobotConstraints robotConstraints = GetRobotConstraints()[config.communication];
            string algorithmName = config.algorithm;
            RobotSpawner.CreateAlgorithmDelegate algorithm = GetAlgorithms(robotConstraints)[config.algorithm];


            var spawningPosList = new List<Vector2Int>();
            while (spawningPosList.Count < config.robotsNumber)
            {
                var newPosition = new Vector2Int(
                    random_map.Next(-size / 2, size / 2),
                    random_map.Next(-size / 2, size / 2));
                if (!spawningPosList.Contains(newPosition))
                {
                    spawningPosList.Add(newPosition);
                }
            }
            if (!spawnTogether)
            {
                return new MySimulationScenario(seed: simulationScenarioSeed, // this is for the map
                    mapSpawner: generator => generator.GenerateMap(mapConfig),
                    robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsAtPositions(
                        collisionMap: buildingConfig,
                        seed: random_algo.Next(0, 1000000), // seed for the algorithm of the robot
                        numberOfRobots: config.robotsNumber,
                        spawnPositions: spawningPosList,
                        createAlgorithmDelegate: algorithm),
                    statisticsFileName: statisticsFileName,
                    hasFinishedSim: MySimulationScenario.InfallibleToFallibleSimulationEndCriteria(
                        simulation => (simulation.SimulateTimeSeconds >= maxTime ||
                                 simulation.ExplorationTracker.ExploredProportion > 0.98f)),
                    robotConstraints: robotConstraints);
            }
            else
            {
                return new MySimulationScenario(seed: simulationScenarioSeed, // seed for the map
                    mapSpawner: generator => generator.GenerateMap(mapConfig),
                    robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsTogether(
                        collisionMap: buildingConfig,
                        seed: random_algo.Next(0, 1000000), // seed for the algorithm of the robot
                        numberOfRobots: config.robotsNumber,
                        suggestedStartingPoint: spawningPosList[0],
                        createAlgorithmDelegate: algorithm),
                    statisticsFileName: statisticsFileName,
                    robotConstraints: robotConstraints);
            }
        }
        private void StartOld()
        {
            string[] args = System.Environment.GetCommandLineArgs();


            ZeroLogString();
            LogString("GetCommandLineArgs " + string.Join(", ", args));
            ExperimentConfig config = ParseCmdLine();
            //ExperimentConfig config = ParseCmdLine2();
            LogString("Experiments Configuration:\n" + Newtonsoft.Json.JsonConvert.SerializeObject(config));

            System.Random random_master = new System.Random(config.seed);
            List<int> rand_seeds = new List<int>();
            for (int i = 0; i < 2 * config.mapsNumber; i++)
                rand_seeds.Add(random_master.Next(0, 1000000));



            // I don't reuse the random_master again.
            // For each map, I will have two Randoms, one for map + robot placement, and one for the algorithm execution
            //            var simulator = MySimulator.GetInstance();
            var scenarios = new List<MySimulationScenario>();
            for (int size = 50; size <= 150; size += 50)
            {
                for (int map_number = 0; map_number < config.mapsNumber; map_number++)
                {
                    //                var size = config.mapSize;
                    var statisticsFileName = $"{config.algorithm}-{size}-{config.mapType}-{config.communication}-{config.robotsNumber}-SpawnTogether-seedMap-{rand_seeds[2 * map_number]}-seedAlgo-{rand_seeds[2 * map_number + 1]}";
                    //LogString(Path.GetFullPath("./" + GlobalSettings.StatisticsOutPutPath));
                    LogString(DateTime.Now.ToString("h:mm:ss tt") + ": experiment " + map_number + ": " + statisticsFileName);
                    if (0 >= Directory.GetFiles(Path.GetFullPath("./" + GlobalSettings.StatisticsOutPutPath+"/"+statisticsFileName), statisticsFileName + "*.csv").Length)
                    {
                        scenarios.Add(CreateSimulationScenario(
                            config,
                            rand_seeds[2 * map_number],
                            rand_seeds[2 * map_number + 1],
                            size,
                            true, // spawnTogether
                            statisticsFileName));
                    }
                    else
                    {
                        LogString("EXISTS: file " + statisticsFileName);
                    }

                    statisticsFileName = $"{config.algorithm}-{size}-{config.mapType}-{config.communication}-{config.robotsNumber}-SpawnApart-seedMap-{rand_seeds[2 * map_number]}-seedAlgo-{rand_seeds[2 * map_number + 1]}";
                    LogString(DateTime.Now.ToString("h:mm:ss tt") + ": experiment " + map_number + ": " + statisticsFileName);
                    if (0 == Directory.GetFiles(Path.GetFullPath("./" + GlobalSettings.StatisticsOutPutPath+"/"+statisticsFileName), statisticsFileName + "*.csv").Length)
                    {
                        scenarios.Add(CreateSimulationScenario(
                            config,
                            rand_seeds[2 * map_number],
                            rand_seeds[2 * map_number + 1],
                            size,
                            false, // spawnTogether
                            statisticsFileName));
                    }
                    else
                    {
                        LogString("EXISTS: file " + statisticsFileName);
                    }
                }
            }
            //Just code to make sure the last experiment does not get lost
            var dumpMap = new BuildingMapConfig(-1, widthInTiles: 50, heightInTiles: 50);

            scenarios.Add(new MySimulationScenario(seed: 123,
                mapSpawner: generator => generator.GenerateMap(dumpMap),
                robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsTogether(
                                                                 buildingConfig,
                                                                 seed: 123,
                                                                 numberOfRobots: 1,
                                                                 suggestedStartingPoint: Vector2Int.zero,
                                                                 createAlgorithmDelegate: _ => new MinotaurAlgorithm(GetRobotConstraints()[config.communication], 2)),
                statisticsFileName: $"delete-me",
                robotConstraints: GetRobotConstraints()[config.communication]));
            var simulator = new MySimulator(scenarios);
            simulator.PressPlayButton(); // Instantly enter play mode

            //simulator.GetSimulationManager().AttemptSetPlayState(SimulationPlayState.FastAsPossible);
        }


        private void Start()
        {
            ExperimentConfig config = ParseCmdLine2();
            config.seed = 123456; // this is fixed
            config.mapsNumber = 100; // this is fixed


            //config.algorithm = "greedG_EuclidPath";
//            string[] algos = { "greedG_EuclidPath", "greedG_util", "minotaur", "tnf", "greed" };
            string[] algos = { "tnf" };
            config.communication = "Material"; // this is fixed
            config.mapType = "building";
            //config.mapType = "cave";
            //config.mapSize = 150;
            //int[] mapSizes = { 50, 150 };
            int[] mapSizes = { 150 };
            //config.robotsNumber = 16;
            //int[] robotsNumbers = { 2, 16 };
            int[] robotsNumbers = { 16 };
            var together = false; // this is fixed

            var random_master = new System.Random(config.seed);
            var rand_seeds = new List<int>();
            for (var i = 0; i < 2 * config.mapsNumber; i++) { rand_seeds.Add(random_master.Next(0, 1000000)); }

            // I don't reuse the random_master again.
            // For each map, I will have two Randoms, one for map + robot placement, and one for the algorithm execution
            //            var simulator = MySimulator.GetInstance();
            var scenarios = new List<MySimulationScenario>();
            for (var map_number = 0; map_number < config.mapsNumber; map_number++)
            {
                foreach (var rbtsNmbr in robotsNumbers)
                {
                    config.robotsNumber = rbtsNmbr;
                    foreach (var mpsz in mapSizes)
                    {
                        config.mapSize = mpsz;
                        foreach (var algorithm in algos)
                        {
                            config.algorithm = algorithm; // I will not use algorithm anymore
                            var together_string = together ? "SpawnTogether" : "SpawnApart";
                            var statisticsFileName = $"{config.algorithm}-{config.mapSize}-{config.mapType}-{config.communication}-{config.robotsNumber}-{together_string}-seedMap-{rand_seeds[2 * map_number]}-seedAlgo-{rand_seeds[2 * map_number + 1]}";

                            LogString(DateTime.Now.ToString("h:mm:ss tt") + ": experiment " + map_number + ": " + statisticsFileName);
                            if (0 >= Directory.GetFiles(Path.GetFullPath("./" + GlobalSettings.StatisticsOutPutPath+"/"+statisticsFileName), statisticsFileName + "*.csv").Length)
                            {
                                scenarios.Add(CreateSimulationScenario(
                                    config,
                                    rand_seeds[2 * map_number],
                                    rand_seeds[2 * map_number + 1],
                                    config.mapSize,
                                    together,
                                    statisticsFileName,
                                    2520)); // number of seconds
                            }
                            else
                            {
                                LogString("EXISTS: file " + statisticsFileName);
                            }
                        }
                    }
                }
            }
                //Just code to make sure the last experiment does not get lost
                var dumpMap = new BuildingMapConfig(-1, widthInTiles: 50, heightInTiles: 50);

                scenarios.Add(new MySimulationScenario(seed: 123,
                    mapSpawner: generator => generator.GenerateMap(dumpMap),
                    robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsTogether(
                                                                     buildingConfig,
                                                                     seed: 123,
                                                                     numberOfRobots: 1,
                                                                     suggestedStartingPoint: Vector2Int.zero,
                                                                     createAlgorithmDelegate: _ => new MinotaurAlgorithm(GetRobotConstraints()[config.communication], 2)),
                    hasFinishedSim: MySimulationScenario.InfallibleToFallibleSimulationEndCriteria(
                        simulation => (simulation.SimulateTimeSeconds >= 450 || simulation.ExplorationTracker.CoverageProportion > 0.98f)),
                    statisticsFileName: $"delete-me",
                    robotConstraints: GetRobotConstraints()[config.communication]));
                var simulator = new MySimulator(scenarios);



            simulator.PressPlayButton(); // Instantly enter play mode
                simulator.SimulationManager.AttemptSetPlayState(SimulationPlayState.FastAsPossible);
        }



    }
}
