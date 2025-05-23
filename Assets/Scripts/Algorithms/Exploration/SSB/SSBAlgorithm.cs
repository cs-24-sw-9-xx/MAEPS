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
using Maes.Map.PathFinding;
using Maes.Robot;
using Maes.Robot.Tasks;
using Maes.Utilities;

using UnityEngine;

using static Maes.Utilities.CardinalDirection;
using static Maes.Utilities.CardinalDirection.RelativeDirection;

namespace Maes.Algorithms.Exploration.SSB
{
    public partial class SsbAlgorithm : IExplorationAlgorithm
    {
        // Set by SetController
        private IRobotController _controller = null!;

        // Set by SetController
        private CoarseGrainedMap _map = null!;

        // The robots must reserve their starting position at the begginning of the map
        private bool _hasPerformedInitialReservation;

        private readonly SsbAlgorithm.TileReservationSystem _reservationSystem;

        private State _currentState = State.Backtracking;

        // Backtracking variables
        private Vector2Int? _backtrackTarget;
        private Queue<PathStep>? _backtrackingPath;
        private PathStep? _nextBackTrackStep;
        private readonly HashSet<Vector2Int> _backTrackingPoints = new();
        // This stores all of the bps found during the current spiralling phase
        // (to avoid sharing bps from inside the spiral)
        private readonly HashSet<Vector2Int> _bpsFoundThisSpiralPhase = new();

        // Spiraling information
        // The side that the outer wall of the spiral is on, relative to the spiraling robot
        private RelativeDirection _referenceLateralSide;
        // The opposite side of the rls. This is the side pointing toward the center of the spiral
        private RelativeDirection _oppositeLateralSide;
        // Target for next step in spiral movement
        private Vector2Int? _nextSpiralTarget;

        // Primitive time tracking mechanism
        private int _currentTick;
        // Used to perform idle waiting
        private int _ticksToWait;
        // Track amount of ticks that the robot has waited for another robot to move.
        // This is a deadlock avoidance measure
        private int _ticksWaitedInDeadlock;

        // State variable for tracking the last time that this robot requested or received
        // a list of bp's from other robots
        // This is used to avoid sending requests too often
        private int _lastBPRequestTick = -1;
        private const int MinimumTicksBetweenBpRequests = 10;
        // This variable traces whether this robot has sent its own request the previous tick
        // (to avoid duplicates when multiple robots send a request at the same time)
        private int _tickOfLastRequestSentByThisRobot = int.MinValue;

        private readonly bool debugValue = false;

        private enum State
        {
            Spiraling,
            Backtracking,
            Terminated
        }

        public SsbAlgorithm()
        {
            _reservationSystem = new SsbAlgorithm.TileReservationSystem(this);
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
                _currentTick++;

                if (debugValue)
                {
                    var count = Enumerable.Range(0, 50)
                        .SelectMany(x => Enumerable.Range(0, 50).Select(y => new Vector2Int(x, y)))
                        .Where(v => !_map.IsTileExplored(v) && _map.GetTileStatus(v) == SlamMap.SlamTileStatus.Open);
                    Debug.Log(string.Join(",", count));
                }

                // Only triggered upon initial ticks of the simulation
                if (!_hasPerformedInitialReservation)
                {
                    if (_reservationSystem.IsTileReservedByThisRobot(_map.GetCurrentTile()))
                    {
                        _hasPerformedInitialReservation = true;
                    }
                    else
                    {
                        _reservationSystem.Reserve(new HashSet<Vector2Int>() { _map.GetCurrentTile() });
                        yield return WaitForCondition.WaitForLogicTicks(1);
                        continue;
                    }
                }

                // If in wait mode 
                _ticksToWait--;
                if (_ticksToWait < 0)
                {
                    _ticksToWait = 0;
                }

                var tickCount = 0;

                if (_controller.HasCollidedSinceLastLogicTick)
                {
                    // In case of collision, perform full reset
                    _currentState = State.Backtracking;
                    _backtrackingPath = null;
                    _nextBackTrackStep = null;
                    _controller.StopCurrentTask();
                }

                ProcessIncomingCommunication();
                while (_ticksToWait <= 0 && _controller.Status == RobotStatus.Idle && _currentState != State.Terminated)
                {
                    if (_currentState == State.Backtracking)
                    {
                        PerformBackTrack();
                    }

                    if (_currentState == State.Spiraling)
                    {
                        PerformSpiraling();
                    }

                    tickCount++;
                    if (tickCount > 50)
                    {
                        throw new Exception($"Robot {RobotID()} has entered an infinite update loop");
                    }
                }

                yield return WaitForCondition.WaitForLogicTicks(1);
            }
        }

        private void ProcessIncomingCommunication()
        {
            var messages = _controller.ReceiveBroadcast();
            var ssbMessages = new List<ISsbBroadcastMessage>();
            foreach (var msg in messages)
            {
                if (msg is ISsbBroadcastMessage ssbMsg)
                {
                    CombineOrAdd(ssbMessages, ssbMsg);
                }
                else
                {
                    throw new Exception("Received message that was not of type SsbBroadcastMessage");
                }
            }

            // Now process each message
            foreach (var msg in ssbMessages)
            {
                var response = msg.Process(this);
                // If processing of the message prompts a response then broadcast it 
                if (response != null)
                {
                    _controller.Broadcast(response);
                }
            }
        }

        // Combines received messages when possible
        private void CombineOrAdd(List<ISsbBroadcastMessage> broadcastMessages, ISsbBroadcastMessage newMsg)
        {
            ISsbBroadcastMessage? overwrittenMessage = null;
            ISsbBroadcastMessage? combinedMsg = null;
            foreach (var existingMessage in broadcastMessages)
            {
                combinedMsg = existingMessage.Combine(newMsg, this);
                if (combinedMsg != null)
                {
                    overwrittenMessage = existingMessage;
                    break;
                }
            }

            // If combination was successful then replace old msg with new combined one
            if (combinedMsg != null)
            {
                broadcastMessages[broadcastMessages.IndexOf(overwrittenMessage!)] = combinedMsg;
            }
            else
            {
                // Otherwise just add the new one to the list
                broadcastMessages.Add(newMsg);
            }
        }

        // --------------  Backtracking phase  -------------------
        private void PerformBackTrack()
        {
            if (_backtrackTarget == null)
            {
                // Clear reservations to avoid blocking other robots
                _reservationSystem.ClearThisRobotsReservationsExcept(_map.GetCurrentTile());

                // Send a request to receives bps from others and start an auction
                // (if enough time has passed since last request)
                if (_currentTick - _lastBPRequestTick >= MinimumTicksBetweenBpRequests)
                {
                    BroadcastBPRequest();
                    _ticksToWait = 1;
                }

                if (_tickOfLastRequestSentByThisRobot == _currentTick - 3)
                {
                    //Debug.Log($"[{_currentTick}]No bids were won by robot {_controller.GetRobotID()} in the auction. " +
                    // $"Overriding result with best candidate from local list of bps. " +
                    // $"This may result in conflicts with other robots");
                    // Case of no response from other robots after starting request/auction
                    // or if no bps were won by this robot
                    _backtrackTarget = FindBestBackTrackingTarget();
                    // If no local backtracking points could be found either, then wait till next tick
                    if (_backtrackTarget == null)
                    {
                        _ticksToWait = 1;
                    }
                }
                else if (_tickOfLastRequestSentByThisRobot == _currentTick - 1)
                {
                    // Broadcast this robots bps now to match timing of other robots that has just received the request  
                    // Debug.Log($"[{_currentTick}] Auctioneer robot {_controller.GetRobotID()} broadcasting {_backTrackingPoints.Count} bps");
                    _controller.Broadcast(new BackTrackingPointsMessage(_controller.Id, new HashSet<Vector2Int>(_backTrackingPoints)));
                    _ticksToWait = 1;
                }
                else
                {
                    _ticksToWait = 1;
                }

                return;
            }

            // Check if backtracking has been explored another robot since it was chosen
            if (_map.IsTileExplored(_backtrackTarget.Value))
            {
                _backtrackTarget = null;
                return;
            }

            // Find path to target, if no path has been found previously
            if (_backtrackingPath == null)
            {
                var path = _map.GetPathSteps(_backtrackTarget!.Value);
                if (path == null)
                {
                    _ticksToWait = 10;
                    _backtrackTarget = null;
                    return;
                }
                _backtrackingPath ??= new Queue<PathStep>(path);
            }


            // Check if the path has been completed (ie. no more steps remain)
            if (_nextBackTrackStep == null && _backtrackingPath.Count == 0)
            {
                // Robot has reached target, ensure that we are oriented at some angle
                // that is aligned with the grid before moving on to the spiral phase
                var isAligned = EnsureCorrectOrientationForSpiraling();
                if (isAligned)
                {
                    // Ready to enter Spiraling phase. Clear back tracking information
                    StartSpiraling();
                }
                return;
            }

            if (_nextBackTrackStep == null)
            {
                // Begin next step in the path
                // First ensure that this all tiles in this step is reserved
                _nextBackTrackStep ??= _backtrackingPath!.Dequeue();
                _reservationSystem.ClearThisRobotsReservationsExcept(_map.GetCurrentTile());
                // Wait until next tick to ensure that reservations are cleared before making new ones
                _ticksToWait = 1;

                // Check if any of the tiles (except the current one) in the next part of the path have become blocked
                // (This can happen when all sub tiles tiles are revealed)
                // The first one (current tile of the robot) is skipped as the robot may be standing on a partly solid
                // tile, either because it spawned there or because it was pushed there by other robots 
                if (_nextBackTrackStep.Value.CrossedTiles.Skip(1).Any(t => _map.GetTileStatus(t, true) == SlamMap.SlamTileStatus.Solid))
                {
                    _nextBackTrackStep = null;
                    _backtrackingPath = null;
                }

                return;
            }

            // Assert that we have reserved all the tiles for the next path step
            if (!_reservationSystem.AllTilesReservedByThisRobot(_nextBackTrackStep.Value.CrossedTiles))
            {
                // If there are conflicting reservations then this path is now invalid
                if (_reservationSystem.AnyTilesReservedByOtherRobot(_nextBackTrackStep.Value.CrossedTiles))
                {
                    _backtrackingPath = null;
                    _nextBackTrackStep = null;
                    if (_ticksWaitedInDeadlock > 100)
                    {
                        // Deadlock avoidance: after waiting 10 seconds, delete target bp and find a new one
                        _ticksWaitedInDeadlock = 0;
                        _backtrackTarget = null;
                        return;
                    }
                    _ticksToWait = 10;
                    _ticksWaitedInDeadlock += 10;

                }
                else
                {
                    // otherwise try to reserve the tiles of this path step and wait for confirmation
                    _reservationSystem.Reserve(_nextBackTrackStep.Value.CrossedTiles);
                    _ticksToWait = 1;
                }
                return;
            }

            // The robot can proceed, clear deadlock counter
            _ticksWaitedInDeadlock = 0;

            // Check if the robot needs to move to reach next step target 
            var relativeTarget = _map.GetTileCenterRelativePosition(_nextBackTrackStep.Value.End);
            if (relativeTarget.Distance > 0.2f)
            {
                MoveTo(relativeTarget);
            }
            else
            {
                // This part of the back track path has been completed
                _nextBackTrackStep = null;
            }
        }

        private void StartSpiraling()
        {
            _currentState = State.Spiraling;
            _nextBackTrackStep = null;
            _backtrackingPath = null;
            _backtrackTarget = null;
            MarkTileExplored(_map.GetCurrentTile());
            DetectBacktrackingPoints();
        }

        private void BroadcastBPRequest()
        {
            // Debug.Log($"Backtracking request for backtracking points [Robot: {_controller.GetRobotID()}]");
            _lastBPRequestTick = _currentTick;
            var request = new RequestMessage(_controller.Id, this);
            _controller.Broadcast(request);
        }

        // --------------  Spiraling phase  -------------------
        private void PerformSpiraling()
        {
            _nextSpiralTarget ??= DetermineNextSpiralTarget();

            if (_nextSpiralTarget == null)
            {
                // Spiralling complete. Add all unexplored bps discovered during this phase to the global bps list
                _bpsFoundThisSpiralPhase.RemoveWhere(bp => _map.IsTileExplored(bp));
                _backTrackingPoints.UnionWith(_bpsFoundThisSpiralPhase);
                _bpsFoundThisSpiralPhase.Clear();
                // Also clear reservations made during the last phase of this spiral
                _reservationSystem.ClearThisRobotsReservationsExcept(_map.GetCurrentTile());

                _currentState = State.Backtracking;
                return;
            }

            // If the spiral target has been reserved by another robot, then forget this target and find a new one
            if (_reservationSystem.IsTileReservedByOtherRobot(_nextSpiralTarget!.Value))
            {
                _nextSpiralTarget = null;
                return;
            }

            var relativePosition = _map.GetTileCenterRelativePosition(_nextSpiralTarget!.Value);
            if (relativePosition.Distance > 0.3f)
            {
                MoveToSpiralTarget(relativePosition);
            }
            else
            {
                // Mark the current tile as explored and continue to next target
                MarkTileExplored(_nextSpiralTarget!.Value);
                _nextSpiralTarget = null;
            }
        }

        private void MoveToSpiralTarget(RelativePosition relativePosition)
        {
            if (Mathf.Abs(relativePosition.RelativeAngle) > 1f)
            {
                // Detect backtracking points before rotating
                DetectBacktrackingPoints();
                // When rotating, also clear all reservations except for the current tile.
                // This is done to avoid blocking other robots with trailing reservations from this spiral
                _reservationSystem.ClearThisRobotsReservationsExcept(_map.GetCurrentTile());
                _controller.Rotate(relativePosition.RelativeAngle);
            }
            else
            {
                // Then assert that the robot has reserved the tile before moving there
                var nextTileReserved = EnsureFutureSpiralPathReserved();
                if (!nextTileReserved)
                {
                    // Cannot move into target tile as it is not yet reserved by this robot,
                    // wait until next tick and then check again
                    _ticksToWait = 1;
                    return;
                }

                // Also detect back tracking points after reloading 
                DetectBacktrackingPoints();

                // Finally move towards the target
                _controller.Move(relativePosition.Distance);
            }
        }

        private bool EnsureFutureSpiralPathReserved()
        {
            // Returns true if the next immediate step is reserved by this robot and false if not
            // Also attempts to predict path and reserve in advance
            var predictedTiles = EstimateSpiralPathBeforeTurning(5);
            if (predictedTiles.Count == 0)
            {
                throw new Exception("Cannot reserve future spiral path because tiles straight ahead are blocked");
            }

            // Count how many tiles are already reserved
            var reservedTiles = 0;
            foreach (var futureTile in predictedTiles)
            {
                if (!_reservationSystem.IsTileReservedByThisRobot(futureTile))
                {
                    break;
                }

                reservedTiles++;
            }

            // At minimum the next two tiles should be reserved in advance (unless only one tile remains)
            var minimumReservations = Math.Min(2, predictedTiles.Count);
            if (reservedTiles >= minimumReservations)
            {
                return true;
            }

            // Otherwise attempt to reserve the next part of the path now
            var maximumReservations = 5;
            _reservationSystem.Reserve(new HashSet<Vector2Int>(predictedTiles.Take(maximumReservations)));

            // Return true if the next immediate tile in the path is reserved
            return _reservationSystem.IsTileReservedByThisRobot(predictedTiles[0]);
        }

        private List<Vector2Int> EstimateSpiralPathBeforeTurning(int maxSteps)
        {
            var currentTile = _nextSpiralTarget!.Value;
            var path = new List<Vector2Int>() { currentTile };
            var direction = DirectionFromDegrees(_map.GetApproximateGlobalDegrees());
            var rlsDir = direction.DirectionFromRelativeDirection(_referenceLateralSide);

            // The spiral continues in a straight line as long as
            // the front tile is not blocked and the rls tile is blocked 
            while (path.Count < maxSteps && !IsTileBlocked(currentTile + direction.Vector) && IsTileBlocked(currentTile + rlsDir.Vector))
            {
                currentTile += direction.Vector;
                path.Add(currentTile);
            }

            return path;
        }

        private void MarkTileExplored(Vector2Int exploredTile)
        {
            //_map.SetTileExplored(exploredTile, true);
            Debug.Assert(_map.IsTileExplored(exploredTile));
            // Remove this from possible back tracking candidates, if present
            _backTrackingPoints.Remove(exploredTile);
            _bpsFoundThisSpiralPhase.Remove(exploredTile);
        }

        // Rotates the robot such that it directly faces a non-solid tile
        private bool EnsureCorrectOrientationForSpiraling()
        {
            CardinalDirection? targetDirection = null;
            var directions = new List<CardinalDirection> { East, South, West, North };
            foreach (var direction in directions)
            {
                if (IsTileBlocked(_map.GetGlobalNeighbour(direction)))
                {
                    continue;
                }

                // If this tile is open and the left neighbour is blocked, start spiraling along the left wall 
                if (IsTileBlocked(_map.GetGlobalNeighbour(direction.DirectionFromRelativeDirection(Left))))
                {
                    _referenceLateralSide = Left;
                    _oppositeLateralSide = Right;
                    targetDirection = direction;
                    break;
                }

                // If this tile is open and the right neighbour is blocked, start spiraling along the right wall
                if (IsTileBlocked(_map.GetGlobalNeighbour(direction.DirectionFromRelativeDirection(Right))))
                {
                    _referenceLateralSide = Right;
                    _oppositeLateralSide = Left;
                    targetDirection = direction;
                    break;
                }
            }

            if (targetDirection == null)
            {
                // No surrounding tile is solid, start spiral progress from this point with the following default setup:
                targetDirection = North;
                _referenceLateralSide = Right;
                _oppositeLateralSide = Left;
            }

            // Find relative position of neighbour located in target direction
            var targetRelativePosition = _map
                .GetTileCenterRelativePosition(_map.GetGlobalNeighbour(targetDirection.Value));
            // Assert that we are pointed in the right direction
            if (Mathf.Abs(targetRelativePosition.RelativeAngle) <= 1.5f)
            {
                return true;
            }

            // otherwise rotate to face the target tile
            _controller.Rotate(targetRelativePosition.RelativeAngle);
            return false;
        }

        private bool IsSolidOrExplored(RelativeDirection direction)
        {
            return IsTileSolidOrExplored(_map.GetRelativeNeighbour(direction));
        }

        // Determine whether the tile is physically blocked or marked as explored
        private bool IsTileSolidOrExplored(Vector2Int tileCoord)
        {
            return _map.GetTileStatus(tileCoord) == SlamMap.SlamTileStatus.Solid
                || _map.IsTileExplored(tileCoord);
        }

        // Determines if it is impossible to move to the given tile in spiral mode (true if solid, explored or reserved)
        private bool IsTileBlocked(Vector2Int tileCoord)
        {
            if (_map.GetTileStatus(tileCoord) == SlamMap.SlamTileStatus.Solid)
            {
                return true; // physically blocked
            }

            // Return virtual blocked status (true if either explored or reserved by another robot)
            return _map.IsTileExplored(tileCoord) || _reservationSystem.IsTileReservedByOtherRobot(tileCoord);
        }

        private void MoveTo(RelativePosition relativePosition)
        {
            if (Mathf.Abs(relativePosition.RelativeAngle) > 1.5f)
            {
                _controller.Rotate(relativePosition.RelativeAngle);
            }
            else
            {
                _controller.Move(relativePosition.Distance);
            }
        }

        private Vector2Int? FindBestBackTrackingTarget()
        {
            // Remove all bps that have been explored since discovery
            _backTrackingPoints.RemoveWhere(IsTileBlocked);

            if (_backTrackingPoints.Count == 0)
            {
                // If no bps are within reach, then try to locate one nearby
                var nearbyBp = FindNearbyBP(_backTrackingPoints);
                if (nearbyBp != null)
                {
                    // If location was successful, then try to find target again
                    _backTrackingPoints.Add(nearbyBp.Value);
                }
                else
                {
                    return null;
                }
            }

            // Order bps by euclidean distance
            var robotPosition = _map.GetCurrentTile();
            var orderedBps = _backTrackingPoints
                .OrderBy(bp => Vector2Int.Distance(robotPosition, bp));

            // Find closest tile that has an eligible path
            foreach (var bp in orderedBps)
            {
                var path = _map.GetPathSteps(bp);
                if (path != null)
                {
                    return bp;
                }
            }

            return null;
        }

        // Looks through nearby tiles to find a bp
        private Vector2Int? FindNearbyBP(HashSet<Vector2Int> existingBPs)
        {
            var currentTile = _map.GetCurrentTile();
            if (!IsTileBlocked(currentTile) && !existingBPs.Contains(currentTile))
            {
                return currentTile;
            }

            // Try to find one
            foreach (var direction in CardinalAndOrdinalDirections)
            {
                var candidateTile = currentTile + direction.Vector;
                if (!existingBPs.Contains(candidateTile) && !IsTileBlocked(candidateTile))
                {
                    return candidateTile;
                }
            }

            return null;
        }

        // Will perform spiral movement according to the algorithm described in the paper
        // Returns true if successful, and false if no open tiles are available 
        private Vector2Int? DetermineNextSpiralTarget()
        {
            var front = _map.GetRelativeNeighbour(Front);
            var rls = _map.GetRelativeNeighbour(_referenceLateralSide);
            var ols = _map.GetRelativeNeighbour(_oppositeLateralSide);

            var frontTileBlocked = IsTileBlocked(front);
            var rlsBlocked = IsTileBlocked(rls);
            var olsBlocked = IsTileBlocked(ols);

            if (frontTileBlocked && rlsBlocked && olsBlocked)
            {
                // Special cases where spiraling is blocked by reserved tiled from backtracking robots,
                // but not explored or solid. In this case we should always add these points to the bp candidates as
                // the other robot may not mark them as explored
                if (!IsTileSolidOrExplored(front))
                {
                    _backTrackingPoints.Add(front);
                }

                if (!IsTileSolidOrExplored(rls))
                {
                    _backTrackingPoints.Add(rls);
                }

                if (!IsTileSolidOrExplored(ols))
                {
                    _backTrackingPoints.Add(ols);
                }

                return null; // No more open tiles left. Spiraling has finished
            }

            // The following is the spiral algorithm specified by the bsa paper
            if (!rlsBlocked)
            {
                return _map.GetRelativeNeighbour(_referenceLateralSide);
            }

            if (!IsTileSolidOrExplored(rls))
            {
                _backTrackingPoints.Add(rls);
            }

            if (frontTileBlocked)
            {
                if (!IsTileSolidOrExplored(front))
                {
                    _backTrackingPoints.Add(front);
                }

                return _map.GetRelativeNeighbour(_oppositeLateralSide);
            }

            if (!IsTileSolidOrExplored(ols))
            {
                _backTrackingPoints.Add(ols);
            }

            return _map.GetRelativeNeighbour(Front);
        }

        private RelativeDirection? GetSpiralTargetDirection(bool frontBlocked, bool rlsBlocked, bool olsBlocked)
        {
            if (frontBlocked && rlsBlocked && olsBlocked)
            {
                return null; // No more open tiles left. Spiraling has finished
            }

            // The following is the spiral algorithm specified by the bsa paper
            if (!rlsBlocked)
            {
                return _referenceLateralSide;
            }

            if (frontBlocked)
            {
                return _oppositeLateralSide;
            }

            return Front;
        }

        // Adds backtracking points as specified by the algorithm presented in the SSB paper
        // In the SSB variation of the BP identification, a tile is only considered to be a BP if
        // it has solid tile to its immediate right or left
        private void DetectBacktrackingPoints()
        {
            if (!IsSolidOrExplored(Right))
            {
                if (IsSolidOrExplored(Front) || IsSolidOrExplored(FrontRight) || IsSolidOrExplored(RearRight))
                {
                    _backTrackingPoints.Add(_map.GetRelativeNeighbour(Right));
                }
            }

            if (!IsSolidOrExplored(Left))
            {
                if (IsSolidOrExplored(Front) || IsSolidOrExplored(FrontLeft) || IsSolidOrExplored(RearLeft))
                {
                    _backTrackingPoints.Add(_map.GetRelativeNeighbour(Left));
                }
            }
        }

        // Simulates what spiraling might look like assuming all unknown tiles are non-solid
        // Returns the amount of tiles left to traverse
        private int? SimulateSpiraling(int maxCost = 100)
        {
            if (_currentState != State.Spiraling)
            {
                throw new Exception("Illegal state. Can only call SimulateSpiraling when in spiraling mode");
            }

            // Number of tiles left to traverse
            var cost = 1;

            var simulatedExplored = new HashSet<Vector2Int>();

            var simulatedSpiralTile = _nextSpiralTarget ?? _map.GetCurrentTile();
            // Default to current tile
            var stepsSinceRotating = 0;
            var maxStepsBeforeRotating = 20;

            var simulatedDirection = DirectionFromDegrees(_map.GetApproximateGlobalDegrees());
            while (cost < maxCost)
            {
                simulatedExplored.Add(simulatedSpiralTile);
                var simulatedFront = simulatedSpiralTile + simulatedDirection.Vector;
                var simulatedRls = simulatedSpiralTile + simulatedDirection.DirectionFromRelativeDirection(_referenceLateralSide).Vector;
                var simulatedOls = simulatedSpiralTile + simulatedDirection.DirectionFromRelativeDirection(_oppositeLateralSide).Vector;

                var frontBlocked = !IsPotentiallyExplorable(simulatedFront) || simulatedExplored.Contains(simulatedFront);
                var rlsBlocked = !IsPotentiallyExplorable(simulatedRls) || simulatedExplored.Contains(simulatedRls);
                var olsBlocked = !IsPotentiallyExplorable(simulatedOls) || simulatedExplored.Contains(simulatedOls);

                var nextDirection = GetSpiralTargetDirection(frontBlocked, rlsBlocked, olsBlocked);
                if (nextDirection == null) // Spiralling successfully terminated
                {
                    return cost;
                }

                if (nextDirection == Front)
                {
                    stepsSinceRotating++;
                    // If going too far in a straight line, we have probably exited the map
                    // or the spiral is too big to simulate
                    if (stepsSinceRotating > maxStepsBeforeRotating)
                    {
                        return null;
                    }
                }
                else
                {
                    stepsSinceRotating = 0;
                }

                // Step the simulation forward to the next tile in the spiral
                simulatedDirection = simulatedDirection.DirectionFromRelativeDirection(nextDirection.Value);
                simulatedSpiralTile += simulatedDirection.Vector;
                cost++;
            }

            return null;
        }


        // Returns true unless the tile is known to be solid or if the tile is reserved by another robot
        private bool IsPotentiallyExplorable(Vector2Int tile)
        {
            return !_map.IsPotentiallyExplorable(tile) || _reservationSystem.IsTileReservedByOtherRobot(tile);
        }

        private bool IsBlocked(RelativeDirection direction)
        {
            return IsTileBlocked(_map.GetRelativeNeighbour(direction));
        }

        public void SetController(Robot2DController controller)
        {
            _controller = controller;
            _map = _controller.SlamMap.CoarseMap;
        }

        private int RobotID()
        {
            return _controller.Id;
        }

        public string GetDebugInfo()
        {
            return $"State: {Enum.GetName(typeof(State), _currentState)}" +
                   $"\nCoarse Map Position: {_map.GetApproximatePosition()}" +
                   $"\nBPs: [{string.Join(", ", _backTrackingPoints.Select(bp => bp.ToString()))}]" +
                   $"\nTemp BPs: [{string.Join(", ", _bpsFoundThisSpiralPhase.Select(bp => bp.ToString()))}]" +
                   $"\nReserved tiles: [{string.Join(", ", _reservationSystem.GetTilesReservedByThisRobot())}]" +
                   $"\nBacktracking target: {_backtrackTarget}";
        }

        // Represents a request to broadcast all available backtracking points found by this robot
        private class RequestMessage : ISsbBroadcastMessage
        {
            private readonly int _requestingRobot;

            public RequestMessage(int requestingRobot, SsbAlgorithm algorithm)
            {
                _requestingRobot = requestingRobot;
                algorithm._tickOfLastRequestSentByThisRobot = algorithm._currentTick;
            }

            public ISsbBroadcastMessage? Process(SsbAlgorithm algorithm)
            {
                algorithm._lastBPRequestTick = algorithm._currentTick;

                // Check for possible conflict
                if (algorithm._tickOfLastRequestSentByThisRobot == algorithm._currentTick - 1)
                {
                    // This case occurs when this robot has sent a bp request at the same time that another robot has
                    // sent one. Determine which one to discard based on the robots' ids
                    if (_requestingRobot < algorithm._controller.Id)
                    {
                        // Discard this request received by the other robot, as it has a lower id than this robot 
                        return null;
                    }
                    // If not discarded then continue and create a response for this request
                }

                // Remove backtracking points that have been explored since they were added to the list 
                algorithm._backTrackingPoints.RemoveWhere(bp => algorithm._map.IsTileExplored(bp));
                // Debug.Log($"Robot {algorithm._controller.GetRobotID()} received bp request. Broadcasting {algorithm._backTrackingPoints.Count} bps");

                if (algorithm._backTrackingPoints.Count == 0)
                {
                    return null; // No BPs to share
                }

                // Respond to the request by sending all bps found by this robot
                return new BackTrackingPointsMessage(_requestingRobot, new HashSet<Vector2Int>(algorithm._backTrackingPoints));
            }

            public ISsbBroadcastMessage? Combine(ISsbBroadcastMessage other, SsbAlgorithm _)
            {
                // In case of multiple simultaneous request messages use
                // one the one issued by the robot with the highest id 
                if (other is RequestMessage otherRequestMsg)
                {
                    return otherRequestMsg._requestingRobot > _requestingRobot ? otherRequestMsg : this;
                }

                return null;
            }
        }

        // Represents a message containing all known unexplored bps for an agent
        private class BackTrackingPointsMessage : ISsbBroadcastMessage
        {

            // The id of the robot that sent the original request for BPs
            public readonly int RequestingRobot;

            public readonly HashSet<Vector2Int> BackTrackingPoints;

            public BackTrackingPointsMessage(int requestingRobot, HashSet<Vector2Int> backTrackingPoints)
            {
                BackTrackingPoints = backTrackingPoints;
                RequestingRobot = requestingRobot;
            }

            // Generates a bid for each bp currently in its list
            public ISsbBroadcastMessage? Process(SsbAlgorithm algorithm)
            {
                // Add potentially unknown bps from other robots
                algorithm._backTrackingPoints.UnionWith(BackTrackingPoints);
                algorithm._backTrackingPoints.RemoveWhere(bp => algorithm._map.IsTileExplored(bp));

                // Check for possible conflict
                if (algorithm._tickOfLastRequestSentByThisRobot == algorithm._currentTick - 2)
                {
                    // This case occurs when this robot has sent a bp request at the same time that another robot has
                    // sent one. Determine which one to discard based on the robots' ids
                    if (RequestingRobot < algorithm._controller.Id)
                    {
                        // Discard this request received by the other robot, as it has a lower id than this robot 
                        return null;
                    }
                    // If not discarded then continue and create a response for this request
                }

                // If this robot the one that requested the BPs then do not respond to this message
                if (RequestingRobot == algorithm._controller.Id)
                {
                    return null;
                }

                var bids = algorithm.GenerateBids(algorithm._backTrackingPoints);

                // Debug.Log($"[{algorithm._currentTick}] Robot {algorithm._controller.GetRobotID()} generated {bids.Count} bids");
                return new BiddingMessage(RequestingRobot, bids);
            }

            public ISsbBroadcastMessage? Combine(ISsbBroadcastMessage other, SsbAlgorithm _)
            {
                if (other is BackTrackingPointsMessage bpMessage)
                {
                    // Resolve potential conflict in case of multiple simultaneous auctions
                    if (bpMessage.RequestingRobot > RequestingRobot)
                    {
                        bpMessage.BackTrackingPoints.UnionWith(bpMessage.BackTrackingPoints);
                        return bpMessage;
                    }
                    else
                    {
                        BackTrackingPoints.UnionWith(bpMessage.BackTrackingPoints);
                        return this;
                    }

                }

                return null;
            }
        }

        // Generates a series of bids for each of the given backtracking points
        private List<Bid> GenerateBids(HashSet<Vector2Int> backTrackingPoints)
        {
            var bids = new List<Bid>();

            int? spiralFinishCost = 0;
            if (_currentState == State.Spiraling)
            {
                spiralFinishCost = SimulateSpiraling();
            }

            // Spiraling could not be fully simulated - No bids
            if (spiralFinishCost == null)
            {
                return bids;
            }

            var robotPosInt = _map.GetCurrentTile();

            // Generate a bid based on the length of the calculated path to reach the bp (if present)
            foreach (var bp in backTrackingPoints)
            {
                var distanceCost = Geometry.ManhattanDistance(robotPosInt, bp);
                bids.Add(new Bid(bp, spiralFinishCost.Value + distanceCost, _controller.Id));
                // Debug.Log($"Robot: {_controller.GetRobotID()} added bid of cost {spiralFinishCost.Value + pathLength} for tile {bp}");
            }

            return bids;
        }

        private float GetRobotPathLength(List<Vector2Int> path)
        {
            var lastTile = _map.GetCurrentTile();
            var totalDistance = 0f;
            path.ForEach(tile =>
            {
                totalDistance += Vector2Int.Distance(lastTile, tile);
                lastTile = tile;
            });
            return totalDistance;
        }

        private readonly struct Bid : IEquatable<Bid>
        {

            public readonly Vector2Int BP;
            // Lower cost is better
            public readonly float Cost;
            public readonly int RobotId;

            public Bid(Vector2Int bp, float cost, int robotId)
            {
                BP = bp;
                Cost = cost;
                RobotId = robotId;
            }

            public bool Equals(Bid other)
            {
                return BP.Equals(other.BP) && Cost.Equals(other.Cost) && RobotId == other.RobotId;
            }

            public override bool Equals(object? obj)
            {
                return obj is Bid other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(BP, Cost, RobotId);
            }

            public static bool operator ==(Bid left, Bid right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(Bid left, Bid right)
            {
                return !left.Equals(right);
            }
        }

        // Represents a series of bids (cost estimation) for each known bp 
        private class BiddingMessage : ISsbBroadcastMessage
        {
            private readonly int _requestingRobot;
            private readonly Dictionary<Vector2Int, List<Bid>> _allBids = new();

            public BiddingMessage(int requestingRobot, List<Bid> robotBids)
            {
                _requestingRobot = requestingRobot;
                foreach (var robotBid in robotBids)
                {
                    _allBids.Add(robotBid.BP, new List<Bid>() { robotBid });
                }
            }

            // Process bids by calculating winner through auction
            public ISsbBroadcastMessage? Process(SsbAlgorithm algorithm)
            {
                // This message is ignored if this robot was not the one to start the auction
                if (_requestingRobot != algorithm._controller.Id)
                {
                    return null;
                }

                //Debug.Log($"[{algorithm._currentTick}] Auction processed by robot: {algorithm._controller.GetRobotID()}");

                // Add bids from this robot to the auction
                foreach (var ownBid in algorithm.GenerateBids(algorithm._backTrackingPoints))
                {
                    if (!_allBids.ContainsKey(ownBid.BP))
                    {
                        _allBids.Add(ownBid.BP, new List<Bid>());
                    }

                    _allBids[ownBid.BP].Add(ownBid);
                }

                // Sort each entry by cost (ascending) 
                foreach (var entry in _allBids)
                {
                    entry.Value.Sort((bid1, bid2) => bid1.Cost.CompareTo(bid2.Cost));
                }

                // Each robot can only win ONE bid so best bids are stored in a dictionary
                var bestBids = new Dictionary<int, Bid>();

                // Map dictionary values to queues
                var bidQueues = _allBids.ToDictionary(kvp => kvp.Key, kvp => new Queue<Bid>(kvp.Value));

                var fixedPointReached = false;
                while (!fixedPointReached)
                {
                    fixedPointReached = true;

                    foreach (var entry in bidQueues)
                    {
                        if (entry.Value.Count == 0)
                        {
                            continue; // No more bids left for this tile
                        }

                        var newBid = entry.Value.Peek();
                        if (!bestBids.TryAdd(newBid.RobotId, newBid))
                        {
                            // Skip if this is already registered as the current best bid for this robot
                            if (bestBids[newBid.RobotId] == entry.Value.Peek())
                            {
                                continue;
                            }

                            fixedPointReached = false;
                            if (newBid.Cost < bestBids[newBid.RobotId].Cost)
                            {
                                // Dequeue previous best value to allow bids from other robot on that tile 
                                bidQueues[bestBids[newBid.RobotId].BP].Dequeue();
                                // Register this as the new best bid for this robot
                                bestBids[newBid.RobotId] = newBid;
                            }
                            else
                            {
                                // Dequeue this new bid, as there exists a better candidate for this robot
                                entry.Value.Dequeue();
                            }
                        }
                        else
                        {
                            fixedPointReached = false;
                        }
                    }
                }

                //Debug.Log($"[{algorithm._currentTick}] Auction results: {bestBids.Aggregate("", (e1, e2) => e1 + $"[Robot {e2.Key} won {e2.Value.BP}]")}");

                // Create message containing results
                var resultsMessage = new AuctionResultsMessage(bestBids);
                // Process results for this robot, as it will not receive the message next tick
                resultsMessage.Process(algorithm);
                // Finally broadcast result to all other robots
                return resultsMessage;
            }

            public ISsbBroadcastMessage? Combine(ISsbBroadcastMessage other, SsbAlgorithm algorithm)
            {
                if (other is BiddingMessage bidMessage)
                {
                    // This message can be ignored if this robot was not the one to start the auction
                    if (_requestingRobot != algorithm._controller.Id)
                    {
                        return this;
                    }

                    // Adds all bids from the other message into this one
                    foreach (var entry in bidMessage._allBids)
                    {
                        if (!_allBids.ContainsKey(entry.Key))
                        {
                            _allBids.Add(entry.Key, new List<Bid>());
                        }

                        _allBids[entry.Key].AddRange(entry.Value);
                    }

                    return this;
                }
                return null;
            }
        }


        // Represents a broadcasted message containing the winning bids
        private class AuctionResultsMessage : ISsbBroadcastMessage
        {

            private readonly Dictionary<int, Bid> _results;

            public AuctionResultsMessage(Dictionary<int, Bid> results)
            {
                _results = results;
            }

            public ISsbBroadcastMessage? Process(SsbAlgorithm algorithm)
            {
                var robot = algorithm._controller.Id;
                if (_results.TryGetValue(robot, out var result))
                {
                    // Debug.Log($"Auction resulted in reservation of bp {Results[robot].BP} for robot {robot}");
                    algorithm._backtrackTarget = result.BP;
                }
                else
                {
                    // Debug.Log($"Auction resulted in no bp for robot {robot}");
                    algorithm._backtrackTarget = null;
                }

                // No further messages needed
                return null;
            }

            public ISsbBroadcastMessage? Combine(ISsbBroadcastMessage other, SsbAlgorithm _)
            {
                // There should never be more than one auction result message at a time
                if (other is AuctionResultsMessage)
                {
                    throw new Exception("Illegal state. Multiple auction results received simultaneously");
                }

                return null;
            }
        }
    }
}