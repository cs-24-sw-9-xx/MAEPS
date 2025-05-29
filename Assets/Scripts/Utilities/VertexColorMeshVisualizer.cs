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
// Contributors: Puvikaran Santhirasegaram

using UnityEngine;

namespace Maes.Utilities
{
    public static class VertexColorMeshVisualizer
    {
        public static Mesh GenerateMeshSingleColor(Color32 color)
        {
            var mesh = new Mesh();

            var vertices = new Vector3[4];
            var triangles = new int[6];

            GenerateCell(ref vertices, ref triangles, 0, 0, 0, 0, 1);

            // Assign data to the mesh
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.colors32 = new[] { color, color, color, color };
            mesh.RecalculateNormals();

            return mesh;
        }

        public static Mesh GenerateMeshMultipleColor(Color32[] colorsInput)
        {
            var mesh = new Mesh();
            var gridSize = colorsInput.Length;
            var numCells = gridSize * gridSize;

            // Each cell has 4 unique vertices
            var vertices = new Vector3[numCells * 4];
            var colors = new Color32[numCells * 4];
            var triangles = new int[numCells * 6];

            var step = 1.0f / gridSize;

            int vertIndex = 0, triIndex = 0;
            for (var y = 0; y < gridSize; y++)
            {
                var cellColor = colorsInput[y];
                for (var x = 0; x < gridSize; x++)
                {
                    GenerateCell(ref vertices, ref triangles, vertIndex, triIndex, x, y, step);

                    // Assign same color to all vertices of this face
                    colors[vertIndex] = cellColor;
                    colors[vertIndex + 1] = cellColor;
                    colors[vertIndex + 2] = cellColor;
                    colors[vertIndex + 3] = cellColor;

                    vertIndex += 4; // Move to next set of vertices
                    triIndex += 6; // Move to next set of triangles
                }
            }

            // Assign data to the mesh
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.colors32 = colors;
            mesh.RecalculateNormals();

            return mesh;
        }

        private static void GenerateCell(ref Vector3[] vertices, ref int[] triangles, int vertIndex, int triIndex, int x, int y, float step)
        {
            // Define unique vertices per face
            var x0 = (x * step) - 0.5f;
            var x1 = ((x + 1) * step) - 0.5f;
            var y0 = (y * step) - 0.5f;
            var y1 = ((y + 1) * step) - 0.5f;

            vertices[vertIndex] = new Vector3(x0, y0, 0); // Bottom-left
            vertices[vertIndex + 1] = new Vector3(x1, y0, 0); // Bottom-right
            vertices[vertIndex + 2] = new Vector3(x0, y1, 0); // Top-left
            vertices[vertIndex + 3] = new Vector3(x1, y1, 0); // Top-right

            // Define triangles
            triangles[triIndex] = vertIndex;
            triangles[triIndex + 1] = vertIndex + 2;
            triangles[triIndex + 2] = vertIndex + 3;

            triangles[triIndex + 3] = vertIndex;
            triangles[triIndex + 4] = vertIndex + 3;
            triangles[triIndex + 5] = vertIndex + 1;
        }
    }
}