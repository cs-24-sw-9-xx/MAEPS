using System.Collections.Generic;

using Maes.Map;
using Maes.Statistics;

using UnityEngine;
using UnityEngine.Rendering;

namespace Maes.UI.Visualizers
{
    public abstract class Visualizer : MonoBehaviour, IVisualizer
    {
        public MeshRenderer meshRenderer = null!;
        public MeshFilter meshFilter = null!;

        // Set in SetSimulationMap
        protected Mesh _mesh = null!;

        public static readonly Color32 SolidColor = new(0, 0, 0, 255);
        public static readonly Color32 StandardCellColor = new(170, 170, 170, 255);
        public static readonly Color32 VisibleColor = new(32, 130, 57, 255);

        private int _widthInTiles, _heightInTiles;

        protected readonly List<Vector3> _vertices = new();
        private readonly List<int> _triangles = new();

        // Set in SetSimulationMap
        private SimulationMap<Cell> _map = null!;
        protected Color32[] _colors = null!;

        private const int ResolutionMultiplier = 2;


        public delegate Color32 CellToColor(Cell cell);
        public delegate Color32 CellIndexToColor(int cellIndex);

        public virtual void SetSimulationMap(SimulationMap<Cell> newMap)
        {
            _map = newMap;
            _widthInTiles = _map.WidthInTiles;
            _heightInTiles = _map.HeightInTiles;

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
        }

        private void GenerateTriangleVertices()
        {
            _vertices.Clear();
            const float vertexDistance = 1f / ResolutionMultiplier;
            for (var y = 0; y < _heightInTiles; y++)
            {
                var translatedY = y - 0.5f;
                for (var x = 0; x < _widthInTiles; x++)
                {
                    var translatedX = x - 0.5f;
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
            for (var i = 0; i < _vertices.Count; i++)
            {
                _triangles.Add(i);
            }
        }

        // Colors each triangle depending on its current state
        private void InitializeColors(SimulationMap<Cell> newMap)
        {
            foreach (var (index, cell) in newMap)
            {
                var vertexIndex = index * 3;
                var color = InitializeCellColor(cell);
                _colors[vertexIndex] = color;
                _colors[vertexIndex + 1] = color;
                _colors[vertexIndex + 2] = color;
            }
        }

        protected virtual Color32 InitializeCellColor(Cell cell)
        {
            return cell.IsExplorable ? StandardCellColor : SolidColor;
        }

        /// <summary>
        /// Updates the color of ALL triangles based on the given map and color function. This is an expensive operation
        /// and should be only called when it is necessary to replace all colors.
        /// </summary>
        public void SetAllColors(SimulationMap<Cell> map, CellToColor cellToColor)
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
        /// and should be only called when it is necessary to replace all colors.
        /// </summary>
        public void SetAllColors(SimulationMap<Cell> map, CellIndexToColor cellToColor)
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

        public void ResetCellColor()
        {
            foreach (var (index, _) in _map)
            {
                SetCellColor(index, StandardCellColor);
            }
        }
    }
}