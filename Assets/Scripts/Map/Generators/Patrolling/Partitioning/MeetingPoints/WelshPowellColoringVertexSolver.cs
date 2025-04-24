using System.Collections.Generic;
using System.Linq;

namespace Maes.Map.Generators.Patrolling.Partitioning.MeetingPoints
{
    public class WelshPowellColoringVertexSolver
    {
        private class Vertex
        {
            public Vertex(MeetingPoint meetingPoint)
            {
                MeetingPoint = meetingPoint;
            }

            public MeetingPoint MeetingPoint { get; }
            public int Id => MeetingPoint.VertexId;
            public int? ColorId { get; set; }
            public HashSet<Vertex> Neighbors { get; } = new();
        }

        public WelshPowellColoringVertexSolver(MeetingPoint[] meetingPoints)
        {
            _vertices = meetingPoints.Select(meetingPoint => new Vertex(meetingPoint)).ToArray();
            SetNeighbors();
        }

        private readonly Vertex[] _vertices;

        private void SetNeighbors()
        {
            foreach (var (vertex1, vertex2) in _vertices.Combinations())
            {
                if (!vertex1.MeetingPoint.RobotIds.Overlaps(vertex2.MeetingPoint.RobotIds))
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

                    if (isFeasible)
                    {
                        vertex.ColorId = color;
                        coloredVertices++;
                    }
                }
            }
        }
    }
}