using System.Collections.Generic;

using Maes.Algorithms.Patrolling;
using Maes.Algorithms.Patrolling.HMPPatrollingAlgorithms.FaultTolerance;
using Maes.Map.Generators;
using Maes.Map.Generators.Patrolling.Waypoints.Generators;
using Maes.Simulation.Patrolling;
using Maes.UI;

using UnityEngine;

namespace Maes.Experiments.Patrolling.GroupA
{
    using static Map.RobotSpawners.RobotSpawner<IPatrollingAlgorithm>;

    using MySimulationScenario = PatrollingSimulationScenario;
    using MySimulator = PatrollingSimulator;

    public class GroupADemoExperiment : MonoBehaviour
    {
        private const int Seed = 123;
        private MySimulator _simulator = null!;

        private int _nextPausePointIndex = 0;
        private readonly (int tick, string message)[] _pausePoints = new[]
        {
            (10, "See robot 3 meeting stigmergy."),
            (6300, "See robot 3 meeting."),
            (12300, "See robot 3 meeting. Kill robot 0 before meeting."),
            (18300, "See robot 3 meeting. Scheduling problem."),
            (24300, "See robot 3 meeting. Taking over partition."),
            (30300, "See robot 3 meeting. Other robot is not taking over as it is already there."),
        };

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private void Start()
        {
            var scenarios = new List<MySimulationScenario>();
            PatrollingMapFactory patrollingMapFactory = map => AllWaypointConnectedGenerator.MakePatrollingMap(map, GroupAParameters.MaxDistance);
            CreateAlgorithmDelegate algorithm = seed => new HMPPatrollingAlgorithm(seed, false, false, true);

            var mapConfig = new CaveMapConfig(
                Seed, brokenCollisionMap: false);

            scenarios.Add(new MySimulationScenario(
                Seed,
                int.MaxValue,
                false,
                (map, spawner) => spawner.SpawnRobotsApart(map, Seed, 4, algorithm, dependOnBrokenBehavior: false),
                MySimulationScenario.InfallibleToFallibleSimulationEndCriteria(sim => sim.SimulatedLogicTicks >= 300000),
                mapSpawner => mapSpawner.GenerateMap(mapConfig),
                GroupAParameters.CreateRobotConstraints(),
                "delete-me.csv",
                patrollingMapFactory));

            Debug.Log($"Total scenarios scheduled: {scenarios.Count}");

            _simulator = new MySimulator(scenarios, autoMaxSpeedInBatchMode: false);

            _simulator.PressPlayButton(); // Instantly enter play mode
        }

        private void FixedUpdate()
        {
            if (_nextPausePointIndex >= _pausePoints.Length)
            {
                return;
            }

            var currentPausePoint = _pausePoints[_nextPausePointIndex];
            if (_simulator.SimulationManager.CurrentSimulation!.SimulatedLogicTicks >= currentPausePoint.tick)
            {
                _simulator.SimulationManager.AttemptSetPlayState(SimulationPlayState.Paused);
                Debug.Log(currentPausePoint.message);
                _nextPausePointIndex++;
            }
        }
    }
}