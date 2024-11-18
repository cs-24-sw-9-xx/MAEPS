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

using System;
using System.Collections;

using Maes.Robot;
using Maes.Robot.Task;
using Maes.Simulation;
using Maes.Simulation.SimulationScenarios;
using Maes.UI;

using NUnit.Framework;

using UnityEngine;
using UnityEngine.TestTools;

namespace PlayModeTests
{
    using MySimulationScenario = ExplorationSimulationScenario;
    using MySimulator = ExplorationSimulator;

    [TestFixture(1.0f)]
    [TestFixture(1.5f)]
    [TestFixture(0.5f)]
    public class Robot2DControllerTest
    {

        private const int RandomSeed = 123;
        private MySimulator _maes;
        private TestingAlgorithm _testAlgorithm;
        private ExplorationSimulation _simulationBase;
        private MonaRobot _robot;

        private readonly float _relativeMoveSpeed;

        private Vector2Int _currentCoarseTile => Vector2Int.FloorToInt(_robot.Controller.SlamMap.CoarseMap.GetApproximatePosition());
        public Robot2DControllerTest(float relativeMoveSpeed)
        {
            _relativeMoveSpeed = relativeMoveSpeed;
        }

        [SetUp]
        public void InitializeTestingSimulator()
        {
            var testingScenario = new MySimulationScenario(RandomSeed,
                mapSpawner: StandardTestingConfiguration.EmptyCaveMapSpawner(RandomSeed),
                hasFinishedSim: _ => false,
                robotConstraints: new RobotConstraints(relativeMoveSpeed: _relativeMoveSpeed, mapKnown: true),
                robotSpawner: (map, spawner) => spawner.SpawnRobotsTogether(map, RandomSeed, 1,
                    Vector2Int.zero, _ =>
                    {
                        var algorithm = new TestingAlgorithm();
                        _testAlgorithm = algorithm;
                        return algorithm;
                    }));

            _maes = new MySimulator();
            _maes.EnqueueScenario(testingScenario);
            _simulationBase = _maes.SimulationManager.CurrentSimulation ?? throw new InvalidOperationException("CurrentSimulation is null");
            _robot = _simulationBase.Robots[0];
        }

        [TearDown]
        public void ClearSimulator()
        {
            _maes.Destroy();
        }


        // Test that the robot is able to move the given distance
        [UnityTest]
        [TestCase(1.0f, ExpectedResult = null)]
        [TestCase(2.0f, ExpectedResult = null)]
        [TestCase(5.0f, ExpectedResult = null)]
        [TestCase(10.0f, ExpectedResult = null)]
        [TestCase(20.0f, ExpectedResult = null)]
        public IEnumerator MoveTo_IsDistanceCorrectTest(float movementDistance)
        {
            _testAlgorithm.UpdateFunction = (tick, controller) =>
            {
                if (tick == 0)
                {
                    controller.Move(movementDistance);
                }
            };
            var controller = _robot.Controller;

            // Register the starting position and calculate the expected position
            var transform = _robot.transform;
            var startingPosition = transform.position;
            var expectedEndingPosition = startingPosition + (controller.GetForwardDirectionVector() * movementDistance);

            _maes.SimulationManager.AttemptSetPlayState(SimulationPlayState.FastAsPossible);

            // Wait until the robot has started and completed the movement task
            while (_testAlgorithm.Tick < 10 || _testAlgorithm.Controller.GetStatus() != RobotStatus.Idle)
            {
                yield return null;
            }

            //  Wait 1 second (10 ticks) for the robot to stand completely still
            var movementTaskEndTick = _simulationBase.SimulatedLogicTicks;
            const int ticksToWait = 10;
            while (_simulationBase.SimulatedLogicTicks < movementTaskEndTick + ticksToWait)
            {
                yield return null;
            }

            // Assert that the actual final position approximately matches the expected final position
            var endingPosition = _robot.transform.position;
            const float maximumDeviation = 0.1f;
            var targetPositionDelta = (expectedEndingPosition - endingPosition).magnitude;
            Debug.Log($"Actual: {endingPosition}  vs  expected: {expectedEndingPosition}");
            Assert.LessOrEqual(targetPositionDelta, maximumDeviation);
        }

        [UnityTest]
        [TestCase(1.0f, ExpectedResult = null)]
        [TestCase(-1.0f, ExpectedResult = null)]
        [TestCase(2.0f, ExpectedResult = null)]
        [TestCase(5.0f, ExpectedResult = null)]
        [TestCase(10.0f, ExpectedResult = null)]
        [TestCase(20.0f, ExpectedResult = null)]
        [TestCase(-20.0f, ExpectedResult = null)]
        [TestCase(180.0f, ExpectedResult = null)]
        [TestCase(-180.0f, ExpectedResult = null)]
        public IEnumerator Rotate_RotatesCorrectAmountOfDegrees(float degreesToRotate)
        {
            _testAlgorithm.UpdateFunction = (tick, controller) => { if (tick == 1) { controller.Rotate(degreesToRotate); } };

            // Register the starting position and calculate the expected position
            var transform = _robot.transform;
            var startingRotation = transform.rotation.eulerAngles.z;
            var expectedAngle = startingRotation + degreesToRotate;
            while (expectedAngle < 0)
            {
                expectedAngle += 360;
            }

            expectedAngle %= 360;

            _maes.SimulationManager.AttemptSetPlayState(SimulationPlayState.FastAsPossible);

            // Wait until the robot has started and completed the movement task
            while (_testAlgorithm.Tick < 10 || _testAlgorithm.Controller.GetStatus() != RobotStatus.Idle)
            {
                yield return null;
            }
            //  Wait 1 second (10 ticks) for the robot to stand completely still
            var movementTaskEndTick = _simulationBase.SimulatedLogicTicks;
            const int ticksToWait = 10;
            while (_simulationBase.SimulatedLogicTicks < movementTaskEndTick + ticksToWait)
            {
                yield return null;
            }

            // Assert that the actual final rotation approximately matches the expected angle
            var actualAngle = _robot.transform.rotation.eulerAngles.z;
            const float maximumDeviationDegrees = 0.5f;
            var targetPositionDelta = Mathf.Abs(expectedAngle - actualAngle);
            Debug.Log($"Actual final angle: {actualAngle}  vs  expected angle: {expectedAngle}");
            Assert.LessOrEqual(targetPositionDelta, maximumDeviationDegrees);
        }

        [UnityTest]
        [TestCase(2.0f, ExpectedResult = null)]
        [TestCase(2.1f, ExpectedResult = null)]
        [TestCase(2.5f, ExpectedResult = null)]
        [TestCase(3.0f, ExpectedResult = null)]
        public IEnumerator EstimateDistanceToTarget_IsDistanceCorrectTest(float actualDistance)
        {
            var coarseMapStartingPosition = Vector2Int.FloorToInt(_robot.Controller.SlamMap.CoarseMap.GetApproximatePosition());
            var cellOffset = (int)Math.Round(actualDistance / _robot.Controller.SlamMap.CoarseMap.CellSize());
            var coarseMapTargetPosition = coarseMapStartingPosition + new Vector2Int(0, cellOffset);

            var estimatedDistance = _robot.Controller.EstimateDistanceToTarget(coarseMapTargetPosition);

            const float maximumDeviation = 0.5f;
            Debug.Log($"{nameof(coarseMapStartingPosition)}: {coarseMapStartingPosition}, {nameof(coarseMapTargetPosition)}: {coarseMapTargetPosition}, {nameof(actualDistance)}: {actualDistance}");
            Debug.Log($"Actual distance: {actualDistance}, estimated distance: {estimatedDistance}");
            var targetPositionDelta = Math.Abs(actualDistance - estimatedDistance.Value);
            Assert.LessOrEqual(targetPositionDelta, maximumDeviation);
            yield return null;
        }

        [UnityTest]
        [TestCase(0.1f, ExpectedResult = null)]
        [TestCase(1.0f, ExpectedResult = null)]
        [TestCase(2.0f, ExpectedResult = null)]
        [TestCase(2.1f, ExpectedResult = null)]
        [TestCase(2.5f, ExpectedResult = null)]
        [TestCase(2.6f, ExpectedResult = null)]
        [TestCase(3.0f, ExpectedResult = null)]
        [TestCase(4.0f, ExpectedResult = null)]
        [TestCase(5.0f, ExpectedResult = null)]
        [TestCase(7.5f, ExpectedResult = null)]
        [TestCase(10.0f, ExpectedResult = null)]
        [TestCase(15.0f, ExpectedResult = null)]
        [TestCase(20.0f, ExpectedResult = null)]
        public IEnumerator EstimateTimeToTarget_IsTimeCorrectTest(float actualDistance)
        {
            var debug = false;
            var coarseMapStartingPosition = _currentCoarseTile;
            var cellOffset = (int)Math.Round(actualDistance / _robot.Controller.SlamMap.CoarseMap.CellSize());
            var coarseMapTargetPosition = coarseMapStartingPosition + new Vector2Int(0, cellOffset);
            var estimatedTime = _robot.Controller.EstimateTimeToTarget(coarseMapTargetPosition).Value;

            // Make the robot move to target.
            _testAlgorithm.UpdateFunction = (tick, controller) =>
            {
                controller.PathAndMoveTo(coarseMapTargetPosition);
            };

            //Debug.Log($"PathAndMoveTo coarseTile from: {coarseMapStartingPosition}, to: {coarseMapTargetPosition}");
            _maes.PressPlayButton();
            var prevTick = -1;
            while (_testAlgorithm.Tick == 0 || _currentCoarseTile != coarseMapTargetPosition)
            {
                if (debug && prevTick != _testAlgorithm.Tick)
                {
                    prevTick = _testAlgorithm.Tick;
                    //Debug.Log($"Tick: {_testAlgorithm.Tick}, current position: {_currentCoarseTile}, current status: {_testAlgorithm.Controller.GetStatus()}");
                }
                yield return null;
            }
            var maximumDeviation = 3 + (int)Math.Floor(actualDistance / 10f);
            // Debug.Log($"Final tick: {_testAlgorithm.Tick}, current position: {_currentCoarseTile}, current status: {_testAlgorithm.Controller.GetStatus()}");
            Debug.Log($"Cells moved: {cellOffset}, dist: {actualDistance}, {nameof(estimatedTime)}: {estimatedTime}, {nameof(_testAlgorithm.Tick)}: {_testAlgorithm.Tick}");
            var targetTimeDelta = Math.Abs(_testAlgorithm.Tick - estimatedTime);
            Assert.LessOrEqual(targetTimeDelta, maximumDeviation);
            yield return null;
        }
    }
}