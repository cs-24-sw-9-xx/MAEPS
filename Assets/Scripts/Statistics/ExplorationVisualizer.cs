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

using System.Collections.Generic;
using Maes.Map;
using Maes.Visualizers;

using UnityEngine;
using UnityEngine.Rendering;

namespace Maes.Statistics
{
    public class ExplorationVisualizer : MonoBehaviour, IVisualizer<ExplorationCell>
    {
        public MeshFilter meshFilter = null!;
        public LayerMask _foglayer;
        
        private GameObject? _fogOfWarPlane;
        private Mesh? _fogMesh;
        private Vector3[]? _fogVertices;
        private Color[]? _fogColors;

        // Set in SetSimulationMap
        private Mesh _mesh = null!;

        public static readonly Color32 SolidColor = new Color32(0, 0, 0, 255);
        public static readonly Color32 ExploredColor = new Color32(32, 130, 57, 255);
        public static readonly Color32 StandardCellColor = new Color32(170, 170, 170, 255);
        public static readonly Color32 CoveredColor = new Color32(32, 80, 240, 255);
        public static readonly Color32 SlamSeenColor = new Color32(50, 120, 180, 255);
        public static readonly Color32 WarmColor = new Color32(200, 60, 60, 255);
        public static readonly Color32 ColdColor = new Color32(50, 120, 180, 255);

        private int _widthInTiles, _heightInTiles;
        private Vector3 _offset;

        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<int> _triangles = new List<int>();
        
        // Set in SetSimulationMap
        private SimulationMap<ExplorationCell> _map = null!;
        // Set in SetSimulationMap
        private Color32[] _colors = null!;

        private const int ResolutionMultiplier = 2;

        public delegate Color32 CellToColor(ExplorationCell cell);
        public delegate Color32 CellIndexToColor(int cellIndex);

        public void SetSimulationMap(SimulationMap<ExplorationCell> newMap, Vector3 offset)
        {
            _map = newMap;
            _widthInTiles = _map.WidthInTiles;
            _heightInTiles = _map.HeightInTiles;
            _offset = offset;

            // GenerateVertices();
            GenerateTriangleVertices();
            GenerateTriangles();
            _colors = new Color32[_vertices.Count];
            InitializeColors(_map);

            _mesh = new Mesh
            {
                indexFormat = IndexFormat.UInt32,
                vertices = _vertices.ToArray(),
                triangles = _triangles.ToArray(),
                colors32 = _colors
            };
            _mesh.RecalculateNormals();

            meshFilter.mesh = _mesh;

            //Fog of War related stuff below
            _fogOfWarPlane = GameObject.Find("FogPlaneBetter");
            if (_fogOfWarPlane != null)
            {
                _fogMesh = _fogOfWarPlane.GetComponent<MeshFilter>().mesh;
                _fogVertices = _fogMesh.vertices;
                _fogColors = new Color[_fogVertices.Length];
                for (int i = 0; i < _fogColors.Length; i++)
                {
                    _fogColors[i] = Color.black;
                }
                for (int i = 0; i < _fogVertices.Length; i++)
                {
                    _fogVertices[i] = _fogOfWarPlane.transform.TransformPoint(_fogVertices[i]);
                }
                UpdateFogColor();
            }
        }

        private void GenerateTriangleVertices()
        {
            _vertices.Clear();
            var vertexDistance = 1f / ResolutionMultiplier;
            for (int y = 0; y < _heightInTiles; y++)
            {
                var translatedY = y + _offset.y;
                for (int x = 0; x < _widthInTiles; x++)
                {
                    var translatedX = x + _offset.x;
                    AddTileTriangleVertices(translatedX, translatedY, vertexDistance);
                }
            }
        }

        // Adds all of the vertices needed for a tile of 8 triangles
        // The triangles are indexed and arranged as shown in this very pretty illustration:
        // |4/5|6\7|
        // |0\1|2/3|
        private void AddTileTriangleVertices(float x, float y, float vertexDistance)
        {
            // Triangle 0
            _vertices.Add(new Vector3(x, y, 0f));
            _vertices.Add(new Vector3(x, y + vertexDistance, 0f));
            _vertices.Add(new Vector3(x + vertexDistance, y, 0f));

            // Triangle 1
            _vertices.Add(new Vector3(x, y + vertexDistance));
            _vertices.Add(new Vector3(x + vertexDistance, y + vertexDistance));
            _vertices.Add(new Vector3(x + vertexDistance, y));

            // Triangle 2
            _vertices.Add(new Vector3(x + vertexDistance, y + vertexDistance));
            _vertices.Add(new Vector3(x + 2 * vertexDistance, y + vertexDistance));
            _vertices.Add(new Vector3(x + vertexDistance, y));

            // Triangle 3
            _vertices.Add(new Vector3(x + vertexDistance, y));
            _vertices.Add(new Vector3(x + 2 * vertexDistance, y + vertexDistance));
            _vertices.Add(new Vector3(x + 2 * vertexDistance, y));

            // Triangle 4
            _vertices.Add(new Vector3(x, y + 2 * vertexDistance));
            _vertices.Add(new Vector3(x + vertexDistance, y + 2 * vertexDistance));
            _vertices.Add(new Vector3(x, y + vertexDistance));

            // Triangle 5
            _vertices.Add(new Vector3(x, y + vertexDistance));
            _vertices.Add(new Vector3(x + vertexDistance, y + 2 * vertexDistance));
            _vertices.Add(new Vector3(x + vertexDistance, y + vertexDistance));

            // Triangle 6
            _vertices.Add(new Vector3(x + vertexDistance, y + 2 * vertexDistance));
            _vertices.Add(new Vector3(x + 2 * vertexDistance, y + vertexDistance));
            _vertices.Add(new Vector3(x + vertexDistance, y + vertexDistance));

            // Triangle 7
            _vertices.Add(new Vector3(x + vertexDistance, y + 2 * vertexDistance));
            _vertices.Add(new Vector3(x + 2 * vertexDistance, y + 2 * vertexDistance));
            _vertices.Add(new Vector3(x + 2 * vertexDistance, y + vertexDistance));
        }


        private void GenerateTriangles()
        {
            _triangles.Clear();
            // The vertices are already arranged in the correct order (ie. triangle 0 has vertices indexed 0, 1, 2)
            for (int i = 0; i < _vertices.Count; i++)
                _triangles.Add(i);
        }

        // Colors each triangle depending on its current state
        public void InitializeColors(SimulationMap<ExplorationCell> newMap)
        {
            foreach (var (index, explorationCell) in newMap)
            {
                var vertexIndex = index * 3;
                var color = SolidColor;
                if (explorationCell.IsExplorable)
                    color = explorationCell.IsExplored ? ExploredColor : StandardCellColor;
                _colors[vertexIndex] = color;
                _colors[vertexIndex + 1] = color;
                _colors[vertexIndex + 2] = color;
            }
        }

        /// <summary>
        /// Updates the color of ALL triangles based on the given map and color function. This is an expensive operation
        /// and should be only called when it is necessary to replace all colors. To update a small subset of the
        /// triangles use the <see cref="UpdateColors"/> function.
        /// </summary>
        public void SetAllColors(SimulationMap<ExplorationCell> map, CellToColor cellToColor)
        {
            foreach (var (index, cell) in map)
            {
                var vertexIndex = index * 3;
                var color = cellToColor(cell);
                _colors[vertexIndex] = color;
                _colors[vertexIndex + 1] = color;
                _colors[vertexIndex + 2] = color;
            }

            _mesh.colors32 = _colors;
        }

        /// <summary>
        /// Updates the color of ALL triangles based on the given map and color function. This is an expensive operation
        /// and should be only called when it is necessary to replace all colors. To update a small subset of the
        /// triangles use the <see cref="UpdateColors"/> function.
        /// </summary>
        public void SetAllColors(SimulationMap<ExplorationCell> map, CellIndexToColor cellToColor)
        {
            foreach (var (index, _) in map)
            {
                var vertexIndex = index * 3;
                var color = cellToColor(index);
                _colors[vertexIndex] = color;
                _colors[vertexIndex + 1] = color;
                _colors[vertexIndex + 2] = color;
            }

            _mesh.colors32 = _colors;
        }

        public void SetAllColors(CellIndexToColor getColorByIndex)
        {
            foreach (var (index, _) in _map)
            {
                SetCellColor(index, getColorByIndex(index));
            }
            _mesh.colors32 = _colors;
        }

        private void SetCellColor(int triangleIndex, Color32 color)
        {
            var vertexIndex = triangleIndex * 3;
            _colors[vertexIndex] = color;
            _colors[vertexIndex + 1] = color;
            _colors[vertexIndex + 2] = color;
        }

        /// <summary>
        /// Updates the colors of the triangles corresponding to the given list of exploration cells.
        /// </summary>
        public void UpdateColors(IEnumerable<(int, ExplorationCell)> cellsWithIndices, CellToColor cellToColor)
        {
            foreach (var (index, cell) in cellsWithIndices)
            {
                var vertexIndex = index * 3;
                var color = cellToColor(cell);
                _colors[vertexIndex] = color;
                _colors[vertexIndex + 1] = color;
                _colors[vertexIndex + 2] = color;

                //Fog of War colorchange below, done for every vertex that is seen and explored
                //If turn off exploration mode, tiles dont change color, therefore dont change the FogMesh
                if (_fogMesh != null && _fogColors != null) {
                    for (int i = 0; i <= 2; i++) //The more vertices nearby you check, the more computation and the further you see, 0-2 work, above 0 is much slower
                    {
                        var ray = new Ray(_vertices[vertexIndex + i] + new Vector3(0, 0, -10), Vector3.forward);
                        if (Physics.Raycast(ray, out var hit, 1000, _foglayer, QueryTriggerInteraction.Collide))
                        {
                            int vertexIndexHit = GetClosestVertex(hit, _fogMesh.triangles);
                            _fogColors[vertexIndexHit].a = 0;
                            UpdateFogColor();
                        }
                    }
                }
            }

            _mesh.colors32 = _colors;
        }

        void UpdateFogColor()
        {
            if (_fogMesh != null)
            {
                _fogMesh.colors = _fogColors;
            }
        }

        public static int GetClosestVertex(RaycastHit aHit, int[] aTriangles)
        {
            var b = aHit.barycentricCoordinate;
            int index = aHit.triangleIndex * 3;
            if (index < 0 || index + 2 >= aTriangles.Length)
                return -1;

            if (b.x > b.y)
            {
                if (b.x > b.z)
                    return aTriangles[index]; // x
                else
                    return aTriangles[index + 2]; // z
            }
            else if (b.y > b.z)
                return aTriangles[index + 1]; // y
            else
                return aTriangles[index + 2]; // z
        }
    }
}