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
using System.Collections.Generic;

using Maes.Algorithms;
using Maes.FaultInjections;
using Maes.Map;
using Maes.Map.Generators;
using Maes.Map.RobotSpawners;
using Maes.Robot;

namespace Maes.Simulation
{

    // A function that generates, initializes and returns a world map
    public delegate SimulationMap<Tile> MapFactory(MapSpawner generator);
    // A function that spawns and returns a group of robots
    public delegate List<MonaRobot> RobotFactory<TAlgorithm>(SimulationMap<Tile> map, RobotSpawner<TAlgorithm> spawner) where TAlgorithm : IAlgorithm;

    // A function that returns true if the given simulation has been completed
    public delegate bool SimulationEndCriteriaDelegate<TSimulation>(TSimulation simulationBase, out SimulationEndCriteriaReason? reason)
        where TSimulation : ISimulation;

    public delegate bool SimulationEndCriteriaInfallibleDelegate<TSimulation>(TSimulation simulationBase)
        where TSimulation : ISimulation;

    public readonly struct SimulationEndCriteriaReason
    {
        public string Message { get; }
        public bool Success { get; }

        public SimulationEndCriteriaReason(string message, bool success)
        {
            Message = message;
            Success = success;
        }
    }

    // Contains all information needed for simulating a single simulation scenario
    // (One map, one type of robots)
    public abstract class SimulationScenario<TSimulation, TAlgorithm> : ISimulationScenario
    where TSimulation : ISimulation
    where TAlgorithm : IAlgorithm
    {
        public const int DefaultMaxLogicTicks = 100000;

        public SimulationEndCriteriaDelegate<TSimulation> HasFinishedSim { get; protected set; }

        public MapFactory MapSpawner { get; }
        public RobotFactory<TAlgorithm> RobotSpawner { get; }
        public RobotConstraints RobotConstraints { get; }
        public string StatisticsFileName { get; set; }

        public IFaultInjection? FaultInjection { get; }

        /// <summary>
        /// How many ticks before the scenario is marked as stuck.
        /// </summary>
        public int MaxLogicTicks { get; }

        protected SimulationScenario(
            int seed,
            RobotFactory<TAlgorithm> robotSpawner,
            SimulationEndCriteriaDelegate<TSimulation>? hasFinishedSim = null,
            MapFactory? mapSpawner = null,
            RobotConstraints? robotConstraints = null,
            string? statisticsFileName = null,
            IFaultInjection? faultInjection = null,
            int maxLogicTicks = DefaultMaxLogicTicks
            )
        {
            MaxLogicTicks = maxLogicTicks;
            HasFinishedSim = hasFinishedSim ?? DefaultHasFinishedSim;
            // Default to generating a cave map when no map generator is specified
            MapSpawner = mapSpawner ?? (generator => generator.GenerateMap(new CaveMapConfig(seed)));
            RobotSpawner = robotSpawner;
            RobotConstraints = robotConstraints ?? new RobotConstraints();
            StatisticsFileName = statisticsFileName ?? $"statistics_{DateTime.Now.ToShortDateString().Replace('/', '-')}_{DateTime.Now.ToLongTimeString().Replace(' ', '-').Replace(':', '-')}";
            FaultInjection = faultInjection;
        }

        public static SimulationEndCriteriaDelegate<TSimulation> InfallibleToFallibleSimulationEndCriteria(
            SimulationEndCriteriaInfallibleDelegate<TSimulation> @delegate)
        {
            return ((TSimulation simulation, out SimulationEndCriteriaReason? reason) =>
            {
                var value = @delegate(simulation);
                if (value)
                {
                    reason = new SimulationEndCriteriaReason("Success", true);
                }
                else
                {
                    reason = null;
                }

                return value;
            });
        }

        private bool DefaultHasFinishedSim(TSimulation simulation, out SimulationEndCriteriaReason? reason)
        {
            if (simulation.HasFinishedSim())
            {
                reason = new SimulationEndCriteriaReason("Success", true);
                return true;
            }

            if (simulation.SimulatedLogicTicks > MaxLogicTicks)
            {
                reason = new SimulationEndCriteriaReason("Max ticks reached (stuck?)", false);
                return true;
            }

            reason = null;
            return false;
        }
    }
}