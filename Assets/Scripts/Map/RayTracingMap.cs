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

using Maes.Utilities;

using UnityEngine;

namespace Maes.Map
{
    public class RayTracingMap<TCell>
    {
        private readonly SimulationMap<TCell> _map;
        private readonly RayTracingTriangle[] _traceableTriangles;

        // The order in which edges are stored for each RayTracingTriangle
        private const int Diagonal = 0, Horizontal = 1, Vertical = 2;

        private static readonly double MaxTraceLengthPerTriangle = Math.Sqrt(2) / 4f;

        public RayTracingMap(SimulationMap<TCell> map)
        {
            _map = map;
            var totalTriangles = map.WidthInTiles * map.HeightInTiles * 8;
            var trianglesPerRow = map.WidthInTiles * 8;
            const float vertexDistance = 0.5f; // Vertices and in triangles are 0.5 tiles apart

            _traceableTriangles = new RayTracingTriangle[totalTriangles];
            for (var x = 0; x < map.WidthInTiles; x++)
            {
                for (var y = 0; y < map.HeightInTiles; y++)
                {
                    var index = x * 8 + y * trianglesPerRow;
                    AddTraceableTriangles(new Vector2(x, y) + map.ScaledOffset, vertexDistance, index,
                        trianglesPerRow);
                }
            }

            foreach (var (index, cell) in map)
            {
                _traceableTriangles[index].Cell = cell;
            }
        }

        // Adds 8 'Traceable triangles' for a given tile. These triangles contain information that allows effective 
        // raytracing. Each triangle contains a list of their neighbours' indices. Additionally it contains information
        // about the 3 lines that make up the triangle (An inclined line, a horizontal line and a vertical line
        // , in that order)
        private void AddTraceableTriangles(Vector2 bottomLeft, float vertexDistance, int index, int trianglesPerRow)
        {
            var x = bottomLeft.x;
            var y = bottomLeft.y;
            // Triangle 0
            var neighbours = new int[3];
            neighbours[Diagonal] = index + 1;
            neighbours[Horizontal] = index - trianglesPerRow + 4;
            neighbours[Vertical] = index - 5;
            _traceableTriangles[index] = new RayTracingTriangle(
                new Vector2(x, y + vertexDistance),
                new Vector2(x + vertexDistance, y),
                new Vector2(x, y),
                neighbours
            );

            // Triangle 1
            index++;
            var neighbours1 = new int[3];
            neighbours1[Diagonal] = index - 1;
            neighbours1[Horizontal] = index + 4;
            neighbours1[Vertical] = index + 1;
            _traceableTriangles[index] = new RayTracingTriangle(
                new Vector2(x + vertexDistance, y),
                new Vector2(x, y + vertexDistance),
                new Vector2(x + vertexDistance, y + vertexDistance),
                neighbours1
            );

            // Triangle 2
            index++;
            var neighbours2 = new int[3];
            neighbours2[Diagonal] = index + 1;
            neighbours2[Horizontal] = index + 4;
            neighbours2[Vertical] = index - 1;
            _traceableTriangles[index] = new RayTracingTriangle(
                new Vector2(x + vertexDistance, y),
                new Vector2(x + 2 * vertexDistance, y + vertexDistance),
                new Vector2(x + vertexDistance, y + vertexDistance),
                neighbours2
            );

            // Triangle 3
            index++;
            var neighbours3 = new int[3];
            neighbours3[Diagonal] = index - 1;
            neighbours3[Horizontal] = index - trianglesPerRow + 4;
            neighbours3[Vertical] = index + 5;
            _traceableTriangles[index] = new RayTracingTriangle(
                new Vector2(x + 2 * vertexDistance, y + vertexDistance),
                new Vector2(x + vertexDistance, y),
                new Vector2(x + 2 * vertexDistance, y),
                neighbours3
            );

            // Triangle 4
            index++;
            var neighbours4 = new int[3];
            neighbours4[Diagonal] = index + 1;
            neighbours4[Horizontal] = index + trianglesPerRow - 4;
            neighbours4[Vertical] = index - 5;
            _traceableTriangles[index] = new RayTracingTriangle(
                new Vector2(x, y + vertexDistance),
                new Vector2(x + vertexDistance, y + 2 * vertexDistance),
                new Vector2(x, y + 2 * vertexDistance),
                neighbours4
            );

            // Triangle 5
            index++;
            var neighbours5 = new int[3];
            neighbours5[Diagonal] = index - 1;
            neighbours5[Horizontal] = index - 4;
            neighbours5[Vertical] = index + 1;
            _traceableTriangles[index] = new RayTracingTriangle(
                new Vector2(x + vertexDistance, y + 2 * vertexDistance),
                new Vector2(x, y + vertexDistance),
                new Vector2(x + vertexDistance, y + vertexDistance),
                neighbours5
            );

            // Triangle 6
            index++;
            var neighbours6 = new int[3];
            neighbours6[Diagonal] = index + 1;
            neighbours6[Horizontal] = index - 4;
            neighbours6[Vertical] = index - 1;
            _traceableTriangles[index] = new RayTracingTriangle(
                new Vector2(x + vertexDistance, y + 2 * vertexDistance),
                new Vector2(x + 2 * vertexDistance, y + vertexDistance),
                new Vector2(x + vertexDistance, y + vertexDistance),
                neighbours6
            );

            // Triangle 7
            index++;
            var neighbours7 = new int[3];
            neighbours7[Diagonal] = index - 1;
            neighbours7[Horizontal] = index + trianglesPerRow - 4;
            neighbours7[Vertical] = index + 5;
            _traceableTriangles[index] = new RayTracingTriangle(
                new Vector2(x + 2 * vertexDistance, y + vertexDistance),
                new Vector2(x + vertexDistance, y + 2 * vertexDistance),
                new Vector2(x + 2 * vertexDistance, y + 2 * vertexDistance),
                neighbours7
            );
        }

        private struct TriangleTrace
        {
            public int EnteringEdge;
            public int NextTriangleIndex;

            public TriangleTrace(int enteringEdge, int nextTriangleIndex)
            {
                EnteringEdge = enteringEdge;
                NextTriangleIndex = nextTriangleIndex;
            }
        }


        public delegate bool CellFunction(int index, TCell cell);

        // Casts a trace starting at given point, moving in the given direction. The given function will be called on
        // each cell that is encountered. If the function returns true the trace will continue to the next cell,
        // if it returns false the trace will terminate. The trace automatically terminates when it exits map bounds.
        public void Raytrace(Vector2 startingPoint, float angleDegrees, float distance,
            CellFunction shouldContinueFromCell)
        {
#if DEBUG
            if (angleDegrees < 0f || angleDegrees > 360f)
            {
                throw new ArgumentException($"Given angle must be between 0-360 degrees. Angle was: {angleDegrees}");
            }
#endif

            var startingIndex = _map.GetTriangleIndex(startingPoint);

            // Convert given angle and starting point to a linear equation: ax + b
            var a = Mathf.Tan(Mathf.PI / 180 * angleDegrees);
            var b = startingPoint.y - a * startingPoint.x;

            var triangle = _traceableTriangles[startingIndex];
            var enteringEdge = triangle.FindInitialEnteringEdge(angleDegrees, a, b);
            var traceCount = 1;
            var trace = new TriangleTrace(enteringEdge, startingIndex);

            // If a trace travels diagonally in the bottom half of a tile, it will cross at least 4 tiles
            var minimumTracesBeforeDistanceCheck = (int)(distance / MaxTraceLengthPerTriangle);
            var maxTraces = distance * 8;
            while (true)
            {
                if (traceCount > maxTraces)
                { // Safety measure for avoiding infinite loops 
                    Debug.LogError($"Equation: {a}x + {b}");
                    throw new Exception($"INFINITE LOOP: {startingPoint.x}, {startingPoint.y}. Distance: {distance}");
                }

                // Invoke the given function on the cell, and only continue if it returns true
                if (!shouldContinueFromCell(trace.NextTriangleIndex, triangle.Cell))
                {
                    break;
                }

                // Perform the ray tracing step for the current triangle
                triangle.RayTrace(ref trace, angleDegrees, a, b);
                traceCount++;

                // Break if the next triangle is outside the map bounds
                if (trace.NextTriangleIndex < 0 || trace.NextTriangleIndex >= _traceableTriangles.Length)
                {
                    break;
                }

                triangle = _traceableTriangles[trace.NextTriangleIndex];

                // Optimization - Only start performance distance checks once we have performed a certain amount of traces
                if (traceCount >= minimumTracesBeforeDistanceCheck)
                {
                    // All vertices of the triangle must be within range for the triangle to be considered visible
                    var withinRange = Vector2.Distance(startingPoint, triangle.Lines[0].Start) <= distance;
                    withinRange &= Vector2.Distance(startingPoint, triangle.Lines[0].End) <= distance;
                    withinRange &= Vector2.Distance(startingPoint, triangle.Lines[1].End) <= distance;
                    if (!withinRange)
                    {
                        break;
                    }
                }
            }
        }


        // Secondary RayTracing function for finding intersection with first cell that causes the given CellFunction to
        // return false. This function returns the intersection point in world space, and the angle in degrees of the
        // intersecting line (relative to the x-axis) 
        public (Vector2, float)? FindIntersection(Vector2 startingPoint, float angleDegrees, float distance, CellFunction shouldContinue)
        {
#if DEBUG
            if (angleDegrees < 0f || angleDegrees > 360f)
            {
                throw new ArgumentException($"Given angle must be range 0-360 degrees. Angle was: {angleDegrees}");
            }
#endif

            var startingIndex = _map.GetTriangleIndex(startingPoint);

            // Convert given angle and starting point to a linear equation: ax + b
            var a = Mathf.Tan(Mathf.PI / 180 * angleDegrees);

            // TODO: Temp fix for 90 and 270 degree angles. Should be replaced with special case logic.
            if (Math.Abs(angleDegrees - 90f) < 0.01f)
            {
                a = 99.9f;
            }
            else if (Math.Abs(angleDegrees - 270f) < 0.01f)
            {
                a = -99.9f;
            }

            var b = startingPoint.y - a * startingPoint.x;

            var triangle = _traceableTriangles[startingIndex];
            var enteringEdge = triangle.FindInitialEnteringEdge(angleDegrees, a, b);
            var traceCount = 1;
            var trace = new TriangleTrace(enteringEdge, startingIndex);

            // If a trace travels diagonally in the bottom half of a tile, it will cross at least 4 tiles
            var minimumTracesBeforeDistanceCheck = (int)(distance / MaxTraceLengthPerTriangle);
            while (true)
            {
                if (traceCount > 1500)
                { // Safety measure for avoiding infinite loops 
                    Debug.Log($"Equation: {a}x + {b}");
                    throw new Exception($"INFINITE LOOP: {startingPoint.x}, {startingPoint.y}");
                }

                // Invoke the given function on the cell, and return the current intersection if it returns true
                if (!shouldContinue(trace.NextTriangleIndex, triangle.Cell))
                {
                    // Find intersection point
                    var intersection = triangle.Lines[trace.EnteringEdge].GetIntersection(a, b)!.Value;
                    // Find the angle of the intersecting line
                    var intersectingLineAngle = triangle.GetLineAngle(trace.EnteringEdge);
                    return (intersection, intersectingLineAngle);
                }

                // Perform the ray tracing step for the current triangle
                triangle.RayTrace(ref trace, angleDegrees, a, b);
                traceCount++;

                // Break if the next triangle is outside the map bounds
                if (trace.NextTriangleIndex < 0 || trace.NextTriangleIndex >= _traceableTriangles.Length)
                {
                    break;
                }

                triangle = _traceableTriangles[trace.NextTriangleIndex];

                // Optimization - Only start performance distance checks once we have performed a certain amount of traces
                if (traceCount >= minimumTracesBeforeDistanceCheck)
                {
                    // All vertices of the triangle must be within range for the triangle to be considered visible
                    var withinRange = Vector2.Distance(startingPoint, triangle.Lines[0].Start) <= distance;
                    withinRange &= Vector2.Distance(startingPoint, triangle.Lines[0].End) <= distance;
                    withinRange &= Vector2.Distance(startingPoint, triangle.Lines[1].End) <= distance;
                    if (!withinRange)
                    {
                        break;
                    }
                }
            }

            return null;
        }

        private struct RayTracingTriangle
        {
            public readonly Line2D[] Lines;
            private readonly int[] _neighbourIndex;
            public TCell Cell;

            public RayTracingTriangle(Vector2 p1, Vector2 p2, Vector2 p3, int[] neighbourIndex)
            {
                Lines = new[] { new Line2D(p1, p2), new Line2D(p2, p3), new Line2D(p3, p1) };
                _neighbourIndex = neighbourIndex;
                Cell = default!;
            }

            // Returns the side at which the trace exited the triangle, the exit intersection point
            // and the index of the triangle that the trace enters next
            // Takes the edge that this tile was entered from, and the linear equation ax+b for the trace 
            public void RayTrace(ref TriangleTrace trace, float angle, float a, float b)
            {
                // Variable for storing an intersection and the corresponding edge
                Vector2? intersection = null;
                var intersectionEdge = -1;
                for (var edge = 0; edge < 3; edge++)
                {
                    // The line must exit the triangle in one of the two edges that the line did not enter through
                    // Therefore only check intersection for these two lines
                    if (edge == trace.EnteringEdge)
                    {
                        continue;
                    }

                    // Find the intersection for the current edge
                    var currentIntersection = Lines[edge].GetIntersection(a, b);

                    if (currentIntersection == null) // No intersection with this edge
                    {
                        // If an intersection was found with a previously checked edge, then just use that
                        if (intersection != null)
                        {
                            break;
                        }

                        // Otherwise, since there is no intersection on this line, it has to be the other one (that isn't the entering edge)
                        // If the entering edge is 2, the next edge must be 1
                        // Otherwise the next edge can only be 2 (the cases where enter edge is 0 or 1)
                        intersectionEdge = trace.EnteringEdge == 2 ? 1 : 2;
                        break;
                    }
                    else // There is an intersection for this edge
                    {
                        // If there is no previous intersection, just use this one
                        if (intersection == null)
                        {
                            intersection = currentIntersection;
                            intersectionEdge = edge;
                        }
                        // Otherwise, if there is another conflicting intersection,
                        // then choose the highest one if angle is between 0-180 otherwise choose the lowest one.
                        // This is a conflict resolution measure to avoid infinite loops.
                        else if (angle >= 0 && angle <= 180)
                        {
                            var currentIntersectionValue = currentIntersection.Value;
                            var intersectionValue = intersection.Value;
                            if (currentIntersectionValue.y > intersectionValue.y)
                            {
                                intersection = currentIntersectionValue;
                                intersectionEdge = edge;
                            }
                            else if (Mathf.Abs(currentIntersectionValue.y - intersectionValue.y) < 0.0001f)
                            {
                                // If the y-axis is the same then choose by x-axis instead
                                if (angle < 90 && currentIntersectionValue.x > intersectionValue.x)
                                {
                                    // For 0-90 degrees prefer intersection with highest x value
                                    intersection = currentIntersectionValue;
                                    intersectionEdge = edge;
                                }
                                else if (angle > 90 && currentIntersectionValue.x < intersectionValue.x)
                                {
                                    // For 90-180 degrees prefer intersection with lowest x value
                                    intersection = currentIntersectionValue;
                                    intersectionEdge = edge;
                                }
                            }
                        }
                        else
                        {
                            var currentIntersectionValue = currentIntersection.Value;
                            var intersectionValue = intersection.Value;
                            // For 180-360 degrees prefer intersection with lowest y-value
                            if (currentIntersectionValue.y < intersectionValue.y)
                            {
                                intersection = currentIntersectionValue;
                                intersectionEdge = edge;
                            }
                            else if (Mathf.Abs(currentIntersectionValue.y - intersectionValue.y) < 0.0001f)
                            {
                                // If the y-axis is the same choose by x-axis instead
                                if (angle < 270 && currentIntersectionValue.x < intersectionValue.x)
                                {
                                    // For 180-270 degrees prefer intersection with highest x value
                                    intersection = currentIntersectionValue;
                                    intersectionEdge = edge;
                                }
                                else if (angle > 270 && currentIntersectionValue.x > intersectionValue.x)
                                {
                                    // For 270-360 degrees prefer intersection with lowest x value
                                    intersection = currentIntersectionValue;
                                    intersectionEdge = edge;
                                }
                            }
                        }
                    }
                }

                if (intersectionEdge != -1)
                {
                    // Modify out parameter (Slight performance increase over returning a value)
                    trace.EnteringEdge = intersectionEdge;
                    trace.NextTriangleIndex = _neighbourIndex[intersectionEdge];
                    return;
                }

                throw new Exception("Triangle does not have any intersections with the given line");
            }


            // When starting a ray trace, it must be determined which of the 3 edges are to be considered to the
            // initial "entering" edge
            public unsafe int FindInitialEnteringEdge(float direction, float a, float b)
            {
                var intersectionsAndEdge = (Span<(Vector2, int)>)stackalloc (Vector2, int)[3];
                var i = 0;
                for (var edge = 0; edge < 3; edge++)
                {
                    var intersection = Lines[edge].GetIntersection(a, b);
                    if (intersection != null)
                    {
                        intersectionsAndEdge[i++] = (intersection.Value, edge);
                    }
                }

                if (direction <= 90 || direction >= 270)
                {
                    // Entering point must be the left most intersection
                    var currentMin = intersectionsAndEdge[0];
                    for (var i1 = 1; i1 < i; i1++)
                    {
                        if (intersectionsAndEdge[i1].Item1.x < currentMin.Item1.x)
                        {
                            currentMin = intersectionsAndEdge[i1];
                        }
                    }

                    return currentMin
                        .Item2;
                }
                else
                {
                    // Entering point must be the right most intersection
                    var currentMin = intersectionsAndEdge[0];
                    for (var i1 = 1; i1 < i; i1++)
                    {
                        if (intersectionsAndEdge[i1].Item1.x > currentMin.Item1.x)
                        {
                            currentMin = intersectionsAndEdge[i1];
                        }
                    }

                    return currentMin
                        .Item2;
                }
            }

            public float GetLineAngle(int lineIndex)
            {
                return lineIndex switch
                {
                    Diagonal when Lines[lineIndex].IsGrowing() => 45,
                    Diagonal => -45,
                    Horizontal => 0,
                    _ => 90
                };
            }
        }
    }
}