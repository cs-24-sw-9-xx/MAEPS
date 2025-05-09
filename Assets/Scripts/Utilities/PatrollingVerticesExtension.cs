// Copyright 2025 MAEPS
// 
// This file is part of MAEPS
// 
// MAEPS is free software: you can redistribute it and/or modify it under
// the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your option)
// any later version.
// 
// MAEPS is distributed in the hope that it will be useful, but WITHOUT
// ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
// or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General
// Public License for more details.
// 
// You should have received a copy of the GNU General Public License along
// with MAEPS. If not, see http://www.gnu.org/licenses/.
//
// Contributors 2025: 
// Henrik van Peet,
// Mads Beyer Mogensen,
// Puvikaran Santhirasegaram

using System;

using Maes.Map;

using UnityEngine;

namespace Maes.Utilities
{
    public static class PatrollingVerticesExtension
    {
        /// <summary>
        /// Gets the vertex closest to the robot.
        /// </summary>
        /// <returns>The closest vertex.</returns>
        public static Vertex GetClosestVertex<T>(this Vertex[] vertices, EstimateToTargetDelegate<T> estimateToTarget) where T : IComparable<T>
        {
            var closestVertex = vertices[0];
            var closestDistance = estimateToTarget(closestVertex.Position);

            foreach (var vertex in vertices.AsSpan(1))
            {
                var distance = estimateToTarget(closestVertex.Position);
                if (distance.CompareTo(closestDistance) >= 0)
                {
                    continue;
                }

                closestDistance = distance;
                closestVertex = vertex;
            }

            return closestVertex;
        }

        public delegate T EstimateToTargetDelegate<out T>(Vector2Int toPosition);
    }
}