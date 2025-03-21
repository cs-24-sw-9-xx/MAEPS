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
// Casper Nyvang Sørensen,
// Christian Ziegler Sejersen,
// Jakob Meyer Olsen
// Mads Beyer Mogensen

using System.Collections.Generic;
using System.Linq;

using JetBrains.Annotations;

using Maes.Utilities;

using UnityEngine;


namespace Maes.Map
{
    public sealed class Partition
    {
        public int PartitionId { get; }
        public IReadOnlyList<Vertex> Vertices { get; }
        public Bitmap CommunicationZone { get; }
        public IReadOnlyDictionary<Vector2Int, Bitmap> CommunicationZones { get; }
        public IReadOnlyDictionary<int, Bitmap> IntersectionZones => _intersectionZones;
        public IReadOnlyDictionary<int, float> CommunicationRatio => _communicationRatio;

        private readonly Dictionary<int, Bitmap> _intersectionZones = new();
        private readonly Dictionary<int, float> _communicationRatio = new();

        public Partition(int partitionId, IReadOnlyList<Vertex> vertices, IReadOnlyDictionary<Vector2Int, Bitmap> communicationZones)
        {
            PartitionId = partitionId;
            Vertices = vertices;
            CommunicationZones = communicationZones;
            CommunicationZone = CreatePartitionCommunicationZone(vertices, communicationZones);
        }

        public void CalculateIntersectionAndRatio(Partition otherPartition)
        {
            var intersection = Bitmap.Intersection(CommunicationZone, otherPartition.CommunicationZone);
            _intersectionZones[otherPartition.PartitionId] = intersection;

            var ratio = otherPartition.CommunicationZone.Count > 0 ? (float)intersection.Count / otherPartition.CommunicationZone.Count : 0f;
            _communicationRatio[otherPartition.PartitionId] = ratio;
        }

        [MustDisposeResource]
        private static Bitmap CreatePartitionCommunicationZone(IReadOnlyList<Vertex> vertices, IReadOnlyDictionary<Vector2Int, Bitmap> communicationZones)
        {
            // Extract dimensions from the first available Bitmap
            var firstBitmap = communicationZones.First().Value;
            var partitionCommunicationZone = new Bitmap(0, 0, firstBitmap.Width, firstBitmap.Height);

            foreach (var vertex in vertices)
            {
                var zoneBitmap = communicationZones[vertex.Position];
                partitionCommunicationZone.Union(zoneBitmap);
            }

            return partitionCommunicationZone;
        }
    }
}