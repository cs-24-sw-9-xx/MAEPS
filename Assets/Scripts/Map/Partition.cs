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

using System;
using System.Collections.Generic;
using System.Linq;

using JetBrains.Annotations;

using Maes.Utilities;

using UnityEngine;


namespace Maes.Map
{
    public sealed class Partition
    {
        private readonly Func<IReadOnlyDictionary<Vector2Int, Bitmap>> _communicationZonesMaker;
        public int PartitionId { get; }
        public IReadOnlyList<Vertex> Vertices { get; }

        private Bitmap? _communicationZone;

        public Bitmap CommunicationZone
        {
            get
            {
                if (_communicationZone == null)
                {
                    _communicationZone = CreatePartitionCommunicationZone(Vertices, WaypointsCommunicationZones);
                }

                return _communicationZone;
            }
        }

        private IReadOnlyDictionary<Vector2Int, Bitmap>? _waypointsCommunicationZones;

        public IReadOnlyDictionary<Vector2Int, Bitmap> WaypointsCommunicationZones
        {
            get
            {
                if (_waypointsCommunicationZones == null)
                {
                    _waypointsCommunicationZones = _communicationZonesMaker();
                }

                return _waypointsCommunicationZones;
            }
        }
        public IReadOnlyDictionary<int, Bitmap> IntersectionZones => _intersectionZones;
        public IReadOnlyDictionary<int, float> CommunicationRatio => _communicationRatio;

        private readonly HashSet<Partition> _neighborPartitions = new();
        private readonly Dictionary<int, Bitmap> _intersectionZones = new();
        private readonly Dictionary<int, float> _communicationRatio = new();

        public Partition(int partitionId, IReadOnlyList<Vertex> vertices, Func<IReadOnlyDictionary<Vector2Int, Bitmap>> communicationZonesMaker)
        {
            _communicationZonesMaker = communicationZonesMaker;
            PartitionId = partitionId;
            Vertices = vertices;
        }

        public void AddNeighborPartition(Partition partition)
        {
            if (!_neighborPartitions.Add(partition))
            {
                return;
            }

            CalculateIntersectionAndRatio(partition);
            partition.AddNeighborPartition(this);
        }

        public void CalculateIntersectionAndRatio(Partition otherPartition)
        {
            var communicationZoneIntersection = new Bitmap(CommunicationZone.Width, CommunicationZone.Height);

            foreach (var (position, vertexComZone) in otherPartition.WaypointsCommunicationZones)
            {
                using var intersection = Bitmap.Intersection(CommunicationZone, vertexComZone);
                communicationZoneIntersection.Union(intersection);
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
            var partitionCommunicationZone = new Bitmap(firstBitmap.Width, firstBitmap.Height);

            foreach (var vertex in vertices)
            {
                var zoneBitmap = communicationZones[vertex.Position];
                partitionCommunicationZone.Union(zoneBitmap);
            }

            return partitionCommunicationZone;
        }
    }
}