// Copyright 2022 MAES
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
// Contributors: Malte Z. Andreasen, Philip I. Holler and Magnus K. Jensen
// 
// Original repository: https://github.com/MalteZA/MAES

using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace Maes.Algorithms.Exploration.SSB
{
    public partial class SsbAlgorithm
    {

        public readonly struct Reservation : IEquatable<Reservation>
        {
            public readonly int ReservingRobot;
            public readonly Vector2Int ReservedTile;
            public readonly int StartingTick;

            public Reservation(int reservingRobot, Vector2Int reservedTile, int startingTick)
            {
                ReservingRobot = reservingRobot;
                ReservedTile = reservedTile;
                StartingTick = startingTick;
            }

            public bool Equals(Reservation other)
            {
                return ReservingRobot == other.ReservingRobot && ReservedTile.Equals(other.ReservedTile) && StartingTick == other.StartingTick;
            }

            public override bool Equals(object? obj)
            {
                return obj is Reservation other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(ReservingRobot, ReservedTile, StartingTick);
            }

            public static bool operator ==(Reservation left, Reservation right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(Reservation left, Reservation right)
            {
                return !left.Equals(right);
            }
        }

        public class TileReservationSystem
        {

            private readonly Algorithms.Exploration.SSB.SsbAlgorithm _algorithm;
            private readonly Dictionary<Vector2Int, Reservation> _reservations = new Dictionary<Vector2Int, Reservation>();

            public TileReservationSystem(Algorithms.Exploration.SSB.SsbAlgorithm algorithm)
            {
                _algorithm = algorithm;
            }

            // Creates a reservation locally and returns it. Returns null if the reservation already exists
            private Reservation? ReserveLocally(Vector2Int tile)
            {
                // Only perform reservation if this tile is not already reserved
                if (_reservations.ContainsKey(tile))
                {
                    if (_reservations[tile].ReservingRobot != _algorithm._controller.Id)
                    {
                        throw new Exception("Attempted to reserve a tile that is already reserved by another robot");
                    }

                    return null;
                }
                else
                {
                    _reservations[tile] = new Reservation(_algorithm.RobotID(), tile, _algorithm._currentTick);
                    return _reservations[tile];
                }
            }

            // Saves and broadcasts reservations for the given set of tiles
            public void Reserve(HashSet<Vector2Int> tiles)
            {
                var newReservations = new HashSet<Reservation>();
                foreach (var tile in tiles)
                {
                    var newRes = ReserveLocally(tile);
                    if (newRes != null)
                    {
                        newReservations.Add(newRes.Value);
                    }
                }

                if (newReservations.Count == 0)
                {
                    return; // No new reservations added, so no need to 
                }

                // Broadcast reservations to all nearby robots
                _algorithm._controller.Broadcast(new ReservationMessage(new HashSet<Reservation>(
                    tiles.Select(t => new Reservation(_algorithm.RobotID(), t, _algorithm._currentTick)))
                ));
            }

            // Returns true if the robot has a confirmed reservation for the given tile
            public bool IsTileReservedByThisRobot(Vector2Int tile)
            {
                return _reservations.ContainsKey(tile)
                       && _reservations[tile].ReservingRobot == _algorithm.RobotID()
                       && _algorithm._currentTick != _reservations[tile].StartingTick;
                // The last step avoids counting reservations that were made this tick,
                // to avoid conflicts in cases where to robots reserve the same tile at the same time
            }

            // Returns true if another robot has a confirmed reservation for the given tile
            public bool IsTileReservedByOtherRobot(Vector2Int tile)
            {
                return _reservations.ContainsKey(tile)
                       && _reservations[tile].ReservingRobot != _algorithm.RobotID();
            }

            // Returns the id of the robot that is reserving the given tile
            // Returns null if no robot has reserved the tile
            public int? GetReservingRobot(Vector2Int tile)
            {
                if (!_reservations.ContainsKey(tile))
                {
                    return null;
                }

                var reservation = _reservations[tile];
                // Ignore reservations made this tick by this robot (as there could potentially be a conflict next tick)
                if (_algorithm.RobotID() == reservation.ReservingRobot && _algorithm._currentTick == reservation.StartingTick)
                {
                    return null;
                }

                return _reservations[tile].ReservingRobot;
            }


            public void RegisterReservationFromOtherRobot(Reservation newRes)
            {
                // Only register reservation if no previous reservation exists
                // or if the new reservation has a higher robot id
                var tile = newRes.ReservedTile;
                if (!_reservations.ContainsKey(tile) || _reservations[tile].ReservingRobot < newRes.ReservingRobot)
                {
                    _reservations[tile] = newRes;
                }
            }

            public void ClearThisRobotsReservationsExcept(Vector2Int exception)
            {
                var removableReservations = new HashSet<Reservation>(_reservations
                    .Where(r => r.Value.ReservingRobot == _algorithm.RobotID() && !r.Key.Equals(exception))
                    .Select(e => e.Value));

                // Only broadcast if there are any removable tiles
                if (removableReservations.Count == 0)
                {
                    return;
                }

                // Remove from local reservations list
                foreach (var reservation in removableReservations)
                {
                    _reservations.Remove(reservation.ReservedTile);
                }

                // Broadcast removal to other robots
                _algorithm._controller.Broadcast(new ReservationClearingMessage(removableReservations));
            }

            public void ClearReservation(Reservation reservation)
            {
                _reservations.Remove(reservation.ReservedTile);
            }

            public void ClearReservations(HashSet<Reservation> reservationsToClear)
            {
                foreach (var reservation in reservationsToClear)
                {
                    _reservations.Remove(reservation.ReservedTile);
                }
            }

            public List<Vector2Int> GetTilesReservedByThisRobot()
            {
                var thisRobot = _algorithm.RobotID();
                return _reservations
                    .Where(res => res.Value.ReservingRobot == thisRobot)
                    .Select(entry => entry.Key)
                    .ToList();
            }

            public HashSet<Vector2Int> GetTilesReservedByOtherRobots()
            {
                var thisRobot = _algorithm.RobotID();
                return new HashSet<Vector2Int>(_reservations
                    .Where(entry => entry.Value.ReservingRobot != thisRobot)
                    .Select(entry => entry.Key));
            }

            public bool AnyTilesReservedByOtherRobot(HashSet<Vector2Int> tiles)
            {
                var thisRobot = _algorithm.RobotID();
                foreach (var tile in tiles)
                {
                    if (_reservations.ContainsKey(tile) && _reservations[tile].ReservingRobot != thisRobot)
                    {
                        return true;
                    }
                }

                return false;
            }

            public bool AllTilesReservedByThisRobot(HashSet<Vector2Int> tiles)
            {
                var thisRobot = _algorithm.RobotID();
                foreach (var tile in tiles)
                {
                    if (!_reservations.ContainsKey(tile) || _reservations[tile].ReservingRobot != thisRobot)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        public class ReservationMessage : ISsbBroadcastMessage
        {

            private readonly HashSet<Reservation> _reservations;

            public ReservationMessage(HashSet<Reservation> reservations)
            {
                _reservations = reservations;
            }

            public ISsbBroadcastMessage? Process(Algorithms.Exploration.SSB.SsbAlgorithm algorithm)
            {
                foreach (var reservation in _reservations)
                {
                    algorithm._reservationSystem.RegisterReservationFromOtherRobot(reservation);
                }

                // Debug.Log($"Robot {algorithm.RobotID()} " +
                //           $"registered {_reservations.Count} reservations from other robots");
                return null;
            }

            public ISsbBroadcastMessage? Combine(ISsbBroadcastMessage other, Algorithms.Exploration.SSB.SsbAlgorithm algorithm)
            {
                if (other is ReservationMessage reservationMsg)
                {
                    _reservations.UnionWith(reservationMsg._reservations);
                    return this;
                }

                return null;
            }
        }

        public class ReservationClearingMessage : ISsbBroadcastMessage
        {

            private readonly HashSet<Reservation> _reservationsToClear;

            public ReservationClearingMessage(HashSet<Reservation> reservationsToClear)
            {
                _reservationsToClear = reservationsToClear;
            }

            public ISsbBroadcastMessage? Process(Algorithms.Exploration.SSB.SsbAlgorithm algorithm)
            {
                algorithm._reservationSystem.ClearReservations(_reservationsToClear);
                // Debug.Log($"Robot {algorithm.RobotID()} " +
                //           $"received and processed request to clear {_reservationsToClear} reservations");
                return null;
            }

            public ISsbBroadcastMessage? Combine(ISsbBroadcastMessage other, Algorithms.Exploration.SSB.SsbAlgorithm algorithm)
            {
                if (other is ReservationClearingMessage clearingMsg)
                {
                    _reservationsToClear.UnionWith(clearingMsg._reservationsToClear);
                    return this;
                }

                return null;
            }
        }

    }

}