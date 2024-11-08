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

using UnityEngine;

namespace Maes.Statistics
{
    public class ExplorationVisualizer : Visualizer<ExplorationCell>
    {
        public LayerMask _foglayer;
        
        private GameObject? _fogOfWarPlane;
        private Mesh? _fogMesh;
        private Vector3[]? _fogVertices;
        private Color[]? _fogColors;
        
        public static readonly Color32 ExploredColor = new Color32(32, 130, 57, 255);
        public static readonly Color32 CoveredColor = new Color32(32, 80, 240, 255);
        public static readonly Color32 SlamSeenColor = new Color32(50, 120, 180, 255);
        public static readonly Color32 WarmColor = new Color32(200, 60, 60, 255);
        public static readonly Color32 ColdColor = new Color32(50, 120, 180, 255);

        public override void SetSimulationMap(SimulationMap<ExplorationCell> newMap, Vector3 offset)
        {
            base.SetSimulationMap(newMap, offset);
            
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

        protected override Color32 InitializeCellColor(ExplorationCell cell)
        {
            var color = SolidColor;
            if (cell.IsExplorable)
                color = cell.IsExplored ? ExploredColor : StandardCellColor;
            return color;
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

        private static int GetClosestVertex(RaycastHit aHit, int[] aTriangles)
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