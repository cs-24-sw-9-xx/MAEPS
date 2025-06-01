using System.Collections.Generic;

using Maes.Map;
using Maes.Statistics;
using Maes.Utilities;

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


        // Set in SetSimulationMap
        private SimulationMap<Cell> _map = null!;
        protected Color32[] _colors = null!;
        private Dictionary<int, HashSet<int>> _cellIndexToTriangleIndexes = null!;

        private const int ResolutionMultiplier = 2;


        public delegate Color32 CellToColor(Cell cell);
        public delegate Color32 CellIndexToColor(int cellIndex);

#if !MAEPS_GUI
        private void Start()
        {
            Destroy(gameObject);
        }
#endif

#if MAEPS_GUI
        public virtual void SetSimulationMap(SimulationMap<Cell> newMap)
        {
            _map = newMap;
            _widthInTiles = _map.WidthInTiles;
            _heightInTiles = _map.HeightInTiles;

            var vertices = GenerateTriangleVertices();
            var triangles = GenerateTriangles(vertices);
            _colors = new Color32[vertices.Length];
            InitializeColors(_map);

            _mesh = new Mesh
            {
                name = "Visualizer",
                indexFormat = IndexFormat.UInt32,
                vertices = vertices,
                triangles = triangles,
                colors32 = _colors
            };
            _mesh.RecalculateNormals();

            _cellIndexToTriangleIndexes = newMap.CellIndexToTriangleIndexes();
            meshFilter.sharedMesh = _mesh;
        }

        private void OnDestroy()
        {
            Destroy(_mesh);
        }
#endif

        private Vector3[] GenerateTriangleVertices()
        {
            // 8 triangles per tile
            // 3 vertices per triangle
            var vertices = new List<Vector3>(_widthInTiles * _heightInTiles * 8 * 3);
            const float vertexDistance = 1f / ResolutionMultiplier;
            for (var y = 0; y < _heightInTiles; y++)
            {
                var translatedY = y - 0.5f;
                for (var x = 0; x < _widthInTiles; x++)
                {
                    var translatedX = x - 0.5f;
                    AddTileTriangleVertices(translatedX, translatedY, vertexDistance, vertices);
                }
            }

            return vertices.ToArray();
        }

        // Adds all of the vertices needed for a tile of 8 triangles
        // The triangles are indexed and arranged as shown in this very pretty illustration:
        // |4/5|6\7|
        // |0\1|2/3|
        private static void AddTileTriangleVertices(float x, float y, float vertexDistance, List<Vector3> vertices)
        {
            // Triangle 0
            vertices.Add(new Vector3(x, y, 0f));
            vertices.Add(new Vector3(x, y + vertexDistance, 0f));
            vertices.Add(new Vector3(x + vertexDistance, y, 0f));

            // Triangle 1
            vertices.Add(new Vector3(x, y + vertexDistance));
            vertices.Add(new Vector3(x + vertexDistance, y + vertexDistance));
            vertices.Add(new Vector3(x + vertexDistance, y));

            // Triangle 2
            vertices.Add(new Vector3(x + vertexDistance, y + vertexDistance));
            vertices.Add(new Vector3(x + 2 * vertexDistance, y + vertexDistance));
            vertices.Add(new Vector3(x + vertexDistance, y));

            // Triangle 3
            vertices.Add(new Vector3(x + vertexDistance, y));
            vertices.Add(new Vector3(x + 2 * vertexDistance, y + vertexDistance));
            vertices.Add(new Vector3(x + 2 * vertexDistance, y));

            // Triangle 4
            vertices.Add(new Vector3(x, y + 2 * vertexDistance));
            vertices.Add(new Vector3(x + vertexDistance, y + 2 * vertexDistance));
            vertices.Add(new Vector3(x, y + vertexDistance));

            // Triangle 5
            vertices.Add(new Vector3(x, y + vertexDistance));
            vertices.Add(new Vector3(x + vertexDistance, y + 2 * vertexDistance));
            vertices.Add(new Vector3(x + vertexDistance, y + vertexDistance));

            // Triangle 6
            vertices.Add(new Vector3(x + vertexDistance, y + 2 * vertexDistance));
            vertices.Add(new Vector3(x + 2 * vertexDistance, y + vertexDistance));
            vertices.Add(new Vector3(x + vertexDistance, y + vertexDistance));

            // Triangle 7
            vertices.Add(new Vector3(x + vertexDistance, y + 2 * vertexDistance));
            vertices.Add(new Vector3(x + 2 * vertexDistance, y + 2 * vertexDistance));
            vertices.Add(new Vector3(x + 2 * vertexDistance, y + vertexDistance));
        }


        private static int[] GenerateTriangles(Vector3[] vertices)
        {
            var triangles = new int[vertices.Length];
            // The vertices are already arranged in the correct order (ie. triangle 0 has vertices indexed 0, 1, 2)
            for (var i = 0; i < vertices.Length; i++)
            {
                triangles[i] = i;
            }

            return triangles;
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

        public void SetAllColors(Bitmap map, Color32 isContained, Color32 isNotContained)
        {
            var triangleIndexes = new HashSet<int>();
            foreach (var tile in map)
            {
                var index = tile.x + tile.y * map.Width;
                triangleIndexes.UnionWith(_cellIndexToTriangleIndexes[index]);
            }

            Color32 CellIndexToColor(int triangleIndex)
            {
                return triangleIndexes.Contains(triangleIndex)
                ? isContained
                : isNotContained;
            }

            SetAllColors(CellIndexToColor);
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
            _mesh.colors32 = _colors;
        }
    }
}