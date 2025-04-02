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
        /// <remarks>
        /// TODO: This does not take walls into account so it might not pick the best waypoint.
        /// </remarks>
        /// <returns>The closest vertex.</returns>
        public static Vertex GetClosestVertex(this Vertex[] vertices, Vector2Int fromPosition)
        {
            var closestVertex = vertices[0];
            var closestDistance = Vector2Int.Distance(fromPosition, closestVertex.Position);

            foreach (var vertex in vertices.AsSpan(1))
            {
                var distance = Vector2Int.Distance(fromPosition, vertex.Position);
                if (!(distance < closestDistance))
                {
                    continue;
                }

                closestDistance = distance;
                closestVertex = vertex;
            }

            return closestVertex;
        }
    }
}