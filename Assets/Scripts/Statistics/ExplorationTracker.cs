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
using System.Linq;

using JetBrains.Annotations;
using Maes.Map;
using Maes.Map.MapGen;

using MAES.Map.Visualization.Exploration;

using Maes.Robot;

using MAES.Trackers;

using Maes.Utilities;

using UnityEngine;

namespace Maes.Statistics {
    public class ExplorationTracker : Tracker<ExplorationCell, ExplorationVisualizer, IExplorationVisualizationMode>
    {
        private CoverageCalculator _coverageCalculator;
        
        // The low-resolution collision map used to create the smoothed map that robots are navigating 
        private SimulationMap<Tile> _collisionMap;

        private readonly int _totalExplorableTriangles;
        public int ExploredTriangles { get; private set; }
        public int CoveredMiniTiles => _coverageCalculator.CoveredMiniTiles;

        public float ExploredProportion => ExploredTriangles / (float) _totalExplorableTriangles;
        // Coverage is measured in 'mini-tiles'. Each large map tile consists of 4 mini-tiles, 
        // where each mini-tile is composed of two triangles
        public float CoverageProportion => _coverageCalculator.CoverageProportion;
        
        public List<SnapShot<float>> _coverSnapshots = new List<SnapShot<float>>();
        public List<SnapShot<float>> _exploreSnapshots = new List<SnapShot<float>>();
        public List<SnapShot<float>> _distanceSnapshots = new List<SnapShot<float>>();
        private float mostRecentDistance;

        public struct SnapShot<TValue> {
            public readonly int Tick;
            public readonly TValue Value;

            public SnapShot(int tick, TValue value) {
                Tick = tick;
                this.Value = value;
            }
        }

        public ExplorationTracker(SimulationMap<Tile> collisionMap, ExplorationVisualizer explorationVisualizer, RobotConstraints constraints) : base(collisionMap, explorationVisualizer, constraints, tile => new ExplorationCell(isExplorable: !Tile.IsWall(tile.Type))) {
            var explorableTriangles = 0;
            _collisionMap = collisionMap;
            _constraints = constraints;
            
            _currentVisualizationMode = new AllRobotsExplorationVisualization(_map);
            _totalExplorableTriangles = collisionMap.Count(x => !Tile.IsWall(x.Item2.Type));

            _coverageCalculator = new CoverageCalculator(_map, collisionMap);
        }

        protected override void CreateSnapShot() {
            _coverSnapshots.Add(new SnapShot<float>(_currentTick, CoverageProportion * 100));
            _exploreSnapshots.Add(new SnapShot<float>(_currentTick, ExploredProportion * 100));
            _distanceSnapshots.Add(new SnapShot<float>(_currentTick, mostRecentDistance));
        }

        private float CalculateAverageDistance(IReadOnlyList<MonaRobot> robots){
            List<float> averages = new List<float>();
            float average = 0;
            float sum = 0;
            foreach (var robot in robots) {
                var robotPosition = robot.transform.position;
                foreach (var otherRobot in robots){
                    var otherRobotPosition = otherRobot.transform.position;
                    averages.Add((float)Math.Sqrt(Math.Pow(robotPosition.x - otherRobotPosition.x, 2) + Math.Pow(robotPosition.y - otherRobotPosition.y, 2) + Math.Pow(robotPosition.z - otherRobotPosition.z, 2)));
                }
            }
            foreach (var number in averages) {
                sum += number;
            }
            average = sum / averages.Count;
            return average;
        }
        private void UpdateCoverageStatus(MonaRobot robot) {
            var newlyCoveredCells = new List<(int, ExplorationCell)> {};
            var robotPos = robot.transform.position;
            
            // Find each mini tile (two triangle cells) covered by the robot and execute the following function on it
            _coverageCalculator.UpdateRobotCoverage(robotPos, _currentTick,(index1, triangle1, index2, triangle2) => {
                if (!triangle1.IsCovered) {
                    // This tile was not covered before, register as newly covered
                    newlyCoveredCells.Add((index1, triangle1));
                    newlyCoveredCells.Add((index2, triangle2));
                }
            });
            
            _currentVisualizationMode.RegisterNewlyCoveredCells(robot, newlyCoveredCells);
        }

        private Vector2Int GetCoverageMapPosition(Vector2 robotPosition) {
            robotPosition -= _collisionMap.ScaledOffset;
            return new Vector2Int((int)robotPosition.x, (int)robotPosition.y);
        }
        
        protected override void OnFirstTick(IReadOnlyList<MonaRobot> robots) {
            foreach (var robot in robots) UpdateCoverageStatus(robot);
            mostRecentDistance = CalculateAverageDistance(robots);
        }

        List<(int, ExplorationCell)> newlyExploredTriangles = new List<(int, ExplorationCell)>();
        protected override void AfterRayTracingARobot(MonaRobot robot)
        {
            // Register newly explored cells of this robot for visualization
            _currentVisualizationMode.RegisterNewlyExploredCells(robot, newlyExploredTriangles);
            newlyExploredTriangles.Clear();
        }

        protected override RayTracingMap<ExplorationCell>.CellFunction RayTracingMapCellFunction(SlamMap slamMap)
        {
            return (index, cell) =>
            {
                if (cell.IsExplorable)
                {
                    if (!cell.IsExplored)
                    {
                        cell.IsExplored = true;
                        cell.LastExplorationTimeInTicks = _currentTick;
                        cell.ExplorationTimeInTicks += 1;
                        newlyExploredTriangles.Add((index, cell));
                        ExploredTriangles++;
                    }

                    cell.RegisterExploration(_currentTick);
                }

                // Update robot slam map if present (slam map only non-null if 'shouldUpdateSlamMap' is true)
                slamMap?.SetExploredByTriangle(triangleIndex: index, isOpen: cell.IsExplorable);
                slamMap?.SetCurrentlyVisibleByTriangle(triangleIndex: index, isOpen: cell.IsExplorable);

                return cell.IsExplorable;
            };
        }

        public override void SetVisualizedRobot([CanBeNull] MonaRobot robot) {
            _selectedRobot = robot;
            if (_selectedRobot != null)
                SetVisualizationMode(new CurrentlyVisibleAreaVisualization(_map, _selectedRobot.Controller));
            else 
                // Revert to all robots exploration visualization when current robot is deselected
                // while visualization mode is based on the selected robot
                SetVisualizationMode(new AllRobotsExplorationVisualization(_map));
        }

        public void ShowAllRobotExploration() {
            SetVisualizationMode(new AllRobotsExplorationVisualization(_map));
        }

        public void ShowAllRobotCoverage() {
            SetVisualizationMode(new AllRobotsCoverageVisualization(_map));
        }

        public void ShowAllRobotCoverageHeatMap() {
            SetVisualizationMode(new CoverageHeatMapVisualization(_map));
        }

        public void ShowAllRobotExplorationHeatMap() {
            SetVisualizationMode(new ExplorationHeatMapVisualization(_map));
        }
        
        public void ShowSelectedRobotVisibleArea() {
            if (_selectedRobot == null)
                throw new Exception("Cannot change to 'ShowSelectedRobotVisibleArea' visualization mode when no robot is selected");
            SetVisualizationMode(new CurrentlyVisibleAreaVisualization(_map, _selectedRobot.Controller));
        }

        public void ShowSelectedRobotSlamMap() {
            if (_selectedRobot == null)
                throw new Exception("Cannot change to 'ShowSelectedRobotSlamMap' visualization mode when no robot is selected");
            SetVisualizationMode(new SelectedRobotSlamMapVisualization(_map, _selectedRobot.Controller));
        }
    }
}