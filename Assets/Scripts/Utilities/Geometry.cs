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

using UnityEngine;

namespace Maes.Utilities
{
    public static class Geometry
    {
        public static Vector2Int FromRosCoord(Vector2Int rosPosition)
        {
            return new Vector2Int(-rosPosition.x, -rosPosition.y);
        }

        public static Vector2 FromRosCoord(Vector3 rosPosition)
        {
            return new Vector2(-rosPosition.x, -rosPosition.y);
        }

        public static Vector2 ToRosCoord(Vector3 mapPosition)
        {
            // Map position is robots position in the tile grid. NOT world position / game object position
            return new Vector2(-mapPosition.x, -mapPosition.y);
        }

        public static bool IsPointWithinCircle(Vector2Int point, Vector2 circleStartPosition, float maxRadius)
        {
            return Mathf.Pow(point.x - circleStartPosition.x, 2) + Mathf.Pow(point.y - circleStartPosition.y, 2) < Mathf.Pow(maxRadius, 2);
        }

        /// <summary>
        /// Utilizes this inequality (x - <paramref name="circleStartPosition"/>.x)^2 + (y - <paramref name="circleStartPosition"/>.y)^2 = r^2 &lt; <paramref name="maxRadius"/>^2 <para/>
        /// Based on <see href="https://math.stackexchange.com/questions/1307832/how-to-tell-if-x-y-coordinate-is-within-a-circle">How to tell if (X,Y) coordinate is within a Circle</see>
        /// </summary>
        /// <param name="points">The points that gets checked if they are within the circle</param>
        /// <param name="circleStartPosition">The origin point of the circle</param>
        /// <param name="maxRadius">The radius in coarse tiles from the robot. R in the inequality</param>
        /// <returns>Doorway tiles around the robot within range</returns>
        public static IEnumerable<Vector2Int> PointsWithinCircle(IEnumerable<Vector2Int> points, Vector2 circleStartPosition, float maxRadius)
        {
            return points.Where(point => IsPointWithinCircle(point, circleStartPosition, maxRadius));
        }

        public static Vector2 VectorFromDegreesAndMagnitude(float angleDegrees, float magnitude)
        {
            var angleRad = angleDegrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad)) * magnitude;
        }

        public static Vector2 DirectionAsVector(float angleDegrees)
        {
            return new Vector2(Mathf.Cos(angleDegrees * Mathf.Deg2Rad), Mathf.Sin(angleDegrees * Mathf.Deg2Rad));
        }

        public static int ManhattanDistance(Vector2Int v1, Vector2Int v2)
        {
            return Math.Abs(v1.x - v2.x) + Math.Abs(v1.y - v2.y);
        }
    }
}