
using Maes.Map.MapPatrollingGen;

using NUnit.Framework;

using UnityEngine;

namespace PlayModeTests
{
    public class ComputeVisibilityTest
    {
        public class TestCase {
            public string Name;
            public bool[,] Map;
            public int Expected;
            public TestCase(string name, bool[,] map, int expected) {
                Name = name;
                Map = map;
                Expected = expected;
            }
        }

        public static TestCase[] ComputeVisibilityTestCases = new TestCase[] {
            new TestCase("1x1",
            new bool[,] {
                {false}
            }, 1),
            new TestCase("2x2, diagonal", new bool[,] {
                {false, true},
                {true, true},
            }, 1),
            new TestCase("2x2 all floor",new bool[,] {
                {false, false},
                {false, false},
            }, 4),
            new TestCase("2x2 can view horizontal and vertical",new bool[,] {
                {false, false},
                {false, true},
            }, 3),
            new TestCase("3x3 can see diagonal", new bool[,] {
                {false, true, true},
                {true, false, true},
                {true, true, false},
            }, 3),
            new TestCase("3x3 cannot see through walls", new bool[,] {
                {false, true, false},
                {true, true, false},
                {false, false, false},
            }, 1),
            new TestCase("3x3 middle row wall", new bool[,] {
                {false, false, false},
                {true, true, true},
                {false, false, false},
            }, 3), 
            new TestCase("3x3 can see slightly around walls", new bool[,] {
                {false, false, false},
                {true, true, false},
                {false, false, false},
            }, 4),
            new TestCase("3x3 diagonal is not a hole", new bool[,] {
                {false, true, false},
                {true, false, false},
                {false, false, false},
            }, 3),
            new TestCase("4x4 can see through hole", new bool[,] {
                {false, false, true, true},
                {true, false, false, false},
                {true, true, false, false},
                {false, true, true, false},
            }, 8),
            new TestCase("4x4 can see through small hole", new bool[,] {
                {false, false, true, false},
                {true, false, false, false},
                {true, true, true, false},
                {true, true, false, false},
            }, 6),
            new TestCase("2x4 can see around walls", new bool[,] {
                {false, true},
                {false, true},
                {false, true},
                {false, false},
            }, 4),
            };

        [Theory]
        [TestCaseSource("ComputeVisibilityTestCases")]
        public void ComputeVisibilityOfPointTests(TestCase testCase) {
            var map = testCase.Map;
            var expected = testCase.Expected;
            var result = WatchmanRouteSolver.ComputeVisibilityOfPoint(new Vector2Int(0,0), map);
            Assert.AreEqual(expected, result.Count, $"Name: {testCase.Name}");
        }
        
        [Theory]
        [TestCaseSource("ComputeVisibilityTestCases")]
        public void ComputeVisibilityOfPointFastBreakColumnTests(TestCase testCase) {
            var map = testCase.Map;
            var expected = testCase.Expected;
            var result = WatchmanRouteSolver.ComputeVisibilityOfPointFastBreakColumn(new Vector2Int(0,0), map);
            Assert.AreEqual(expected, result.Count, $"Name: {testCase.Name}");
        }
    }
}
