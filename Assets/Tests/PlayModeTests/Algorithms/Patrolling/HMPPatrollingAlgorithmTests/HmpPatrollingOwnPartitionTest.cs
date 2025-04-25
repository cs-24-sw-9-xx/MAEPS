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
// Contributors 2025: 
// Henrik van Peet,
// Mads Beyer Mogensen,
// Puvikaran Santhirasegaram

/*using System;
using System.Collections;
using System.Collections.Generic;

using Maes.Algorithms;
using Maes.Algorithms.Patrolling;
using Maes.Map;
using Maes.Map.Generators;
using Maes.Map.Generators.Patrolling.Partitioning;
using Maes.Map.Generators.Patrolling.Waypoints.Generators;
using Maes.Robot;
using Maes.Simulation.Patrolling;

using NUnit.Framework;

using UnityEngine;

namespace Tests.PlayModeTests.Algorithms.Patrolling.HMPPatrollingAlgorithmTests
{
    public class HmpPatrollingOwnPartitionTest
    {
        private PatrollingSimulator _maes;
        private const int Seed = 1;
        private const int MaxSimulatedLogicTicks = 250000;
        private const int MapSize = 100;
        private const int RobotCount = 3;
        private const int TotalCycles = 3;
        private TrackerVertices _trackerVertices;

        [SetUp]
        public void Setup()
        {
            _maes = new PatrollingSimulator();
        }

        [TearDown]
        public void ClearSimulator()
        {
            _maes.Destroy();
        }

        [Test(ExpectedResult = null)]
        public IEnumerator Test_RobotsPatrolOnlyItsPartition()
        {
            CreateAndEnqueueScenario();

            var simulation = _maes.SimulationManager.CurrentSimulation!;
            _trackerVertices = new TrackerVertices(simulation.Robots);

            _maes.SimulationManager.AttemptSetPlayState(Maes.UI.SimulationPlayState.FastAsPossible);
            while (!simulation.HasFinishedSim() && simulation.SimulatedLogicTicks < MaxSimulatedLogicTicks)
            {
                yield return null;
            }

            foreach (var (robotId, expected) in _trackerVertices.GeneratedRobotVertices)
            {
                Assert.True(_trackerVertices.ObservedRobotVertices[robotId].SetEquals(expected), $"Robot {robotId} did not visit all vertices in its partition.");
            }
        }

        private void CreateAndEnqueueScenario()
        {
            var robotConstraints = new RobotConstraints(
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

            var mapConfig = new BuildingMapConfig(123, widthInTiles: MapSize, heightInTiles: MapSize, brokenCollisionMap: false);

            _maes.EnqueueScenario(
                new PatrollingSimulationScenario(
                    seed: Seed,
                    totalCycles: TotalCycles,
                    stopAfterDiff: false,
                    robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsTogether(
                        collisionMap: buildingConfig,
                        seed: Seed,
                        numberOfRobots: RobotCount,
                        suggestedStartingPoint: null,
                        createAlgorithmDelegate: _ => new WrapperHMPPatrollingAlgorithm(new AdapterToPartitionGenerator(SpectralBisectionPartitioningGenerator.Generator), algorithm => _trackerVertices.AddGeneratedRobotVertices(algorithm))),
                    mapSpawner: generator => generator.GenerateMap(mapConfig),
                    robotConstraints: robotConstraints,
                    statisticsFileName: $"test",
                    patrollingMapFactory: AllWaypointConnectedGenerator.MakePatrollingMap)
            );
        }

        private class TrackerVertices
        {
            public TrackerVertices(List<MonaRobot> robots)
            {
                foreach (var robot in robots)
                {
                    ObservedRobotVertices[robot.id] = new HashSet<int>();
                    if (robot.Algorithm is IPatrollingAlgorithm algorithm)
                    {
                        algorithm.SubscribeOnReachVertex(vertexId =>
                        {
                            OnReachVertex(robot.id, vertexId);
                        });
                    }
                    else
                    {
                        Debug.LogError($"Robot {robot.id} does not have a IPatrollingAlgorithm.");
                    }
                }
            }

            public readonly Dictionary<int, HashSet<int>> ObservedRobotVertices = new();
            public readonly Dictionary<int, HashSet<int>> GeneratedRobotVertices = new();

            private void OnReachVertex(int robotId, int vertexId)
            {
                ObservedRobotVertices[robotId].Add(vertexId);
            }

            public void AddGeneratedRobotVertices(HMPPatrollingAlgorithm algorithm)
            {
                var partitionInfo = algorithm.PartitionInfo;
                if (partitionInfo is { VertexIds: { Count: > 0 } })
                {
                    GeneratedRobotVertices[partitionInfo.RobotId] = new HashSet<int>(partitionInfo.VertexIds);
                }
            }
        }

        private class WrapperHMPPatrollingAlgorithm : IPatrollingAlgorithm
        {
            public WrapperHMPPatrollingAlgorithm(IPartitionGenerator partitionGenerator, Action<HMPPatrollingAlgorithm> onLogicUpdate)
            {
                _algorithm = new HMPPatrollingAlgorithm(partitionGenerator);
                _onLogicUpdate = onLogicUpdate;
            }

            private readonly HMPPatrollingAlgorithm _algorithm;
            private readonly Action<HMPPatrollingAlgorithm> _onLogicUpdate;

            public string AlgorithmName => _algorithm.AlgorithmName;
            public Vertex TargetVertex => _algorithm.TargetVertex;
            public Dictionary<int, Color32[]> ColorsByVertexId => _algorithm.ColorsByVertexId;
            public void SetPatrollingMap(PatrollingMap map)
            {
                _algorithm.SetPatrollingMap(map);
            }

            public void SetGlobalPatrollingMap(PatrollingMap globalMap)
            {
                _algorithm.SetGlobalPatrollingMap(globalMap);
            }

            public void SubscribeOnReachVertex(OnReachVertex onReachVertex)
            {
                _algorithm.SubscribeOnReachVertex(onReachVertex);
            }

            public IEnumerable<WaitForCondition> PreUpdateLogic()
            {
                foreach (var result in _algorithm.PreUpdateLogic())
                {
                    _onLogicUpdate(_algorithm);
                    yield return result;
                }
            }

            public IEnumerable<WaitForCondition> UpdateLogic()
            {

                return _algorithm.UpdateLogic();
            }

            public void SetController(Robot2DController controller)
            {
                _algorithm.SetController(controller);
            }

            public string GetDebugInfo()
            {
                return _algorithm.GetDebugInfo();
            }
        }
    }
}*/