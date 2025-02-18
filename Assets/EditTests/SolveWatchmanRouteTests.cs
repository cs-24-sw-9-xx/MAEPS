using Maes.Map.MapPatrollingGen;
using Maes.Utilities;

using NUnit.Framework;

using System.Collections.Generic;

using UnityEngine;

using static Maes.Map.PatrollingMap;

namespace EditTests
{
    public class WatchmanRouteTest
    {
        [Test]
        public void SingleTileMap_ReturnsSingleGuard()
        {
            // Arrange
            var map = new BitMap2D(1, 1);  // Single tile map
            var visibilityAlgorithm = new VisibilityMethod((position, m) => new HashSet<Vector2Int> { position });

            // Act
            var result = WatchmanRouteSolver.SolveWatchmanRoute(map, visibilityAlgorithm);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);  // Only one guard needed for a single tile
        }

        [Test]
        public void SimplifiedVisiblityAlgorithm_ReturnsMultipleGuards()
        {
            // Arrange
            var map = new BitMap2D(3, 3);  // 3x3 map as an example
            var visibilityAlgorithm = new VisibilityMethod((position, m) => new HashSet<Vector2Int> { position }); // Simplified visibility

            // Act
            var result = WatchmanRouteSolver.SolveWatchmanRoute(map, visibilityAlgorithm);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(9, result.Count);  // Assuming it needs 9 guards for full coverage
        }

        [Test]
        public void ComputevisiblityOfPoint_ReturnsValidGuardPositions()
        {
            // Arrange
            var map = new BitMap2D(5, 5);  // 5x5 map as an example

            // Act
            var result = WatchmanRouteSolver.SolveWatchmanRoute(map, LineOfSightUtilities.ComputeVisibilityOfPoint);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);  // Since only one guard should be needed to cover the entire map.
        }

        [Test]
        public void ComputeVisibilityOfPointFastBreak_ReturnsValidGuardPositions()
        {
            // Arrange
            var map = new BitMap2D(5, 5);  // 5x5 map as an example

            // Act
            var result = WatchmanRouteSolver.SolveWatchmanRoute(map, LineOfSightUtilities.ComputeVisibilityOfPointFastBreak);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);  // Since only one guard should be needed to cover the entire map.
        }
    }
}