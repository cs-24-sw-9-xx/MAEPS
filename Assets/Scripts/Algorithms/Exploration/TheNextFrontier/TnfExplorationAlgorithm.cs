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

using System.Collections.Generic;
using System.Linq;
using System.Text;

using Maes.Map;
using Maes.Map.PathFinding;
using Maes.Robot;
using Maes.Robot.Tasks;
using Maes.Utilities;

using UnityEngine;

namespace Maes.Algorithms.Exploration.TheNextFrontier
{
    public class TnfExplorationAlgorithm : IExplorationAlgorithm
    {
        private static class Gaussian
        {
            private const float A = 5f, B = .5f, C = .1f; // Magnitude, Mean, and Spread.
            private const float Alpha = -1 / (C * C * 2);
            private const float Beta = B / (C * C);
            private static readonly float Gamma;

            static Gaussian()
            { // My best suggested constexpr substitute
                Gamma = Mathf.Log(A) - B * B / (2 * C * C);
            }

            /// <returns>
            /// value based on <a href="https://en.wikipedia.org/wiki/Gaussian_function#Properties">this</a> formula.
            /// </returns>
            public static float Gauss(float x)
            {
                return Mathf.Exp(Alpha * x * x + Beta * x + Gamma);
            }
        }

        private enum TnfStatus
        {
            AwaitMoving,
            AwaitRotating,
            AwaitNextFrontier,
            AwaitCollisionMitigation,
            OutOfFrontiers
        }

        // Set by SetController
        private IRobotController _robotController = null!;

        // Set by SetController
        private CoarseGrainedMap _map = null!;

        private TnfStatus _robotTnfStatus;
        private int Alpha { get; }
        private int Beta { get; }

        private List<Frontier>? _frontiers;

        private Vector2 _robotPos;
        private Vector2Int _robotPosInt;
        private int _robotId = -1;
        private LinkedList<PathStep>? _path;
        private PathStep? _nextTileInPath;
        private const float AngleDelta = .5f;
        private const float MinimumMoveDistance = .3f;

        private int _logicTicks;
        private int _logicTicksSinceLastCommunication;
        private bool _bonked;
        private readonly System.Random _random;
        private bool _isCommunicating;
        private int _ticksSpentColliding;
        private readonly List<(int, Vector2)> _currentDestinations = new();


        private class Frontier
        {
            public readonly List<(Vector2Int, float)> Cells;

            public Frontier(List<(Vector2Int, float)> cells)
            {
                Cells = cells;
                UtilityValue = -1;
            }

            public float UtilityValue { get; set; }
        }

        public TnfExplorationAlgorithm(int alpha, int beta, int randomSeed)
        {
            Alpha = alpha;
            Beta = beta;
            _robotTnfStatus = TnfStatus.AwaitNextFrontier;
            _random = new System.Random(randomSeed);
        }

        private float InformationPotential((Vector2Int, float) cell, List<(Vector2Int, float)> neighbours)
        {
            return Gaussian.Gauss(cell.Item2) + neighbours.Sum(neighbour => Gaussian.Gauss(neighbour.Item2));
        }

        private float InformationFactor(Frontier frontier)
        {
            return frontier.Cells.Sum(c => InformationPotential(c, GetNeighbouringCells(c))) / _frontiers!.Count;
        }

        private List<(Vector2Int, float)> GetNeighbouringCells((Vector2Int, float) cell)
        {
            var res = new List<(Vector2Int, float)>();

            for (var x = cell.Item1.x - 1; x <= cell.Item1.x + 1; x++)
            {
                for (var y = cell.Item1.y - 1; y <= cell.Item1.y + 1; y++)
                {
                    if (x == cell.Item1.x && y == cell.Item1.y)
                    {
                        continue;
                    }
                    var position = new Vector2Int(x, y);
                    var v = _map.GetTileStatus(position).ToTnfCellValue();
                    res.Add((position, v));
                }
            }

            return res;
        }

        private float WavefrontNormalized(Frontier frontier, Vector2 fromPosition, float normalizerConstant)
        {
            var wave = Wavefront(frontier, fromPosition).ToList();
            var distFactor = Mathf.Sqrt(wave.Select(d => d / normalizerConstant).Sum());
            return distFactor;
        }

        private float DistanceFactor(Frontier f, Vector2 from, float normalizerConstant)
        {
            var val = WavefrontNormalized(f, from, normalizerConstant);
            return Mathf.Pow(val, Alpha - 1) * Mathf.Pow(1 - val, Beta - 1);
        }

        private IEnumerable<float> Wavefront(Frontier frontier, Vector2 fromPosition)
        {
            return frontier.Cells.Select(cell => Vector2.Distance(fromPosition, cell.Item1));
        }

        private float CoordinationFactor(Frontier frontier)
        {
            var lastSeenNeighbours = _robotController.SenseNearbyRobots();
            if (lastSeenNeighbours.Length <= 0)
            {
                return 0;
            }

            var sum = 0f;
            foreach (var neighbour in lastSeenNeighbours)
            {
                var pos = _robotPos + Geometry.VectorFromDegreesAndMagnitude(neighbour.Angle, neighbour.Distance);
                var normalizerConstant = _frontiers!.Max(f => f.Cells.Select(c => Vector2.Distance(pos, c.Item1)).Max());
                sum += WavefrontNormalized(frontier, pos, normalizerConstant);
            }
            return sum;
        }

        private float UtilityFunction(Frontier frontier, float normalizerConstant)
        {
            return InformationFactor(frontier) + DistanceFactor(frontier, _robotPos, normalizerConstant) - CoordinationFactor(frontier);
        }

        public IEnumerable<WaitForCondition> PreUpdateLogic()
        {
            while (true)
            {
                yield return WaitForCondition.ContinueUpdateLogic();
            }
        }

        public IEnumerable<WaitForCondition> UpdateLogic()
        {
            while (true)
            {
                _logicTicks++;
                _robotPos = _map.GetApproximatePosition();
                _robotPosInt = Vector2Int.RoundToInt(_robotPos);

                if (_logicTicksSinceLastCommunication == 0)
                {
                    var lastSeenNeighbours = _robotController.SenseNearbyRobots();
                    if (lastSeenNeighbours.Any())
                    {
                        _robotController.StopCurrentTask();
                        _isCommunicating = true;
                        if (_path != null && _path.Count > 0)
                        {
                            var lastStep = _path.Last.Value;
                            _robotController.Broadcast((_robotController.SlamMap, _robotController.Id, (Vector2)lastStep.End));
                        }
                        else
                        {
                            _robotController.Broadcast((_robotController.SlamMap, _robotController.Id, _robotPos));
                        }
                    }
                    _logicTicksSinceLastCommunication++;
                    yield return WaitForCondition.WaitForLogicTicks(1);
                    continue;
                }
                _logicTicksSinceLastCommunication++;
                _logicTicksSinceLastCommunication %= 20;

                if (_robotController.HasCollidedSinceLastLogicTick)
                {
                    _robotController.StopCurrentTask();
                    _bonked = true;
                    _robotTnfStatus = TnfStatus.AwaitCollisionMitigation;
                }

                if (_robotController.IsCurrentlyColliding)
                {
                    _ticksSpentColliding++;
                }

                if (_ticksSpentColliding == 5)
                {
                    HardReset();
                }

                if (_isCommunicating)
                {
                    AwaitCommunication();
                    _isCommunicating = false;
                }

                switch (_robotTnfStatus)
                {
                    case TnfStatus.AwaitNextFrontier:
                        var nextFrontier = CalculateNextFrontier(_robotPos);
                        if (nextFrontier == null)
                        {
                            break;
                        }
                        MoveToFrontier(nextFrontier);
                        break;
                    case TnfStatus.AwaitMoving:
                    case TnfStatus.AwaitRotating:
                        DoMovement(_path);
                        break;
                    case TnfStatus.AwaitCollisionMitigation:
                        CollisionMitigation();
                        break;
                    case TnfStatus.OutOfFrontiers:
                        break;
                }

                yield return WaitForCondition.WaitForLogicTicks(1);
            }
        }

        private void HardReset()
        {
            _robotTnfStatus = TnfStatus.AwaitNextFrontier;
            _robotController.StopCurrentTask();
            _frontiers = null;
            _robotPos = _map.GetApproximatePosition();
            _robotPosInt = Vector2Int.RoundToInt(_robotPos);
            _path = null;
            _nextTileInPath = null;
            _bonked = false;
            _isCommunicating = false;
            _ticksSpentColliding = 0;
        }

        private void CollisionMitigation()
        {
            if (_robotController.Status != RobotStatus.Idle)
            {
                return;
            }

            if (_bonked)
            {
                _bonked = false;
                _robotController.Move(.1f, true);
                return;
            }

            var possibleOptions = GetNeighbouringCells((_robotPosInt, 0)).Select(c => c.Item1).ToList();
            var blockedOptions = new List<Vector2Int>();
            var others = _robotController.SenseNearbyRobots();
            foreach (var bot in others)
            {
                blockedOptions.Add(Vector2Int.RoundToInt(bot.GetRelativePosition(_robotPos, _map.GetApproximateGlobalDegrees())));
            }
            foreach (var position in possibleOptions)
            {
                if (_map.GetTileStatus(position) == SlamMap.SlamTileStatus.Solid)
                {
                    blockedOptions.Add(position);
                }
            }
            possibleOptions = possibleOptions.Except(blockedOptions).ToList();
            if (!possibleOptions.Any())
            {
                return;
            }
            var pickedOption = possibleOptions[_random.Next(0, possibleOptions.Count - 1)];

            _path?.Clear(); // No need to follow your old path - you're gonna get a new frontier from your new position.
            _nextTileInPath = new PathStep(_robotPosInt, pickedOption, new HashSet<Vector2Int>());
            _robotTnfStatus = TnfStatus.AwaitMoving;
        }

        private void AwaitCommunication()
        {
            var received = _robotController.ReceiveBroadcast();
            if (!received.Any())
            {
                return;
            }
            var newMaps = new List<SlamMap> { _robotController.SlamMap };
            foreach (var package in received)
            {
                var pack = ((SlamMap, int, Vector2))package;
                newMaps.Add(pack.Item1);
                if (_currentDestinations.Any(dict => dict.Item1 == pack.Item2))
                {
                    var index = _currentDestinations.FindIndex(dict => dict.Item1 == pack.Item2);
                    _currentDestinations[index] = (pack.Item2, pack.Item3);
                }
                else
                {
                    _currentDestinations.Add((pack.Item2, pack.Item3));
                }
            }

            // Largest Robot ID synchronizes to save on Simulator CPU time
            if (!received.Cast<(SlamMap, int, Vector2)>().Any(p => p.Item2 > _robotId))
            {
                SlamMap.Synchronize(newMaps, _logicTicks);
            }
            if (_robotTnfStatus == TnfStatus.OutOfFrontiers)
            {
                _robotTnfStatus = TnfStatus.AwaitNextFrontier;
            }

        }

        private Frontier? CalculateNextFrontier(Vector2 currentPosition)
        {
            _frontiers = GetFrontiers();

            if (_frontiers.Count <= 0)
            {
                return null;
            }
            var normalizerConstant = _frontiers.Max(f => f.Cells.Select(c => Vector2.Distance(currentPosition, c.Item1)).Max());
            foreach (var frontier in _frontiers)
            {
                frontier.UtilityValue = UtilityFunction(frontier, normalizerConstant);
            }
            return _frontiers.OrderByDescending(f => f.UtilityValue).First();

        }

        private void MoveToFrontier(Frontier bestFrontier)
        {
            var targetCell = GetFrontierMoveTarget(bestFrontier);
            var path = _map.GetTnfPathAsPathSteps(targetCell);
            if (path == null)
            {
                _frontiers!.Remove(bestFrontier);
                if (!_frontiers.Any())
                {
                    _robotTnfStatus = TnfStatus.OutOfFrontiers;
                    return;
                }
                var newFrontier = _frontiers.OrderByDescending(f => f.UtilityValue).First();
                MoveToFrontier(newFrontier);
                return;
            }

            _path = new LinkedList<PathStep>(path);
            if (!_path.Any())
            {
                _robotTnfStatus = TnfStatus.AwaitNextFrontier;
                return;
            }
            _robotTnfStatus = TnfStatus.AwaitMoving;
            var nextStep = _path.First;
            _path.Remove(nextStep);
            _nextTileInPath = nextStep.Value;
            StartMoving();
        }

        private Vector2Int GetFrontierMoveTarget(Frontier frontier)
        {
            var count = frontier.Cells.Count;
            var first = 0;
            var middle = count / 2;
            var last = count - 1;
            var dest = new Vector2Int(
                (frontier.Cells[first].Item1.x + frontier.Cells[middle].Item1.x + frontier.Cells[last].Item1.x) / 3,
                (frontier.Cells[first].Item1.y + frontier.Cells[middle].Item1.y + frontier.Cells[last].Item1.y) / 3
            );
            return _map.GetTileCenterRelativePosition(dest).Distance < MinimumMoveDistance ? frontier.Cells[middle].Item1 : dest;
        }

        private void DoMovement(LinkedList<PathStep>? path)
        {
            if (_robotController.Status == RobotStatus.Idle)
            {
                if (path != null && path.Count > 0)
                {
                    if (_robotTnfStatus != TnfStatus.AwaitRotating)
                    {
                        var nextStep = path.First;
                        _nextTileInPath = nextStep.Value;
                        path.Remove(nextStep);
                    }
                    _robotTnfStatus = TnfStatus.AwaitMoving;
                }
                else
                {
                    StartMoving();
                    if (_nextTileInPath == null)
                    {
                        _robotTnfStatus = TnfStatus.AwaitNextFrontier;
                    }
                    return;
                }
                if (_nextTileInPath != null)
                {
                    StartMoving();
                }
                else
                {
                    _robotTnfStatus = TnfStatus.AwaitNextFrontier;
                }
            }
        }

        private void StartMoving()
        {
            if (_robotController.Status != RobotStatus.Idle || _nextTileInPath == null)
            {
                return;
            }
            var relativePosition = _map.GetTileCenterRelativePosition(_nextTileInPath.Value.End);
            if (relativePosition.Distance < MinimumMoveDistance)
            {
                _nextTileInPath = null;
                return;
            }
            if (Mathf.Abs(relativePosition.RelativeAngle) <= AngleDelta)
            {
                _robotController.Move(relativePosition.Distance);
                _robotTnfStatus = TnfStatus.AwaitMoving;
            }
            else
            {
                _robotController.Rotate(relativePosition.RelativeAngle);
                _robotTnfStatus = TnfStatus.AwaitRotating;
            }
        }

        private List<Frontier> GetFrontiers()
        {
            var res = new List<Frontier>();
            var edges = new LinkedList<(Vector2Int, float)>();
            var map = _map.GetExploredTiles();

            // Find edges
            var mapAsList = map.ToList();
            foreach (var cell in mapAsList)
            {
                var isEdge = false;
                for (var x = cell.Key.x - 1; x <= cell.Key.x + 1; x++)
                {
                    for (var y = cell.Key.y - 1; y <= cell.Key.y + 1; y++)
                    {
                        var neighbour = new Vector2Int(x, y);
                        if (!map.ContainsKey(neighbour))
                        {
                            isEdge = true;
                        }
                    }
                }
                if (isEdge && cell.Value != SlamMap.SlamTileStatus.Solid)
                {
                    edges.AddFirst(cell.ToTnfCell());
                }
            }

            // build frontiers by connected cells
            var fCellCount = 0u;
            var eCellCount = edges.Count;
            while (eCellCount > fCellCount)
            {
                var q = new Queue<(Vector2Int, float)>();
                q.Enqueue(edges.First());

                var frontier = new List<(Vector2Int, float)>();
                while (q.Count > 0)
                {
                    var next = q.Dequeue();
                    edges.Remove(next);
                    frontier.Add(next);
                    fCellCount++;
                    for (var x = next.Item1.x - 1; x <= next.Item1.x + 1; x++)
                    {
                        for (var y = next.Item1.y - 1; y <= next.Item1.y + 1; y++)
                        {
                            var position = new Vector2Int(x, y);
                            if (edges.Any(e => e.Item1 == position))
                            {
                                var newCell = edges.First(c => c.Item1 == position);
                                q.Enqueue(newCell);
                                edges.Remove(newCell);
                            }
                        }
                    }
                }


                res.Add(new Frontier(frontier));
            }

            return res;
        }

        public bool IsOutOfFrontiers()
        {
            return _robotTnfStatus == TnfStatus.OutOfFrontiers;
        }

        public void SetController(Robot2DController controller)
        {
            _robotController = controller;
            _map = _robotController.SlamMap.CoarseMap;
            _robotId = _robotController.Id;
        }

        public string GetDebugInfo()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("TnfStatus: {0}\n", _robotTnfStatus);
            sb.Append("Current movement target: ");
            sb.AppendLine(_nextTileInPath != null ? _nextTileInPath.Value.End.ToString() : string.Empty);
            return sb.ToString();
        }

    }
}