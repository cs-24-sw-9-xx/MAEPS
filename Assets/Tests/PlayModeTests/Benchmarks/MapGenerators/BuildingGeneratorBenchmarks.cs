using System.Diagnostics;

using Maes.Map.Generators;

using NUnit.Framework;

using UnityEngine;

using Debug = UnityEngine.Debug;

namespace Tests.PlayModeTests.Benchmarks.MapGenerators
{
    [TestFixture(100)]
    [TestFixture(200)]
    [TestFixture(300)]
    public class BuildingGeneratorBenchmarks
    {
        private readonly BuildingMapConfig _buildingMapConfig;

        private GameObject _mapGeneratorGameObject;
        private BuildingGenerator _mapGenerator;

        public BuildingGeneratorBenchmarks(int mapSize)
        {
            _buildingMapConfig = new(123, widthInTiles: mapSize, heightInTiles: mapSize);
        }

        [SetUp]
        public void Setup()
        {
            var mapGeneratorPrefab = Resources.Load<GameObject>("MapGenerator");
            _mapGeneratorGameObject = Object.Instantiate(mapGeneratorPrefab);
            _mapGenerator = _mapGeneratorGameObject.AddComponent<BuildingGenerator>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_mapGeneratorGameObject);
        }

        [Test]
        [Explicit]
        public void GeneratorBenchmark()
        {
            var count = 0;
            const int iterations = 5;

            var stopWatch = new Stopwatch();
            stopWatch.Start();

            // Benchmark
            for (var i = 0; i < iterations; i++)
            {
                var map = _mapGenerator.GenerateBuildingMap(_buildingMapConfig);
                count += map.WidthInTiles;
            }

            stopWatch.Stop();

            Debug.LogFormat("Iterations: {0}. {1} ms per iteration. Total time: {2} ms", iterations, stopWatch.ElapsedMilliseconds / iterations, stopWatch.ElapsedMilliseconds);


            // To make sure we are not optimizing the code away.
            Assert.Greater(count, 0);
        }
    }
}