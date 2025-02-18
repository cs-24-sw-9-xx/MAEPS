using System;

using Maes.Utilities;

using NUnit.Framework;

using UnityEngine;

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
        public class TestCase
        {
            public string Name;
            public Bitmap Map;
            public int ExpectedVisible;
            public TestCase(string name, string map, int expectedVisible)
            {
                Name = name;
                Map = BitmapFromString(map);
                ExpectedVisible = expectedVisible;
            }

            private static Bitmap BitmapFromString(string map)
            {
                var split = map.Split(';', StringSplitOptions.RemoveEmptyEntries);
                var height = split.Length;

                var width = split[0].Length;

                var bitmap = new Bitmap(0, 0, width, height);

                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        if (split[y][x] == 'X')
                        {
                            bitmap.Set(x, y);
                        }
                    }
                }

                return bitmap;
            }
        }

        public static TestCase[] ComputeVisibilityTestCases = new TestCase[] {
            new TestCase("1x1",
            " ;", 1),
            new TestCase("2x2, diagonal",
                "" +
                " X;" +
                "X ;", 1),
            new TestCase("2x2 all floor",
                "" +
                "  ;" +
                "  ;", 4),
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
            var map = testCase.Map;
            var expected = testCase.ExpectedVisible;
            var result = LineOfSightUtilities.ComputeVisibilityOfPoint(new Vector2Int(0, 0), map);
            Assert.AreEqual(expected, result.Count, $"Name: {testCase.Name}");
        }
    }
}