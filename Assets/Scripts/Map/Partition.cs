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
        public IReadOnlyDictionary<Vector2Int, Bitmap> WaypointsCommunicationZones { get; }
        public IReadOnlyDictionary<int, Bitmap> IntersectionZones => _intersectionZones;
        public IReadOnlyDictionary<int, float> CommunicationRatio => _communicationRatio;
        public HashSet<Partition> NeighborPartitions { get; } = new();

        private readonly Dictionary<int, Bitmap> _intersectionZones = new();
        private readonly Dictionary<int, float> _communicationRatio = new();
        private readonly int _bitmapWidth;
        private readonly int _bitmapHeight;

        public Partition(int partitionId, IReadOnlyList<Vertex> vertices, IReadOnlyDictionary<Vector2Int, Bitmap> communicationZones)
        {
            PartitionId = partitionId;
            Vertices = vertices;
            WaypointsCommunicationZones = communicationZones;
            CommunicationZone = CreatePartitionCommunicationZone(vertices, communicationZones);
            _bitmapWidth = CommunicationZone.Width;
            _bitmapHeight = CommunicationZone.Height;
        }

        public void AddNeighborPartition(Partition partition)
        {
            if (NeighborPartitions.Contains(partition))
            {
                return;
            }
            NeighborPartitions.Add(partition);
            CalculateIntersectionAndRatio(partition);
            partition.AddNeighborPartition(this);
        }

        public void CalculateIntersectionAndRatio(Partition otherPartition)
        {
            var communicationZoneIntersection = new Bitmap(0, 0, _bitmapWidth, _bitmapHeight);

            foreach ((var position, var vertexComZone) in otherPartition.WaypointsCommunicationZones)
            {
                var intersection = Bitmap.Intersection(CommunicationZone, vertexComZone);
                if (intersection.Contains(position))
                {
                    communicationZoneIntersection.Union(intersection);
                }
            }
            _intersectionZones[otherPartition.PartitionId] = communicationZoneIntersection;

            var ratio = otherPartition.CommunicationZone.Count > 0 ? (float)communicationZoneIntersection.Count / otherPartition.CommunicationZone.Count : 0f;
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