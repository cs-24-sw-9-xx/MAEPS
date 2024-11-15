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

using Maes.Map;
using Maes.Map.MapGen;
using Maes.Map.Visualization.Exploration;
using Maes.Robot;
using Maes.Statistics.Exploration;
using Maes.Trackers;

using UnityEngine;

namespace Maes.Statistics
{
    public class ExplorationTracker : Tracker<ExplorationCell, ExplorationVisualizer, IExplorationVisualizationMode>
    {
        // The low-resolution collision map used to create the smoothed map that robots are navigating 
        private readonly SimulationMap<Tile> _collisionMap;

        private readonly int _totalExplorableTriangles;
        public int ExploredTriangles { get; private set; }
        public int CoveredMiniTiles => _coverageCalculator.CoveredMiniTiles;

        public float ExploredProportion => ExploredTriangles / (float)_totalExplorableTriangles;
        // Coverage is measured in 'mini-tiles'. Each large map tile consists of 4 mini-tiles, 
        // where each mini-tile is composed of two triangles
        public float CoverageProportion => _coverageCalculator.CoverageProportion;

        public readonly List<ExplorationSnapShot> SnapShots = new();
        private float _mostRecentDistance;

        private readonly List<(int, ExplorationCell)> _newlyExploredTriangles = new();

        public ExplorationTracker(SimulationMap<Tile> collisionMap, ExplorationVisualizer explorationVisualizer, RobotConstraints constraints)
            : base(collisionMap, explorationVisualizer, constraints, tile => new ExplorationCell(isExplorable: !Tile.IsWall(tile.Type)))
        {
            _collisionMap = collisionMap;
            _constraints = constraints;

            _currentVisualizationMode = new AllRobotsExplorationVisualization(_map);
            _totalExplorableTriangles = collisionMap.Count(x => !Tile.IsWall(x.Item2.Type));
        }

        protected override void CreateSnapShot()
        {
            SnapShots.Add(new ExplorationSnapShot(_currentTick, ExploredProportion, CoverageProportion, _mostRecentDistance));
        }

        private static float CalculateAverageDistance(MonaRobot[] robots)
        {
            var sum = 0f;
            var count = 0;
            for (var i = 0; i < robots.Length; i++)
            {
                var robot = robots[i];
                var robotPosition = robot.transform.position;
                for (var j = i + 1; j < robots.Length; j++)
                {
                    var otherRobot = robots[j];
                    var otherRobotPosition = otherRobot.transform.position;
                    sum += Mathf.Sqrt(
                        Mathf.Pow(robotPosition.x - otherRobotPosition.x, 2) +
                        Mathf.Pow(robotPosition.y - otherRobotPosition.y, 2) +
                        Mathf.Pow(robotPosition.z - otherRobotPosition.z, 2));
                    count++;
                }
            }

            return sum / count;
        }

        private readonly List<(int, ExplorationCell)> _newlyCoveredCells = new();

        protected override void UpdateCoverageStatus(MonaRobot robot)
        {
            _newlyCoveredCells.Clear();
            base.UpdateCoverageStatus(robot);
        }

        protected override void PreCoverageTileConsumer(int index1, ExplorationCell triangle1, int index2, ExplorationCell triangle2)
        {
            if (triangle1.IsCovered)
            {
                return;
            }

            // This tile was not covered before, register as newly covered
            _newlyCoveredCells.Add((index1, triangle1));
            _newlyCoveredCells.Add((index2, triangle2));
        }

        protected override void AfterUpdateCoverageStatus(MonaRobot robot)
        {
            _currentVisualizationMode.RegisterNewlyCoveredCells(robot, _newlyCoveredCells);
        }

        private Vector2Int GetCoverageMapPosition(Vector2 robotPosition)
        {
            robotPosition -= _collisionMap.ScaledOffset;
            return new Vector2Int((int)robotPosition.x, (int)robotPosition.y);
        }

        protected override void OnAfterFirstTick(MonaRobot[] robots)
        {
            _mostRecentDistance = CalculateAverageDistance(robots);
            base.OnAfterFirstTick(robots);
        }

        protected override void AfterRayTracingARobot(MonaRobot robot)
        {
            // Register newly explored cells of this robot for visualization
            _currentVisualizationMode.RegisterNewlyExploredCells(robot, _newlyExploredTriangles);
            _newlyExploredTriangles.Clear();
        }

        protected override void OnNewlyExploredTriangles(int index, ExplorationCell cell)
        {
            _newlyExploredTriangles.Add((index, cell));
            ExploredTriangles++;
        }

        public override void SetVisualizedRobot(MonaRobot? robot)
        {
            _selectedRobot = robot;
            if (_selectedRobot != null)
            {
                SetVisualizationMode(new CurrentlyVisibleAreaVisualizationExploration(_map, _selectedRobot.Controller));
            }
            else
            {
                // Revert to all robots exploration visualization when current robot is deselected
                // while visualization mode is based on the selected robot
                SetVisualizationMode(new AllRobotsExplorationVisualization(_map));
            }
        }

        public void ShowAllRobotExploration()
        {
            SetVisualizationMode(new AllRobotsExplorationVisualization(_map));
        }

        public void ShowAllRobotCoverage()
        {
            SetVisualizationMode(new AllRobotsCoverageVisualization(_map));
        }

        public void ShowAllRobotCoverageHeatMap()
        {
            SetVisualizationMode(new CoverageHeatMapVisualization(_map));
        }

        public void ShowAllRobotExplorationHeatMap()
        {
            SetVisualizationMode(new ExplorationHeatMapVisualization(_map));
        }

        public void ShowSelectedRobotVisibleArea()
        {
            if (_selectedRobot == null)
            {
                throw new Exception("Cannot change to 'ShowSelectedRobotVisibleArea' visualization mode when no robot is selected");
            }

            SetVisualizationMode(new CurrentlyVisibleAreaVisualizationExploration(_map, _selectedRobot.Controller));
        }

        public void ShowSelectedRobotSlamMap()
        {
            if (_selectedRobot == null)
            {
                throw new Exception("Cannot change to 'ShowSelectedRobotSlamMap' visualization mode when no robot is selected");
            }

            SetVisualizationMode(new SelectedRobotSlamMapVisualization(_selectedRobot.Controller));
        }
    }
}