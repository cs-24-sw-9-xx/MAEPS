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
using System.Runtime.CompilerServices;

using UnityEngine;

namespace Maes.Utilities
{

    // Represents the 8 directions found on a compass 
    public readonly struct CardinalDirection : IEquatable<CardinalDirection>
    {
        // Index representing 8 neighbouring tags/tiles
        public static readonly CardinalDirection
            East = new(0),
            SouthEast = new(1),
            South = new(2),
            SouthWest = new(3),
            West = new(4),
            NorthWest = new(5),
            North = new(6),
            NorthEast = new(7);

        public static readonly CardinalDirection[] CardinalDirections = { East, South, West, North };

        public enum RelativeDirection
        {
            // Each relative direction is assign to the corresponding compass offset
            Front = 0,
            FrontRight = 1, FrontLeft = -1,
            Left = -2,
            Right = 2,
            RearRight = 3, RearLeft = -3,
            Rear = 4
        }

        public static readonly CardinalDirection[] CardinalAndOrdinalDirections =
            {East, SouthEast, South, SouthWest, West, NorthWest, North, NorthEast};

        private readonly int _index;
        public readonly Vector2Int Vector;

        // Can only be constructed locally. Must be accessed through public static instances
        private CardinalDirection(int index)
        {
            _index = index;
            Vector = CalculateDirectionVector(index);
        }

        public CardinalDirection OppositeDirection()
        {
            return GetDirection((_index + 4) % 8);
        }

        public float DirectionToAngle()
        {
            return ((8 - _index) % 8) * 45;
        }

        public bool IsDiagonal()
        {
            return _index % 2 != 0;
        }

        // Converts the given absolute angle (relative to the x-axis) to the closest corresponding cardinal direction
        public static CardinalDirection DirectionFromDegrees(float degrees)
        {
#if DEBUG
            if (degrees < 0f)
            {
                throw new ArgumentException($"Degrees must be above zero, was: {degrees}");
            }
#endif

            var offset = (int)(((degrees + 22.5f) % 360) / 45f);
            return CardinalAndOrdinalDirections[(8 - offset) % 8];
        }

        private static CardinalDirection GetDirection(int index)
        {
            while (index < 0)
            {
                index += 8;
            }

            return CardinalAndOrdinalDirections[index % 8];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CardinalDirection Next()
        {
            return GetDirection(_index + 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CardinalDirection Previous()
        {
            return GetDirection(_index - 1);
        }

        private static Vector2Int CalculateDirectionVector(int index)
        {
            var xDir = 0;
            var yDir = 0;

            if (index > 6 || (index < 2 && index >= 0))
            {
                xDir = 1;
            }
            else if (index < 6 && index > 2)
            {
                xDir = -1;
            }

            if (index > 4)
            {
                yDir = 1;
            }
            else if (index < 4 && index > 0)
            {
                yDir = -1;
            }

            return new Vector2Int(xDir, yDir);
        }

        public CardinalDirection DirectionFromRelativeDirection(RelativeDirection dir)
        {
            return GetDirection(_index + (int)dir);
        }

        public static CardinalDirection FromVector(Vector2Int vector)
        {
            foreach (var direction in CardinalAndOrdinalDirections)
            {
                if (direction.Vector == vector)
                {
                    return direction;
                }
            }

            throw new InvalidOperationException("Could not find cardinal direction from vector");
        }

        public static CardinalDirection VectorToDirection(Vector2 vector)
        {
            if (vector == Vector2.zero)
            {
                return new CardinalDirection(-1);
            }
            return AngleToDirection(vector.GetAngleRelativeToX());
        }

        public static CardinalDirection AngleToDirection(float angle)
        {
            return FromVector(Vector2Int.RoundToInt(new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad))));
        }

        public CardinalDirection Counterclockwise()
        {
            return _index switch
            {
                //East
                0 => North,
                //Southeast
                1 => NorthEast,
                //South
                2 => East,
                //Southwest
                3 => SouthEast,
                //West
                4 => South,
                //Northwest
                5 => SouthWest,
                //North
                6 => West,
                //Northeast
                7 => NorthWest,
                _ => new CardinalDirection(-1)
            };
        }

        public CardinalDirection Clockwise()
        {
            return Counterclockwise().OppositeDirection();
        }

        public static CardinalDirection PerpendicularDirection(Vector2 vector)
        {
            return VectorToDirection(Vector2.Perpendicular(vector));
        }

        public override string ToString()
        {
            return _index switch
            {
                0 => "East",
                1 => "Southeast",
                2 => "South",
                3 => "Southwest",
                4 => "West",
                5 => "Northwest",
                6 => "North",
                7 => "Northeast",
                _ => ""
            };
        }

        public bool Equals(CardinalDirection other)
        {
            return _index == other._index;
        }

        public override bool Equals(object? obj)
        {
            return obj is CardinalDirection other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _index;
        }

        public static bool operator ==(CardinalDirection left, CardinalDirection right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CardinalDirection left, CardinalDirection right)
        {
            return !left.Equals(right);
        }
    }
}