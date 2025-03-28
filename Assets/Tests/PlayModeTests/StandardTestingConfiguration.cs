// Copyright 2024 MAES
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
// Contributors: Rasmus Borrisholt Schmidt, Andreas Sebastian Sørensen, Thor Beregaard, Malte Z. Andreasen, Philip I. Holler and Magnus K. Jensen,
// 
// Original repository: https://github.com/Molitany/MAES

using Maes.Map.Generators;
using Maes.Robot;
using Maes.Simulation;

namespace Tests.PlayModeTests
{
    public static class StandardTestingConfiguration
    {
        public static MapFactory EmptyCaveMapSpawner(int randomSeed)
        {
            var mapConfiguration = new CaveMapConfig(randomSeed: randomSeed,
                widthInTiles: 50,
                heightInTiles: 50,
                smoothingRuns: 4,
                connectionPassagesWidth: 4,
                randomFillPercent: 0,
                wallThresholdSize: 10,
                roomThresholdSize: 10,
                borderSize: 1);
            return (generator => generator.GenerateMap(mapConfiguration));
        }

        public static RobotConstraints GlobalRobotConstraints()
        {
            return new RobotConstraints(
                senseNearbyAgentsRange: 5f,
                senseNearbyAgentsBlockedByWalls: true,
                automaticallyUpdateSlam: true,
                slamUpdateIntervalInTicks: 1,
                slamSynchronizeIntervalInTicks: 10,
                slamPositionInaccuracy: 0.2f,
                mapKnown: true,
                distributeSlam: false,
                environmentTagReadRange: 100f,
                slamRayTraceRange: 7f,
                relativeMoveSpeed: 1f,
                agentRelativeSize: 0.6f,
                calculateSignalTransmissionProbability: (_, _) => true);
        }

    }
}