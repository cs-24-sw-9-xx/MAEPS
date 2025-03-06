using System.Diagnostics;

using Maes.Utilities;

using NUnit.Framework;

using Debug = UnityEngine.Debug;

namespace Tests.EditModeTests.Benchmarks
{
    public class BitmapBenchmarks
    {
        [Test]
        [Explicit]
        public void BitmapCountBenchmark()
        {
            var count = 0;
            const int iterations = 1000000;

            using (var bitmap = Utilities.CreateRandomBitmap(100, 100, 1234))
            {
                var stopWatch = new Stopwatch();
                stopWatch.Start();

                // Benchmark
                for (var i = 0; i < iterations; i++)
                {
                    count += bitmap.Count;
                }

                stopWatch.Stop();

                Debug.LogFormat("Took {0} ms with {1} iterations", stopWatch.ElapsedMilliseconds, iterations);
            }


            // To make sure we are not optimizing the code away.
            Assert.Greater(count, 0);
        }

        [Test]
        [Explicit]
        public void BitmapAnyBenchmark()
        {
            var any = false;
            const int iterations = 1000000;

            using (var bitmap = Utilities.CreateRandomBitmap(100, 100, 1234))
            {
                var stopWatch = new Stopwatch();
                stopWatch.Start();

                // Benchmark
                for (var i = 0; i < iterations; i++)
                {
                    any |= bitmap.Any;
                }

                stopWatch.Stop();

                Debug.LogFormat("Took {0} ms with {1} iterations", stopWatch.ElapsedMilliseconds, iterations);
            }


            // To make sure we are not optimizing the code away.
            Assert.True(any);
        }

        [Test]
        [Explicit]
        public void BitmapSetBenchmark()
        {
            const int iterations = 100000000;

            using (var bitmap = Utilities.CreateRandomBitmap(100, 100, 1))
            {
                var stopWatch = new Stopwatch();
                stopWatch.Start();

                // Benchmark
                for (var i = 0; i < iterations; i++)
                {
                    bitmap.Set(1, 1);
                }

                stopWatch.Stop();

                Debug.LogFormat("Took {0} ms with {1} iterations", stopWatch.ElapsedMilliseconds, iterations);
            }
        }

        [Test]
        [Explicit]
        public void BitmapUnsetBenchmark()
        {
            const int iterations = 100000000;

            using (var bitmap = Utilities.CreateRandomBitmap(100, 100, 1))
            {
                var stopWatch = new Stopwatch();
                stopWatch.Start();

                // Benchmark
                for (var i = 0; i < iterations; i++)
                {
                    bitmap.Unset(1, 1);
                }

                stopWatch.Stop();

                Debug.LogFormat("Took {0} ms with {1} iterations", stopWatch.ElapsedMilliseconds, iterations);
            }
        }

        [Test]
        [Explicit]
        public void BitmapIntersectionSameSizeBenchmark()
        {
            const int iterations = 10000;

            using (var bitmap1 = Utilities.CreateRandomBitmap(100, 100, 2))
            {
                using (var bitmap2 = Utilities.CreateRandomBitmap(100, 100, 3))
                {
                    var stopWatch = new Stopwatch();
                    stopWatch.Start();

                    // Benchmark
                    for (var i = 0; i < iterations; i++)
                    {
                        using var bitmap = Bitmap.Intersection(bitmap1, bitmap2);
                    }

                    stopWatch.Stop();

                    Debug.LogFormat("Took {0} ms with {1} iterations", stopWatch.ElapsedMilliseconds, iterations);
                }
            }
        }

        [Test]
        [Explicit]
        public void BitmapIntersectionDifferentSizeBenchmark()
        {
            const int iterations = 10000;

            using (var bitmap1 = Utilities.CreateRandomBitmap(100, 100, 2))
            {
                using (var bitmap2 = Utilities.CreateRandomBitmap(60, 60, 3))
                {
                    var stopWatch = new Stopwatch();
                    stopWatch.Start();

                    // Benchmark
                    for (var i = 0; i < iterations; i++)
                    {
                        using var bitmap = Bitmap.Intersection(bitmap1, bitmap2);
                    }

                    stopWatch.Stop();

                    Debug.LogFormat("Took {0} ms with {1} iterations", stopWatch.ElapsedMilliseconds, iterations);
                }
            }
        }

        [Test]
        [Explicit]
        public void BitmapExceptWithSameSizeBenchmark()
        {
            const int iterations = 1000000;

            using (var bitmap = Utilities.CreateRandomBitmap(100, 100, 5))
            {
                using (var otherBitmap = Utilities.CreateRandomBitmap(100, 100, 4))
                {
                    var stopWatch = new Stopwatch();
                    stopWatch.Start();

                    // Benchmark
                    for (var i = 0; i < iterations; i++)
                    {
                        using (var cloned = bitmap.Clone())
                        {
                            cloned.ExceptWith(otherBitmap);
                        }
                    }

                    stopWatch.Stop();

                    Debug.LogFormat("Took {0} ms with {1} iterations", stopWatch.ElapsedMilliseconds, iterations);
                }
            }
        }

        [Test]
        [Explicit]
        public void BitmapExceptWithDifferentSizesBenchmark()
        {
            const int iterations = 10000;

            using (var bitmap = Utilities.CreateRandomBitmap(100, 100, 5))
            {
                using (var otherBitmap = Utilities.CreateRandomBitmap(60, 60, 4))
                {
                    var stopWatch = new Stopwatch();
                    stopWatch.Start();

                    // Benchmark
                    for (var i = 0; i < iterations; i++)
                    {
                        using (var cloned = bitmap.Clone())
                        {
                            cloned.ExceptWith(otherBitmap);
                        }
                    }

                    stopWatch.Stop();

                    Debug.LogFormat("Took {0} ms with {1} iterations", stopWatch.ElapsedMilliseconds, iterations);
                }
            }
        }
    }
}