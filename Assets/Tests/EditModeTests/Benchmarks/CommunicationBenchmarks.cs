using System.Collections.Generic;
using System.Diagnostics;

using Maes.Map;
using Maes.Map.Generators;
using Maes.Robot;
using Maes.UI;
using Maes.Utilities;

using NUnit.Framework;

using Tests.PlayModeTests.Utilities.MapInterpreter.MapBuilder;

using UnityEngine;

using Debug = UnityEngine.Debug;

namespace Tests.EditModeTests.Benchmarks
{
    public class CommunicationBenchmarks
    {
        private readonly SimulationMap<Tile> _tileMap;
        private RobotConstraints _robotConstraints;
        private readonly CommunicationManager _communicationManager;
        private readonly DebuggingVisualizer _visualizer;
        private readonly GameObject _mapGeneratorGameObject;
        private readonly BuildingGenerator _mapGenerator;
        private readonly BuildingMapConfig _buildingMapConfig;
        private SimulationMap<Tile> _simulationMap;
        private IReadOnlyList<Vertex> _vertices;


        [SetUp]
        public void Setup()
        {
            _simulationMap = new SimulationMapBuilder(Map).Build();
            _vertices = CreateSampleVertices(_simulationMap.HeightInTiles, _simulationMap.HeightInTiles);
            _robotConstraints = new RobotConstraints(
                materialCommunication: true,
                robotCollisions: false);
        }


        [Test]
        [Explicit]
        public void CommunicationZoneGenerationBenchmark()
        {
            // Create required dependencie
            var visualizer = new DebuggingVisualizer();


            var communicationManager = new CommunicationManager(_simulationMap, _robotConstraints, visualizer);

            Dictionary<Vector2Int, Bitmap> result = null;

            var stopWatch = new Stopwatch();
            stopWatch.Start();

            // Measure time to calculate communication zones
            result = communicationManager.CalculateZones(_vertices);

            stopWatch.Stop();

            Debug.LogFormat("Communication zone generation for {0} vertices took {1} ms",
                _vertices.Count, stopWatch.ElapsedMilliseconds);

            Assert.IsNotNull(result);
        }

        [Test]
        [Explicit]
        public void CommunicationBetweenPointsBenchmark()
        {
            // Create required dependencies
            var visualizer = new DebuggingVisualizer();

            var communicationManager = new CommunicationManager(_simulationMap, _robotConstraints, visualizer);

            // Set up benchmark parameters
            var numIterations = 10000;
            var random = new System.Random(42); // Fixed seed for reproducibility

            var stopWatch = new Stopwatch();
            stopWatch.Start();

            // Perform multiple communication checks between random points
            for (var i = 0; i < numIterations; i++)
            {
                var start = new Vector2(
                    random.Next(1, _simulationMap.HeightInTiles - 1),
                    random.Next(1, _simulationMap.WidthInTiles - 1)
                );

                var end = new Vector2(
                    random.Next(1, _simulationMap.HeightInTiles - 1),
                    random.Next(1, _simulationMap.WidthInTiles - 1)
                );

                _ = communicationManager.CommunicationBetweenPoints(start, end);
            }

            stopWatch.Stop();

            Debug.LogFormat("Communication between points: {0} checks took {1} ms",
                numIterations, stopWatch.ElapsedMilliseconds);
        }

        // Helper method to create a sample list of vertices distributed across the map
        private IReadOnlyList<Vertex> CreateSampleVertices(int width, int height)
        {
            var vertices = new List<Vertex>();

            var id = 0;

            // Create a grid of sample points (1 every 10 tiles)
            for (var x = 5; x < width - 10; x += 10)
            {
                for (var y = 5; y < height - 10; y += 10)
                {
                    vertices.Add(new Vertex(id++, new Vector2Int(x, y), x > width / 2 ? 0 : 1));
                }
            }

            return vertices;
        }


        private const string Map = "" +
            "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX;" +
            "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX;" +
            "XX       X    X                   X    X                                   X    X                  XX;" +
            "XX       X    X                   X    X                                   X    X                  XX;" +
            "XX       X    X                   X    X                                   X    X                  XX;" +
            "XX       X    X                   X    X                                   X    X                  XX;" +
            "XX       X    X                   X    X                                   X    X                  XX;" +
            "XX       X    X                   X    X                                   X    X                  XX;" +
            "XX       X    X                   X    X                                   X    X                  XX;" +
            "XX       X    X                   X    X                                   X    X                  XX;" +
            "XX       X    X                   X    X                                   X    X                  XX;" +
            "XX       X    X                   X    X                                   X    X                  XX;" +
            "XX       X    X                   X    X                                   X    X                  XX;" +
            "XX       X    X                   X    X                                   X    X                  XX;" +
            "XX       X    X                   X    X                                   X    X                  XX;" +
            "XX       X    X                   X    X                                   X    X                  XX;" +
            "XX       X    X                   X    X                                   X    X                  XX;" +
            "XX       X    X                   X    X                                   X    X                  XX;" +
            "XX       X    X                   X    X                                   X    X                  XX;" +
            "XX       X    X                   X    X                                   X    X                  XX;" +
            "XX       X    X                   X    X                                   X    X                  XX;" +
            "XX       X    X                   X    X                                   X    X                  XX;" +
            "XX       X    X                   X    X                                   X    X                  XX;" +
            "XX       X    X                   X    X                                   X    X                  XX;" +
            "XX       X    X                   X    X                                   X    X                  XX;" +
            "XX       X    X                   X    X                                   X    X                  XX;" +
            "XX       X    X                   X    X                                   X    X                  XX;" +
            "XX       X    X                   X    X                                   X    X                  XX;" +
            "XX       X    X                   X    X                                   X    X                  XX;" +
            "XX       X    X                   X    X                                   X    X                  XX;" +
            "XX       X    X                        X                                   X    X                  XX;" +
            "XX       X    X                        X                                   X    X                  XX;" +
            "XX       X                        X    X                                   X    X                  XX;" +
            "XX       X                        X    X                                   X    X                  XX;" +
            "XX       X    X                   X    X                                   X    X                  XX;" +
            "XX            X                   X    X                                   X    X                  XX;" +
            "XX            X                   X    X                                   X    X                  XX;" +
            "XX       X    X                   X    X                                   X    X                  XX;" +
            "XX       X    X                   X    X                                   X    X                  XX;" +
            "XX       X    X                   X    X                                   X    X                  XX;" +
            "XX       X    X                   X    X                                   X    X                  XX;" +
            "XX       X    X                   X    XXXXXXXXXXXXXXX  XXXXXXXXXXXXXXXXXXXX    XXXXXX  XXXXXXXXXXXXX;" +
            "XX       X    X                   X                                                                XX;" +
            "XX       X    X                   X                                                                XX;" +
            "XX       X    X                   X                                                                XX;" +
            "XX       X    X                   X                                                                XX;" +
            "XX       X    X                   X    XXXXXXXXXXX  XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX;" +
            "XX       X    X                   X    X                                                           XX;" +
            "XX       X    X                   X    X                                                           XX;" +
            "XX       X    X                   X    X                                                           XX;" +
            "XX       X    X                   X    X                                                           XX;" +
            "XX       X    X                   X    X                                                           XX;" +
            "XX       X    X                   X    X                                                           XX;" +
            "XX       X    X                   X    X                                                           XX;" +
            "XX       X    X                   X    X                                                           XX;" +
            "XX       X    X                   X    X                                                           XX;" +
            "XX       X    X                   X    XXXXXXXXXXXXXXXXXXXXXXXX  XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX;" +
            "XX       X    X                   X                                                                XX;" +
            "XX       X    X                   X                                                                XX;" +
            "XX       X    X                   X                                                                XX;" +
            "XX       X    X                   X                                                                XX;" +
            "XX       X    X                   X    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX  XXXXXXXXXXXXXXXXXXXXX;" +
            "XX       X    X                   X    X                                                           XX;" +
            "XX       X    X                   X    X                                                           XX;" +
            "XX       X    X                   X    X                                                           XX;" +
            "XX       X    X                   X    X                                                           XX;" +
            "XX       X    X                   X    X                                                           XX;" +
            "XX       X    X                   X    X                                                           XX;" +
            "XX       X    X                   X    X                                                           XX;" +
            "XX       X    X                   X    X                                                           XX;" +
            "XX       X    X                   X    X                                                           XX;" +
            "XX       X    X                   X    X                                                           XX;" +
            "XX       X    X                   X    X                                                           XX;" +
            "XX       X    X                   X    X                                                           XX;" +
            "XX       X    X                   X    X                                                           XX;" +
            "XX       X    X                   X    X                                                           XX;" +
            "XX       X    X                   X    X                                                           XX;" +
            "XX       X                        X    X                                                           XX;" +
            "XX       X                        X    X                                                           XX;" +
            "XX       X    X                   X    X                                                           XX;" +
            "XX       X    X                   X    X                                                           XX;" +
            "XX       X    X                   X    X                                                           XX;" +
            "XX       X    X                   X    X                                                           XX;" +
            "XX       X    X                   X    X                                                           XX;" +
            "XX       X    X                   X    XXXXXXXXXXXXXXXXXXXXXX  XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX;" +
            "XX       X    X                   X                                                                XX;" +
            "XX       X    X                   X                                                                XX;" +
            "XX            X                   X                                                                XX;" +
            "XX            X                   X                                                                XX;" +
            "XX       X    X                   X    XXX  XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX;" +
            "XX       X    X                   X    X                                                           XX;" +
            "XX       X    X                   X    X                                                           XX;" +
            "XX       X    X                   X    X                                                           XX;" +
            "XX       X    X                   X                                                                XX;" +
            "XX       X    X                   X                                                                XX;" +
            "XX       X    X                   X    X                                                           XX;" +
            "XX       X    X                   X    X                                                           XX;" +
            "XX       X    X                   X    X                                                           XX;" +
            "XX       X    X                   X    X                                                           XX;" +
            "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX;" +
            "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX;";
    }
}