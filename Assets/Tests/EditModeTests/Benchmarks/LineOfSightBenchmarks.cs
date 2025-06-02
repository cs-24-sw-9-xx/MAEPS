using System.Collections.Generic;
using System.Diagnostics;

using Maes.Map.Generators.Patrolling.Waypoints.Generators;
using Maes.Utilities;

using NUnit.Framework;

using Tests.PlayModeTests.Utilities;

using UnityEngine;

using Debug = UnityEngine.Debug;

namespace Tests.EditModeTests.Benchmarks
{
    public class LineOfSightBenchmarks
    {
        [Test]
        [Explicit]
        public void VisibilityComputationBenchmark()
        {
            const int iterations = 10;

            Dictionary<Vector2Int, Bitmap> result = null;

            using (var bitmap = BitmapUtilities.BitmapFromString(Map))
            {
                var stopWatch = new Stopwatch();
                stopWatch.Start();

                for (var i = 0; i < iterations; i++)
                {
                    result = GreedyMostVisibilityWaypointGenerator.ComputeVisibility(bitmap, maxDistance: 0, inaccurateButFast: false);
                }

                stopWatch.Stop();

                Debug.LogFormat("Took {0} ms with {1} iterations", stopWatch.ElapsedMilliseconds, iterations);
            }

            Assert.Greater(result!.Count, 0);
        }
        [Test]
        [Explicit]
        public void VisibilityComputationInaccurateBenchmark()
        {
            const int iterations = 10;

            Dictionary<Vector2Int, Bitmap> result = null;

            using (var bitmap = BitmapUtilities.BitmapFromString(Map))
            {
                var stopWatch = new Stopwatch();
                stopWatch.Start();

                for (var i = 0; i < iterations; i++)
                {
                    result = GreedyMostVisibilityWaypointGenerator.ComputeVisibility(bitmap, maxDistance: 0, inaccurateButFast: true);
                }

                stopWatch.Stop();

                Debug.LogFormat("Took {0} ms with {1} iterations", stopWatch.ElapsedMilliseconds, iterations);
            }

            Assert.Greater(result!.Count, 0);
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