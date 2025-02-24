using JetBrains.Annotations;

using Maes.Utilities;

using NUnit.Framework;

using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

using UnityEngine;

using Bitmap = Maes.Utilities.Bitmap;

namespace EditTests
{
    /// <summary>
    /// These tests are for the ComputeVisibilityOfPoint and ComputeVisibilityOfPointFastBreakColumn methods.
    /// To get a better understanding of the the difference between the two algorithms please use
    /// the SaveAsImage.SaveVisibileTiles() debug method in the following method in WatchmanRouteSolver.cs: 
    /// private static Dictionary<Vector2Int, HashSet<Vector2Int>> ComputeVisibility(bool[,] map)
    /// In short, the ComputeVisibilityOfPointFastBreakColumn, will break the loop when it finds a column without any visible tiles.
    /// However, due to the way visibility is calculated, it is possible that there is a column with visible tiles after a column with no visible tiles.
    /// </summary>
    public class ComputeVisibilityTest
    {
        public sealed class TestCase
        {
            public readonly string Name;
            public readonly string MapData;
            public readonly Vector2Int Point;

            public Bitmap Map
            {
                [MustDisposeResource]
                get => Utilities.BitmapFromString(MapData);
            }

            public readonly int ExpectedVisible;
            public TestCase(string name, string map, int expectedVisible, Vector2Int point)
            {
                Name = name;
                MapData = map;
                ExpectedVisible = expectedVisible;
                Point = point;
            }

            public TestCase(string name, string map, int expectedVisible)
            : this(name, map, expectedVisible, Vector2Int.zero)
            {
                Name = name;
                MapData = map;
                ExpectedVisible = expectedVisible;
            }

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
                "X ;", 1, new Vector2Int(1, 1)),
            new TestCase("2x2 all floor",
                "" +
                "  ;" +
                "  ;", 4),
            new TestCase("2x2 all floor (1,0)",
                "" +
                "  ;" +
                "  ;", 4, new Vector2Int(1,0)),
            new TestCase("2x2 all floor (1,1)",
                "" +
                "  ;" +
                "  ;", 4, new Vector2Int(1,1)),
            new TestCase("2x2 all floor (0,1)",
                "" +
                "  ;" +
                "  ;", 4, new Vector2Int(0,1)),
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
            , 4)
            };

        [Theory]
        [TestCaseSource("ComputeVisibilityTestCases")]
        public void ComputeVisibilityOfPointTests(TestCase testCase)
        {
            using var map = testCase.Map;
            var expected = testCase.ExpectedVisible;
            var nativeMap = map.ToUint4Array();
            var nativeVisibility = new NativeArray<uint4>(nativeMap.Length * map.Height, Allocator.Persistent);

            var job = new VisibilityJob()
            {
                Width = map.Width,
                Height = map.Height,
                Map = nativeMap,
                X = testCase.Point.x,
                Visibility = nativeVisibility
            };

            job.Schedule().Complete();

            using var result = Bitmap.FromUint4Array(map.Width, map.Height, nativeMap.Length, nativeVisibility)[testCase.Point.y];
            nativeMap.Dispose();
            nativeVisibility.Dispose();
            Debug.Log("Input map");
            Debug.Log(map.DebugView.Replace(' ', '_'));

            Debug.LogFormat("Point: {0}", testCase.Point);

            Debug.Log("Output map");
            Debug.Log(result.DebugView.Replace(' ', '_'));

            Assert.AreEqual(expected, result.Count, $"Name: {testCase.Name}");

        }
    }
}