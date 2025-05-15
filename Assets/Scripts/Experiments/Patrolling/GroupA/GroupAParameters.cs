// Copyright 2025 MAEPS
// 
// This file is part of MAEPS
// 
// MAEPS is free software: you can redistribute it and/or modify it under
// the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your option)
// any later version.
// 
// MAEPS is distributed in the hope that it will be useful, but WITHOUT
// ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
// or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General
// Public License for more details.
// 
// You should have received a copy of the GNU General Public License along
// with MAEPS. If not, see http://www.gnu.org/licenses/.
// 
// Contributors: 
// Henrik van Peet,
// Mads Beyer Mogensen,
// Puvikaran Santhirasegaram

using System.Collections.Generic;

using Maes.Robot;

namespace Maes.Experiments.Patrolling
{
    internal static class GroupAParameters
    {
        public const int AmountOfCycles = 100; // Should be changed to 1000 for the final experiment?
        public const int StandardMapSize = 200;
        public const int StandardRobotCount = 8;

        public static readonly string StandardRobotConstraintName = "Standard";

        /// <summary>
        /// LOS communication.
        /// </summary>
        public static readonly RobotConstraints StandardRobotConstraints = CreateRobotConstraints();

        /// <summary>
        /// Creates the robot constraints for the patrolling algorithms.
        /// The default values use LOS communication.
        /// </summary>
        public static RobotConstraints CreateRobotConstraints(float senseNearbyAgentsRange = 5f, bool senseNearbyAgentsBlockedByWalls = true, float communicationDistanceThroughWalls = 0f)
        {
            return new RobotConstraints(
                senseNearbyAgentsRange: senseNearbyAgentsRange,
                senseNearbyAgentsBlockedByWalls: senseNearbyAgentsBlockedByWalls,
                slamUpdateIntervalInTicks: 1,
                mapKnown: true,
                distributeSlam: false,
                environmentTagReadRange: 100f,
                slamRayTraceRange: 0f,
                calculateSignalTransmissionProbability: (_, distanceThroughWalls) => distanceThroughWalls <= communicationDistanceThroughWalls,
                materialCommunication: true);
        }

        public static IEnumerable<int> SeedGenerator(int seedCount)
        {
            var seeds = new List<int>();
            for (var i = 0; i < seedCount; i++)
            {
                seeds.Add(i);
            }
            return seeds;
        }
    }
}