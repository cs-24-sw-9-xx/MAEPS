// Copyright 2024 MAES
// Copyright 2025 MAEPS
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
// Contributors: Rasmus Borrisholt Schmidt, Andreas Sebastian Sørensen, Thor Beregaard, Malte Z. Andreasen, Philip I. Holler and Magnus K. Jensen, Mads Beyer Mogensen
// 
// Original repository: https://github.com/Molitany/MAES

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

using UnityEngine;

namespace Maes.Utilities
{
    public readonly struct Line2D : IEquatable<Line2D>
    {
        public readonly Vector2 Start;
        public readonly Vector2 End;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Line2D(Vector2 start, Vector2 end)
        {
            Start = start;
            End = end;
        }

        // Returns true if the y value of the line grows as x increases
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsGrowing()
        {
            return Start.y < End.y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2? GetIntersection(Line2D otherLine, bool line2Infinite = false)
        {
            return GetIntersection(this, otherLine, line2Infinite);
        }

        // Implemented based on https://web.archive.org/web/20120311005255/https://paulbourke.net/geometry/lineline2d/
        public static Vector2? GetIntersection(Line2D line1, Line2D line2, bool line2Infinite = false)
        {
            var x1 = line1.Start.x;
            var y1 = line1.Start.y;

            var x2 = line1.End.x;
            var y2 = line1.End.y;

            var x3 = line2.Start.x;
            var y3 = line2.Start.y;

            var x4 = line2.End.x;
            var y4 = line2.End.y;

            var denominator = ((y4 - y3) * (x2 - x1) - (x4 - x3) * (y2 - y1));

            // Lines are parallel if this is 0.
            if (denominator == 0f)
            {
                return null;
            }

            var ua = ((x4 - x3) * (y1 - y3) - (y4 - y3) * (x1 - x3))
                     / denominator;

            var intersection = new Vector2(x1 + ua * (x2 - x1), y1 + ua * (y2 - y1));

            if (line2Infinite && ua is >= 0f and <= 1f)
            {
                return intersection;
            }

            var ub = ((x2 - x1) * (y1 - y3) - (y2 - y1) * (x1 - x3))
                     / denominator;


            if (ua is >= 0f and <= 1f && ub is >= 0f and <= 1f)
            {
                return intersection;
            }

            return null;
        }

        /// <summary>
        /// Gets all points on a grid that the line goes through from start to end as integer numbers
        /// </summary>
        /// <param name="granularity">The step size to create points from, defaults to 1 world unit aka. a coarse tile</param>
        /// <returns>List of points</returns>
        public IEnumerable<Vector2> Rasterize(float granularity = 1)
        {
            var minY = Mathf.Min(Start.y, End.y);
            var maxY = Mathf.Max(Start.y, End.y);

            var minX = Mathf.Min(Start.x, End.x);
            var maxX = Mathf.Max(Start.x, End.x);

            var isVertical = Mathf.Approximately(minX, maxX);

            float a;
            float b;

            if (!isVertical)
            {
                a = (End.y - Start.y) / (maxX - minX);
                b = Start.y - a * Start.x;
            }
            else
            {
                a = (End.x - Start.x) / (maxY - minY);
                b = Start.x - a * Start.y;
            }

            var start = isVertical ? Start.y : Start.x;
            var end = isVertical ? End.y : End.x;
            if (start > end)
            {
                (end, start) = (start, end);
            }

            var points = new List<Vector2>();
            for (var x = start; x <= end; x += granularity)
            {
                points.Add(isVertical ? new Vector2(SlopeIntercept(a, b, x), x) : new Vector2(x, SlopeIntercept(a, b, x)));
            }
            return points.Distinct();
        }

        public static bool operator ==(Line2D left, Line2D right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Line2D left, Line2D right)
        {
            return !(left == right);
        }

        public bool Equals(Line2D other)
        {
            return Start == other.Start && End == other.End;
        }

        public override bool Equals(object? obj)
        {
            return obj is Line2D other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Start, End);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float SlopeIntercept(float a, float b, float x)
        {
            return a * x + b;
        }
    }
}