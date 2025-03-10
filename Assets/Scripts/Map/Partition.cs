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
using System;
using System.Collections.Generic;
using System.Linq;

using Maes.Utilities;

using UnityEngine;


namespace Maes.Map
{
    public class Partition
    {
        public List<Vertex> Vertices { get; private set; }
        public Bitmap CommunicationZone { get; private set; }
        public int PartitionId { get; }
        public Dictionary<(int, int), float> CommunicationRatio { get; private set; }
        public Dictionary<(int, int), Bitmap> IntersectionZones { get; private set; }
        public Dictionary<Vector2Int, Bitmap> CommunicationZones { get; private set; }

        public Partition(int partitionId, List<Vertex> vertices, Dictionary<Vector2Int, Bitmap> communicationZones)
        {
            PartitionId = partitionId;
            Vertices = vertices;
            CommunicationZones = communicationZones;
            CommunicationZone = CreatePartitionCommunicationZone(communicationZones);
            IntersectionZones = new Dictionary<(int, int), Bitmap>();
            CommunicationRatio = new Dictionary<(int, int), float>();
        }

        public void CalculateIntersectionAndRatio(Partition otherPartition)
        {
            var key = (PartitionId, otherPartition.PartitionId);

            if (AlreadyCalculated(key))
            {
                return;
            }
            var otherPartitionCommuncationZoneSubset = new Bitmap(0, 0, otherPartition.CommunicationZone.Width, otherPartition.CommunicationZone.Height);
            foreach (var otherPartitionVertex in otherPartition.Vertices)
            {
                otherPartition.CommunicationZones.TryGetValue(otherPartitionVertex.Position, out var zoneBitmap);
                if (zoneBitmap == null)
                {
                    throw new ArgumentException($"No communication zone found for vertex at position {otherPartitionVertex.Position}");
                }
                foreach (var currentPartitionVertex in Vertices)
                {
                    if (zoneBitmap.Contains(currentPartitionVertex.Position))
                    {
                        otherPartitionCommuncationZoneSubset.Union(zoneBitmap);
                    }
                }
            }

            var intersection = Bitmap.Intersection(CommunicationZone, otherPartitionCommuncationZoneSubset);
            IntersectionZones[key] = intersection;

            var ratio = CalculateRatio(intersection.Count, otherPartition.CommunicationZone.Count);
            CommunicationRatio[key] = ratio;
        }

        private Bitmap CreatePartitionCommunicationZone(Dictionary<Vector2Int, Bitmap> communicationZones)
        {
            if (communicationZones.Count == 0)
            {
                throw new ArgumentException("Communication zones dictionary is empty.", nameof(communicationZones));
            }

            // Extract dimensions from the first available Bitmap
            var firstBitmap = communicationZones.First().Value;
            var partitionCommunicationZone = new Bitmap(0, 0, firstBitmap.Width, firstBitmap.Height);

            foreach (var vertex in Vertices)
            {
                if (communicationZones.TryGetValue(vertex.Position, out var zoneBitmap))
                {
                    partitionCommunicationZone.Union(zoneBitmap);
                }
                else
                {
                    throw new ArgumentException($"No communication zone found for vertex at position {vertex.Position}");
                }
            }

            return partitionCommunicationZone;
        }

        private bool AlreadyCalculated((int, int) key)
        {
            return IntersectionZones.ContainsKey(key);
        }

        private float CalculateRatio(int intersectionCount, int totalCount)
        {
            return totalCount > 0 ? (float)intersectionCount / totalCount : 0f;
        }
    }
}