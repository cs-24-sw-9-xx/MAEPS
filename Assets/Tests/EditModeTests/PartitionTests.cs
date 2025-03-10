// Copyright 2025 MAEPS
// 
// This file is part of MAEPS
// 
// MAEPS is free software: you can redistribute it and/or modify it under
// the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your option)
// any later version.
// 
// MAEPS is distributed in the hope that it will be useful, but WITHOUT
// ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
// or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General
// Public License for more details.
// 
// You should have received a copy of the GNU General Public License along
// with MAEPS. If not, see http://www.gnu.org/licenses/.
//
// Contributors 2025: 
// Unit tests by S3JER

using System;
using System.Collections.Generic;

using Maes.Map;
using Maes.Utilities;

using NUnit.Framework;

using UnityEngine;

namespace Tests.EditModeTests
{
    public class PartitionTests
    {
        private List<Vertex> _vertices;
        private Dictionary<Vector2Int, Bitmap> _communicationZones;
        private readonly int _bitmapWidth = 10;
        private readonly int _bitmapHeight = 10;

        [SetUp]
        public void Setup()
        {
            // Create vertices for testing
            _vertices = new List<Vertex>
            {
                new Vertex(0, 1, new Vector2Int(2, 2)),
                new Vertex(1, 1, new Vector2Int(5, 5)),
                new Vertex(2, 1, new Vector2Int(8, 8))
            };

            // Create communication zones for testing
            _communicationZones = new Dictionary<Vector2Int, Bitmap>();

            // Communication zone for vertex at (2,2)
            var bitmap1 = new Bitmap(0, 0, _bitmapWidth, _bitmapHeight);
            for (var x = 1; x <= 3; x++)
            {
                for (var y = 1; y <= 3; y++)
                {
                    bitmap1.Set(x, y);
                }
            }
            _communicationZones[new Vector2Int(2, 2)] = bitmap1;

            // Communication zone for vertex at (5,5)
            var bitmap2 = new Bitmap(0, 0, _bitmapWidth, _bitmapHeight);
            for (var x = 4; x <= 6; x++)
            {
                for (var y = 4; y <= 6; y++)
                {
                    bitmap2.Set(x, y);
                }
            }
            _communicationZones[new Vector2Int(5, 5)] = bitmap2;

            // Communication zone for vertex at (8,8)
            var bitmap3 = new Bitmap(0, 0, _bitmapWidth, _bitmapHeight);
            for (var x = 7; x <= 9; x++)
            {
                for (var y = 7; y <= 9; y++)
                {
                    bitmap3.Set(x, y);
                }
            }
            _communicationZones[new Vector2Int(8, 8)] = bitmap3;
        }

        [TearDown]
        public void TearDown()
        {
            // Dispose all bitmaps to prevent memory leaks
            foreach (var bitmap in _communicationZones.Values)
            {
                bitmap.Dispose();
            }
        }

        [Test]
        public void Constructor_InitializesPropertiesCorrectly()
        {
            // Arrange & Act
            var partition = new Partition(1, _vertices, _communicationZones);

            // Assert
            Assert.AreEqual(1, partition.PartitionId);
            Assert.AreEqual(_vertices, partition.Vertices);
            Assert.AreEqual(_communicationZones, partition.CommunicationZones);
            Assert.IsNotNull(partition.CommunicationZone);

            // Check communication zone contains all expected tiles
            Assert.AreEqual(27, partition.CommunicationZone.Count); // 3x3 + 3x3 + 3x3 = 27 tiles

            // Check key vertices are contained in communication zone
            Assert.IsTrue(partition.CommunicationZone.Contains(2, 2));
            Assert.IsTrue(partition.CommunicationZone.Contains(5, 5));
            Assert.IsTrue(partition.CommunicationZone.Contains(8, 8));
        }

        [Test]
        public void Constructor_ThrowsException_WhenCommunicationZonesEmpty()
        {
            // Arrange
            var emptyZones = new Dictionary<Vector2Int, Bitmap>();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new Partition(1, _vertices, emptyZones));
        }

        [Test]
        public void Constructor_ThrowsException_WhenVertexMissingCommunicationZone()
        {
            // Arrange
            var incompleteZones = new Dictionary<Vector2Int, Bitmap>
            {
                { new Vector2Int(2, 2), _communicationZones[new Vector2Int(2, 2)] },
                // Missing communication zones for other vertices
            };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new Partition(1, _vertices, incompleteZones));
        }

        [Test]
        public void CalculateIntersectionAndRatio_CalculatesCorrectly_WhenPartitionsIntersect()
        {
            // Arrange
            var partition1 = new Partition(1, _vertices, _communicationZones);

            // Create a second partition with vertices that intersect with the first
            var vertices2 = new List<Vertex>
            {
                new Vertex(3, 1, new Vector2Int(3, 3)), // Intersects with vertex at (2,2)
                new Vertex(4, 1, new Vector2Int(7, 7))  // Intersects with vertex at (8,8)
            };

            var communicationZones2 = new Dictionary<Vector2Int, Bitmap>();

            // Communication zone for vertex at (3,3)
            var bitmap4 = new Bitmap(0, 0, _bitmapWidth, _bitmapHeight);
            for (var x = 2; x <= 4; x++)
            {
                for (var y = 2; y <= 4; y++)
                {
                    bitmap4.Set(x, y);
                }
            }
            communicationZones2[new Vector2Int(3, 3)] = bitmap4;

            // Communication zone for vertex at (7,7)
            var bitmap5 = new Bitmap(0, 0, _bitmapWidth, _bitmapHeight);
            for (var x = 6; x <= 8; x++)
            {
                for (var y = 6; y <= 8; y++)
                {
                    bitmap5.Set(x, y);
                }
            }
            communicationZones2[new Vector2Int(7, 7)] = bitmap5;

            var partition2 = new Partition(2, vertices2, communicationZones2);

            // Act
            partition1.CalculateIntersectionAndRatio(partition2);

            // Assert
            var key = (1, 2); // (partition1.PartitionId, partition2.PartitionId)

            // Verify intersection zones dictionary contains the key
            Assert.IsTrue(partition1.IntersectionZones.ContainsKey(key));

            // Verify communication ratio dictionary contains the key
            Assert.IsTrue(partition1.CommunicationRatio.ContainsKey(key));

            // Verify intersection zone has correct count
            var intersectionBitmap = partition1.IntersectionZones[key];
            Assert.Greater(intersectionBitmap.Count, 0);

            // Calculate expected ratio: intersection count / total count of partition2's communication zone
            var expectedRatio = (float)intersectionBitmap.Count / partition2.CommunicationZone.Count;
            Assert.AreEqual(expectedRatio, partition1.CommunicationRatio[key]);

            // Clean up
            bitmap4.Dispose();
            bitmap5.Dispose();
            intersectionBitmap.Dispose();
        }

        [Test]
        public void CalculateIntersectionAndRatio_ReturnsZeroRatio_WhenNoIntersection()
        {
            // Arrange
            var partition1 = new Partition(1, new List<Vertex> { _vertices[0] }, // Only use vertex at (2,2)
                new Dictionary<Vector2Int, Bitmap> { { new Vector2Int(2, 2), _communicationZones[new Vector2Int(2, 2)] } });

            // Create a second partition with no intersection
            var vertex = new Vertex(3, 1, new Vector2Int(9, 9));
            var vertices2 = new List<Vertex> { vertex };

            var communicationZones2 = new Dictionary<Vector2Int, Bitmap>();
            var bitmap4 = new Bitmap(0, 0, _bitmapWidth, _bitmapHeight);
            bitmap4.Set(9, 9);
            communicationZones2[new Vector2Int(9, 9)] = bitmap4;

            var partition2 = new Partition(2, vertices2, communicationZones2);

            // Act
            partition1.CalculateIntersectionAndRatio(partition2);

            // Assert
            var key = (1, 2);

            // Verify dictionaries contain the key
            Assert.IsTrue(partition1.IntersectionZones.ContainsKey(key));
            Assert.IsTrue(partition1.CommunicationRatio.ContainsKey(key));

            // Verify ratio is zero (no intersection)
            Assert.AreEqual(0f, partition1.CommunicationRatio[key]);

            // Verify intersection bitmap is empty
            Assert.AreEqual(0, partition1.IntersectionZones[key].Count);

            // Clean up
            bitmap4.Dispose();
            partition1.IntersectionZones[key].Dispose();
        }

        [Test]
        public void CalculateIntersectionAndRatio_SkipsCalculation_WhenAlreadyCalculated()
        {
            // Arrange
            var partition1 = new Partition(1, _vertices, _communicationZones);
            var partition2 = new Partition(2, _vertices, _communicationZones);

            // Set up the intersection zones and communication ratio dictionaries manually
            var intersectionBitmap = new Bitmap(0, 0, _bitmapWidth, _bitmapHeight);
            intersectionBitmap.Set(5, 5); // Just a sample intersection

            // Initialize the properties through reflection since they are private
            var intersectionZonesField = typeof(Partition).GetProperty("IntersectionZones");
            var communicationRatioField = typeof(Partition).GetProperty("CommunicationRatio");

            // Create dictionaries with pre-calculated values
            var preCalculatedIntersections = new Dictionary<(int, int), Bitmap>
            {
                { (1, 2), intersectionBitmap }
            };
            var preCalculatedRatios = new Dictionary<(int, int), float>
            {
                { (1, 2), 0.5f }
            };

            intersectionZonesField.SetValue(partition1, preCalculatedIntersections);
            communicationRatioField.SetValue(partition1, preCalculatedRatios);

            // Act - This should skip calculation since the key (1, 2) already exists
            partition1.CalculateIntersectionAndRatio(partition2);

            // Assert - Values should remain unchanged
            Assert.AreEqual(0.5f, partition1.CommunicationRatio[(1, 2)]);
            Assert.AreEqual(intersectionBitmap, partition1.IntersectionZones[(1, 2)]);

            // Clean up
            intersectionBitmap.Dispose();
        }
    }
}