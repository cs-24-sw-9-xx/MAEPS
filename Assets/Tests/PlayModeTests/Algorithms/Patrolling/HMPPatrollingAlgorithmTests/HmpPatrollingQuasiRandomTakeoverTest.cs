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

using System.Collections;
using System.Collections.Generic;

using Maes.Algorithms;
using Maes.Algorithms.Patrolling;
using Maes.Algorithms.Patrolling.HMPPatrollingAlgorithms.ImmediateTakeover;
using Maes.Map;
using Maes.Map.Generators;
using Maes.Map.Generators.Patrolling.Waypoints.Generators;
using Maes.Robot;
using Maes.Simulation.Patrolling;

using NUnit.Framework;

using UnityEngine;

namespace Tests.PlayModeTests.Algorithms.Patrolling.HMPPatrollingAlgorithmTests
{
    public class HmpPatrollingQuasiRandomTakeoverTest
    {
        private PatrollingSimulator _maes;
        private const int Seed = 1;
        private const int MaxSimulatedLogicTicks = 250000;
        private const int MapSize = 50;
        private const int RobotCount = 2;
        private const int TotalCycles = 3;
        private TrackerVertices _trackerVertices;

        [TearDown]
        public void ClearSimulator()
        {
            _maes.Destroy();
        }

        [Test(ExpectedResult = null)]
        public IEnumerator Test_RobotsPatrolOnlyItsPartition()
        {
            _trackerVertices = new TrackerVertices();
            CreateAndEnqueueScenario();

            var simulation = _maes.SimulationManager.CurrentSimulation!;

            _maes.SimulationManager.AttemptSetPlayState(Maes.UI.SimulationPlayState.FastAsPossible);
            while (!_trackerVertices.CheckIsRobotPatrollingOwnPartition() && !simulation.HasFinishedSim() && simulation.SimulatedLogicTicks < MaxSimulatedLogicTicks)
            {
                yield return null;
            }

            Assert.IsTrue(_trackerVertices.IsRobotPatrollingOwnPartition, "Robots are not patrolling their own partition or not visited all its vertices.");
        }

        private void CreateAndEnqueueScenario()
        {

            var robotConstraints = new RobotConstraints(
                mapKnown: true,
                distributeSlam: false,
                slamRayTraceRange: 0f,
                robotCollisions: false,
                calculateSignalTransmissionProbability: (_, distanceThroughWalls) => distanceThroughWalls == 0f);

            var mapConfig = new BuildingMapConfig(123, widthInTiles: MapSize, heightInTiles: MapSize, brokenCollisionMap: false);

            var scenarios = new[] {(
                new PatrollingSimulationScenario(
                    seed: Seed,
                    totalCycles: TotalCycles,
                    stopAfterDiff: false,
                    robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsTogether(
                        collisionMap: buildingConfig,
                        seed: Seed,
                        numberOfRobots: RobotCount,
                        suggestedStartingPoint: null,
                        createAlgorithmDelegate: _ => new WrapperHMPPatrollingAlgorithm(_trackerVertices, PartitionComponent.TakeoverStrategy.QuasiRandomStrategy)),
                    mapSpawner: generator => generator.GenerateMap(mapConfig),
                    robotConstraints: robotConstraints,
                    statisticsFileName: $"test",
                    patrollingMapFactory: map => AllWaypointConnectedGenerator.MakePatrollingMap(map))
            )};

            _maes = new PatrollingSimulator(scenarios);
        }

        private class TrackerVertices
        {
            private readonly Dictionary<int, HashSet<int>> _observedRobotVertices = new();
            private readonly Dictionary<int, HashSet<int>> _generatedRobotVertices = new();

            public void AddRobot(int robotId, HMPPatrollingAlgorithm algorithm)
            {
                _observedRobotVertices[robotId] = new HashSet<int>();
                algorithm.SubscribeOnReachVertex(vertexId =>
                {
                    _observedRobotVertices[robotId].Add(vertexId);
                });
            }

            public void AddGeneratedRobotVertices(HMPPatrollingAlgorithm algorithm)
            {
                var partitionInfo = algorithm.PartitionInfo;
                if (partitionInfo is { VertexIds: { Count: > 0 } })
                {
                    _generatedRobotVertices[partitionInfo.RobotId] = new HashSet<int>(partitionInfo.VertexIds);
                }
            }

            public bool IsRobotPatrollingOwnPartition { get; private set; }

            public bool CheckIsRobotPatrollingOwnPartition()
            {
                if (_generatedRobotVertices.Count == 0)
                {
                    IsRobotPatrollingOwnPartition = false;
                    return IsRobotPatrollingOwnPartition;
                }

                foreach (var (robotId, expected) in _generatedRobotVertices)
                {
                    if (!_observedRobotVertices[robotId].SetEquals(expected))
                    {
                        IsRobotPatrollingOwnPartition = false;
                        return IsRobotPatrollingOwnPartition;
                    }
                }

                IsRobotPatrollingOwnPartition = true;
                return IsRobotPatrollingOwnPartition;
            }
        }

        private class WrapperHMPPatrollingAlgorithm : IPatrollingAlgorithm
        {
            public WrapperHMPPatrollingAlgorithm(TrackerVertices trackerVertices, PartitionComponent.TakeoverStrategy takeoverStrategy)
            {
                _algorithm = new HMPPatrollingAlgorithm(takeoverStrategy);
                _trackerVertices = trackerVertices;
            }

            private readonly HMPPatrollingAlgorithm _algorithm;
            private readonly TrackerVertices _trackerVertices;

            public string AlgorithmName => _algorithm.AlgorithmName;
            public Vertex TargetVertex => _algorithm.TargetVertex;
            public Dictionary<int, Color32[]> ColorsByVertexId => _algorithm.ColorsByVertexId;

            public int LogicTicks => throw new System.NotImplementedException();

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

            public void SubscribeOnTrackInfo(OnTrackInfo onTrackInfo)
            {

            }

            public IEnumerable<WaitForCondition> PreUpdateLogic()
            {
                foreach (var result in _algorithm.PreUpdateLogic())
                {
                    _trackerVertices.AddGeneratedRobotVertices(_algorithm);
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
                _trackerVertices.AddRobot(controller.Id, _algorithm);
            }

            public string GetDebugInfo()
            {
                return _algorithm.GetDebugInfo();
            }

            public bool HasSeenAllInPartition(int assignedPartition)
            {
                throw new System.NotImplementedException();
            }

            public void ResetSeenVerticesForPartition(int partitionId)
            {
                throw new System.NotImplementedException();
            }
        }
    }
}