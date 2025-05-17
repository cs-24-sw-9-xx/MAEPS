using JetBrains.Annotations;

using Maes.Map.Generators.Patrolling;
using Maes.Utilities;

using NUnit.Framework;

using Tests.EditModeTests.Utilities;

using Unity.Collections;
using Unity.Jobs;

using UnityEngine;

using Bitmap = Maes.Utilities.Bitmap;

namespace Tests.EditModeTests
{
    /// <summary>
    /// These tests are for the ComputeVisibilityOfPoint and ComputeVisibilityOfPointFastBreakColumn methods.
    /// To get a better understanding of the the difference between the two algorithms please use
    /// the SaveAsImage.SaveVisibileTiles() debug method in the following method in WatchmanRouteSolver.cs: 
    /// private static Dictionary{Vector2Int, HashSet{Vector2Int}} ComputeVisibility(bool[,] map)
    /// In short, the ComputeVisibilityOfPointFastBreakColumn, will break the loop when it finds a column without any visible tiles.
    /// However, due to the way visibility is calculated, it is possible that there is a column with visible tiles after a column with no visible tiles.
    /// </summary>
    public class ComputeVisibilityTest
    {
        public sealed class TestCase
        {
            public readonly string Name;
            public readonly int ExpectedVisible;
            public readonly float MaxVisibilityRange;
            public readonly Vector2Int Point;

            private readonly string _mapData;

            public Bitmap Bitmap
            {
                [MustDisposeResource]
                get => BitmapUtilities.BitmapFromString(_mapData);
            }

            public TestCase(string name, string map, int expectedVisible, float maxVisibilityRange, Vector2Int point)
            {
                Name = name;
                _mapData = map;
                ExpectedVisible = expectedVisible;
                Point = point;
                MaxVisibilityRange = maxVisibilityRange;
            }

            public TestCase(string name, string map, int expectedVisible, float maxVisibilityRange = 0)
            : this(name, map, expectedVisible, maxVisibilityRange, Vector2Int.zero)
            { }
        }

        public static TestCase[] ComputeVisibilityTestCases = new TestCase[] {
            new TestCase("1x1",
            " ;", 1),
            new TestCase("2x2, diagonal",
                "" +
                " X;" +
                "X ;", 1),
            new TestCase("2x2, diagonal (1,1)",
                "" +
                " X;" +
                "X ;", 1, 0, new Vector2Int(1, 1)),
            new TestCase("2x2 all floor",
                "" +
                "  ;" +
                "  ;", 4),
            new TestCase("2x2 all floor (1,0)",
                "" +
                "  ;" +
                "  ;", 4, 0, new Vector2Int(1,0)),
            new TestCase("2x2 all floor (1,1)",
                "" +
                "  ;" +
                "  ;", 4, 0, new Vector2Int(1,1)),
            new TestCase("2x2 all floor (0,1)",
                "" +
                "  ;" +
                "  ;", 4, 0, new Vector2Int(0,1)),
            new TestCase("2x2 can view horizontal and vertical",
                "" +
                "  ;" +
                " X;"
                , 3),
            new TestCase("3x3 cannot diagonal",
                ""+
                " XX;" +
                "X X;" +
                "XX ;", 1),
            new TestCase("3x3 cannot see through walls",
                "" +
                " X ;" +
                "XX ;" +
                "   ;"
                , 1),
            new TestCase("3x3 middle row wall",
                "" +
                "   ;" +
                "XXX;" +
                "   ;", 3),
            new TestCase("3x3 cannot see slightly around walls",
                "" +
                "   ;" +
                "XX ;" +
                "   ;", 3),
            new TestCase("3x3 diagonal is not a hole",
                "" +
                " X ;" +
                "X  ;" +
                "   ;"
            , 1),
            new TestCase("4x4 can see through hole",
                "" +
                "  XX;" +
                "X   ;" +
                "XX  ;" +
                " XX ;", 7),
            new TestCase("4x4 can see through small hole",
                "" +
                "  X ;" +
                "X   ;" +
                "XXX ;" +
                "XX  ;", 5),
            new TestCase("2x4 cannot see around walls",
                "" +
                " X;" +
                " X;" +
                " X;" +
                "  ;"
            , 4),
            new TestCase("1x4 cannot see further than max range",
                "" +
                " ;" +
                " ;" +
                " ;" +
                " ;"
            , 3, 2),
            };

        private const string Map = "" +
           "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX;" +
           "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX;" +
           "XX                                                    X    X                  X    X               XX;" +
           "XX                                                    X    X                  X                    XX;" +
           "XX                                                    X    X                  X                    XX;" +
           "XX                                                    X    X                  X    X               XX;" +
           "XX                                                    X                       X    X               XX;" +
           "XX                                                    X                       X    X               XX;" +
           "XX                                                    X    X                  X    X               XX;" +
           "XXXXXXXXXXX  XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX    X                  X    X               XX;" +
           "XX                                                         X                  X    X               XX;" +
           "XX                                                         X                  X    X               XX;" +
           "XX                                                         X                  X    X               XX;" +
           "XX                                                         X                  X    X               XX;" +
           "XXXXXXXXXXXXXXXXXXXXXXX  XXXXX    XXXXXXXXXXXXXX  XXXXX    X                  X    X               XX;" +
           "XX                           X    X                   X    X                  X    X               XX;" +
           "XX                           X    X                   X    X                  X    X               XX;" +
           "XX                           X    X                   X    X                  X    X               XX;" +
           "XX                           X    X                   X    X                  X    X               XX;" +
           "XX                           X    X                   X    X                  X    X               XX;" +
           "XX                           X    X                   X    X                  X    X               XX;" +
           "XX                           X    X                   X    X                  X    X               XX;" +
           "XX                           X    X                   X    X                  X    X               XX;" +
           "XX                           X    X                   X    X                  X    XX  XXXXXXXXXXXXXX;" +
           "XX                           X    X                   X    X                  X                    XX;" +
           "XX                           X                        X    X                  X                    XX;" +
           "XX                           X                        X    X                  X                    XX;" +
           "XX                           X    X                   X    X                                       XX;" +
           "XX                           X    X                   X    X                       XXXXXXXX  XXXXXXXX;" +
           "XX                           X    X                   X    X                  X    X               XX;" +
           "XX                           X    X                   X    X                  X    X               XX;" +
           "XX                           X    X                   X    X                  X    X               XX;" +
           "XX                           X    X                   X    X                  X    X               XX;" +
           "XX                           X    X                   X    X                  X    X               XX;" +
           "XX                           X    X                   X    X                  X    X               XX;" +
           "XX                           X    X                   X    X                  X    X               XX;" +
           "XX                           X    X                   X    X                  X    X               XX;" +
           "XX                           X    X                   X    X                  X    X               XX;" +
           "XX                           X    X                   X    X                  X    X               XX;" +
           "XX                           X    X                   X    X                  X    X               XX;" +
           "XX                           X    X                   X    X                  X    X               XX;" +
           "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXX    XXXXXXXXXXXXXX  XXXXX    X                  X    X               XX;" +
           "XX                                                         X                  X    X               XX;" +
           "XX                                                         X                  X    X               XX;" +
           "XX                                                         X                  X    X               XX;" +
           "XX                                                         X                  X    X               XX;" +
           "XXXXXXXXXXXXXXXXXXXXXXXXXXX  XXXXXXXXXXXXXXXXXXXXXXXXXX    X                  X    X               XX;" +
           "XX                                                    X    X                  X    X               XX;" +
           "XX                                                    X    X                  X    X               XX;" +
           "XX                                                    X    XXXXXXXXXXX  XXXXXXX    X               XX;" +
           "XX                                                    X                            X               XX;" +
           "XX                                                    X                            X               XX;" +
           "XX                                                    X                            X               XX;" +
           "XX                                                    X                            X               XX;" +
           "XX                                                    X    XXXXXXXXXXX  XXXXXXX    X               XX;" +
           "XX                                                         X                  X    X               XX;" +
           "XX                                                         X                  X    X               XX;" +
           "XX                                                    X    X                  X    X               XX;" +
           "XX                                                    X    X                  X    X               XX;" +
           "XX                                                    X    X                  X    X               XX;" +
           "XX                                                    X    X                  X    X               XX;" +
           "XX                                                    X    X                  X    X               XX;" +
           "XX                                                    X    X                  X    X               XX;" +
           "XX                                                    X    X                  X    X               XX;" +
           "XX                                                    X    X                  X    X               XX;" +
           "XX                                                    X    X                  X    X               XX;" +
           "XXXXXXXXX  XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX    XXXXXXXXXXXXXXXXXXXX    X               XX;" +
           "XX                                                                                 X               XX;" +
           "XX                                                                                 X               XX;" +
           "XX                                                                                 X               XX;" +
           "XX                                                                                 X               XX;" +
           "XXXXXX  XXXX    XX  XXXXXXXXXXX    XXXXXXXXXX  XXXXXXX    XXXXXXX  XXXXXXXXXXXX    X               XX;" +
           "XX         X    X             X    X                 X    X                   X    X               XX;" +
           "XX         X    X             X    X                 X    X                   X    X               XX;" +
           "XX         X    X             X    X                 X    X                   X                    XX;" +
           "XX         X    X             X    X                 X    X                   X                    XX;" +
           "XX         X    X             X    X                 X    X                   X    X               XX;" +
           "XX         X    X             X    X                 X    X                   X    X               XX;" +
           "XX         X    X             X    X                 X    X                   X    X               XX;" +
           "XX         X    X             X    X                 X    X                   X    X               XX;" +
           "XX         X    X             X    X                 X    X                   X    X               XX;" +
           "XX         X    X             X    X                 X    X                   X    X               XX;" +
           "XX         X                  X    X                 X    X                   X    X               XX;" +
           "XX         X                  X    X                 X    X                   X    X               XX;" +
           "XX         X    X             X    X                 X    X                   X    X               XX;" +
           "XX         X    X             X    X                 X    X                   X    X               XX;" +
           "XX         X    X             X    X                 X    X                   X    X               XX;" +
           "XX         X    X             X    X                 X    X                   X    X               XX;" +
           "XX         X    X             X    X                 X    X                   X    X               XX;" +
           "XX         X    X             X    X                 X    X                   X    X               XX;" +
           "XX         X    X             X    X                 X    X                   X    X               XX;" +
           "XX         X    X             X    X                 X    X                   X    X               XX;" +
           "XX         X    X             X    X                 X    X                   X    X               XX;" +
           "XX         X    X             X    X                 X    X                   X    X               XX;" +
           "XX              X             X    X                 X    X                   X    X               XX;" +
           "XX              X             X    X                 X    X                   X    X               XX;" +
           "XX         X    X             X    X                 X    X                   X    X               XX;" +
           "XX         X    X             X    X                 X    X                   X    X               XX;" +
           "XX         X    X             X    X                 X    X                   X    X               XX;" +
           "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX;" +
           "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX;";

        [Theory]
        [TestCaseSource(nameof(ComputeVisibilityTestCases))]
        public void ComputeVisibilityOfPointTests(TestCase testCase)
        {
            using var map = testCase.Bitmap;
            var expected = testCase.ExpectedVisible;
            var nativeMap = map.ToNativeArray();
            var nativeVisibility = new NativeArray<ulong>(nativeMap.Length * map.Height, Allocator.TempJob);

            var job = new VisibilityJob()
            {
                Width = map.Width,
                Height = map.Height,
                Map = nativeMap,
                X = testCase.Point.x,
                Visibility = nativeVisibility,
                MaxDistance = testCase.MaxVisibilityRange,
            };

            job.Schedule().Complete();

            using var result = Bitmap.FromNativeArray(map.Width, map.Height, nativeMap.Length, nativeVisibility)[testCase.Point.y];
            nativeMap.Dispose();
            nativeVisibility.Dispose();
            Debug.Log("Input map");
            Debug.Log(map.DebugView.Replace(' ', '_'));

            Debug.LogFormat("Point: {0}", testCase.Point);

            Debug.Log("Output map");
            Debug.Log(result.DebugView.Replace(' ', '_'));

            Assert.AreEqual(expected, result.Count, $"Name: {testCase.Name}");
        }

        [Test]
        [Explicit]
        public void EnsureValidWaypointsTest()
        {
            using (var bitmap = BitmapUtilities.BitmapFromString(Map))
            {
                var waypoints = VisibilityCache.ComputeVisibility(bitmap);

                // Check that there are no waypoints inside the wall
                foreach (var (waypoint, _) in waypoints)
                {
                    var contains = bitmap.Contains(waypoint);
                    Assert.False(contains, "Waypoint is inside wall at {0}!", waypoint);
                }

                // Check that the whole map is visible
                using (var checkMap = new Bitmap(bitmap.Width, bitmap.Height))
                {
                    for (var x = 0; x < checkMap.Width; x++)
                    {
                        for (var y = 0; y < checkMap.Height; y++)
                        {
                            checkMap.Set(x, y);
                        }
                    }

                    foreach (var (_, visibilityMap) in waypoints)
                    {
                        checkMap.ExceptWith(visibilityMap);
                    }

                    Assert.AreEqual(bitmap, checkMap, "Waypoints does not cover map!");
                }
            }
        }

        [Test]
        [Explicit]
        public void CachedVisibilityTest()
        {
            using (var bitmap = BitmapUtilities.BitmapFromString(Map))
            {
                // Clear the cache folder
                System.IO.Directory.CreateDirectory(Maes.GlobalSettings.MapCacheLocation);
                var files = System.IO.Directory.GetFiles(Maes.GlobalSettings.MapCacheLocation);
                foreach (var file in files)
                {
                    System.IO.File.Delete(file);
                }

                // Write a file to the cache folder, to imitate old cache data
                var oldCacheDataFile = System.IO.Path.Combine(Maes.GlobalSettings.MapCacheLocation, "visibility-1.json");
                System.IO.File.WriteAllText(oldCacheDataFile, "Old cache data");
                files = System.IO.Directory.GetFiles(Maes.GlobalSettings.MapCacheLocation);
                Assert.AreEqual(1, files.Length, "Cache folder should only contain one file");

                // Check if the first use of the cache causes it to clear the cache of old data and add the new map to the cache
                VisibilityCache.ComputeVisibilityCached(bitmap, 0);
                files = System.IO.Directory.GetFiles(Maes.GlobalSettings.MapCacheLocation);
                Assert.AreEqual(1, files.Length, "Cache folder should only contain one file");
                var cachedVisibility = files[0];
                Assert.AreEqual(cachedVisibility, "MapCache/visibility-101X101-maxDist0-index0.json", "Cache file should have the correct name");

                // Cache hit, it should read from the cache and not add any new files to the cache
                VisibilityCache.ComputeVisibilityCached(bitmap, 0);
                files = System.IO.Directory.GetFiles(Maes.GlobalSettings.MapCacheLocation);
                Assert.AreEqual(1, files.Length, "Cache folder should only contain one file");
                // Check the file name, if it has overwritten the previous file, it would use 1 as cache index
                Assert.AreEqual(cachedVisibility, "MapCache/visibility-101X101-maxDist0-index0.json", "Cache file should have the correct name");

                // Add a new map to the cache, check that it used the next index
                var newMap = BitmapUtilities.BitmapFromString("X");
                VisibilityCache.ComputeVisibilityCached(newMap, 0);
                files = System.IO.Directory.GetFiles(Maes.GlobalSettings.MapCacheLocation);
                Assert.AreEqual(2, files.Length, "Cache folder should contain two files");
                var newCachedVisibility = files[1];
                Assert.AreEqual(newCachedVisibility, "MapCache/visibility-1X1-maxDist0-index1.json", "Cache file should have the correct name");
            }
        }
    }
}