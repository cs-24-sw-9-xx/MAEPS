// Copyright 2025 MAES
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
// Contributors: 
// Henrik van Peet,
// Mads Beyer Mogensen,
// Puvikaran Santhirasegaram

using System.Collections.Generic;
using System.Linq;

namespace Maes.Map.Generators.Patrolling.Partitioning.MeetingPoints
{
    /*
     * Welsh-Powell algorithm for coloring the vertices of a graph.
     * The algorithm is inspired by from the paper:
     *      A comparative analysis between two heuristic algorithms for the graph vertex coloring problem
     *      ISSN: 2088-8708, DOI: 10.11591/ijece.v13i3.pp2981-2989
     * The algorithm is rewritten to be more readable and understandable for the purpose of this project.
     */
    public class WelshPowellMeetingPointColorer
    {
        private sealed class Vertex
        {
            public Vertex(int id, HashSet<int> meetingRobotIds)
            {
                Id = id;
                MeetingRobotIds = meetingRobotIds;
            }

            public HashSet<int> MeetingRobotIds { get; }
            public int Id { get; }
            public int? ColorId { get; set; }
            public HashSet<Vertex> Neighbors { get; } = new();
        }

        public WelshPowellMeetingPointColorer(Dictionary<int, HashSet<int>> meetingRobotIdsByVertexId)
        {
            _vertices = meetingRobotIdsByVertexId.Select(kvp => new Vertex(kvp.Key, kvp.Value)).ToArray();
            SetNeighbors();
        }

        private readonly Vertex[] _vertices;

        private void SetNeighbors()
        {
            foreach (var (vertex1, vertex2) in _vertices.Combinations())
            {
                if (!vertex1.MeetingRobotIds.Overlaps(vertex2.MeetingRobotIds))
                {
                    continue;
                }

                vertex1.Neighbors.Add(vertex2);
                vertex2.Neighbors.Add(vertex1);
            }
        }

        public Dictionary<int, int> Run()
        {
            WelshPowellAlgorithm();
            return _vertices.ToDictionary(vertex => vertex.Id, vertex => vertex.ColorId!.Value);
        }

        private void WelshPowellAlgorithm()
        {
            var coloredVertices = 0;
            var color = 0;
            while (coloredVertices < _vertices.Length)
            {
                color++;
                foreach (var vertex in _vertices)
                {
                    if (vertex.ColorId != null)
                    {
                        continue;
                    }

                    var isFeasible = vertex.Neighbors.All(neighbor => neighbor.ColorId != color);

                    if (!isFeasible)
                    {
                        continue;
                    }

                    vertex.ColorId = color;
                    coloredVertices++;
                }
            }
        }
    }
}