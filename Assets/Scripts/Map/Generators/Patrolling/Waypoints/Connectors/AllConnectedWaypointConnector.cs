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

using System.Collections.Generic;

using UnityEngine;

namespace Maes.Map.Generators.Patrolling.Waypoints.Connectors
{
    public static class AllConnectedWaypointConnector
    {
        public static Vertex[] ConnectVertices(HashSet<Vector2Int> vertexPositions)
        {
            var vertices = new Vertex[vertexPositions.Count];
            var id = 0;
            foreach (var vertexPosition in vertexPositions)
            {
                vertices[id] = new Vertex(id, vertexPosition);
                id++;
            }

            foreach (var vertex in vertices)
            {
                foreach (var neighborVertex in vertices)
                {
                    if (vertex == neighborVertex)
                    {
                        continue;
                    }

                    vertex.AddNeighbor(neighborVertex);
                }
            }

            return vertices;
        }
    }
}