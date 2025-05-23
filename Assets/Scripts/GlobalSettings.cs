// Copyright 2022 MAES
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
// Contributors: Malte Z. Andreasen, Philip I. Holler and Magnus K. Jensen
// 
// Original repository: https://github.com/MalteZA/MAES

using System.IO;

using UnityEngine;

namespace Maes
{

    // This class contains all settings related to an instance of an simulation
    public static class GlobalSettings
    {
        // Times per second that robot logic is updated
        public static readonly int LogicTickDeltaMillis = 100;

        // Amount of physics steps to calculate between each robot logic tick
        // Physics tick rate = LogicTickDelta / PhysicsTicksPerLogicUpdate
        public const int PhysicsTicksPerLogicUpdate = 10;

        // Debug visualizer
        public static readonly bool DrawCommunication;

        // Statistics
        public static readonly bool ShouldWriteCsvResults = true;
        public static readonly string StatisticsOutPutPath = "data/";
        public static readonly int TicksPerStatsSnapShot = 1;
        public static readonly bool PopulateAdjacencyAndComGroupsEveryTick;


        // The below constants depend on the above constants. Do not change this individually!
        public static readonly int PhysicsTickDeltaMillis = LogicTickDeltaMillis / PhysicsTicksPerLogicUpdate;
        public static readonly float PhysicsTickDeltaSeconds = PhysicsTickDeltaMillis / 1000f;

        public static readonly int TicksBeforeExplorationHeatMapCold = 10 * 60 * 4;
        public static readonly int TicksBeforeCoverageHeatMapCold = 10 * 60 * 4;
        public static readonly int TicksBeforeWaypointCoverageHeatMapCold = 10 * 60 * 6;

        public static bool IsRosMode = false;

        static GlobalSettings()
        {
            // Maes only loads config from yaml file when in Ros Mode
            if (!IsRosMode)
            {
                return;
            }

            var config = MaesYamlConfigLoader.LoadConfig();

            if (config == null)
            {
                Debug.LogError("Could not load yaml config");
                return;
            }

            // Populating static GlobalSettings class.
            LogicTickDeltaMillis = config.GlobalSettings.LogicTicksDeltaMillis;
            PhysicsTickDeltaMillis = config.GlobalSettings.PhysicsTicksPerLogicUpdate;
            DrawCommunication = config.GlobalSettings.DrawCommunication;
            ShouldWriteCsvResults = config.GlobalSettings.ShouldWriteCsvResults;
            if (config.GlobalSettings.StatisticsResultPath.Length == 0)
            {
                // Puts results file in same dir as the executable is run from
                // If run from editor put the path will be path to project folder
                StatisticsOutPutPath = Directory.GetParent(Application.dataPath)?.ToString() + Path.DirectorySeparatorChar;
                Debug.Log($"{nameof(config.GlobalSettings.StatisticsResultPath)} was empty. Defaulting to executable dir (or project root if run in unity editor)");

            }
            TicksPerStatsSnapShot = config.GlobalSettings.TicksPerStatsSnapshot;
            PopulateAdjacencyAndComGroupsEveryTick = config.GlobalSettings.PopulateAdjacencyAndCommGroupsEveryTick;
            TicksBeforeExplorationHeatMapCold = config.GlobalSettings.TicksBeforeExplorationHeatmapCold;
            TicksBeforeCoverageHeatMapCold = config.GlobalSettings.TicksBeforeCoverageHeatmapCold;
        }
    }
}