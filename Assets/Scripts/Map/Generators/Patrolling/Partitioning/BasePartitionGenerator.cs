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
// Henrik van Peet,
// Mads Beyer Mogensen,
// Puvikaran Santhirasegaram

using System.Collections.Generic;

using Maes.Robot;
using Maes.Utilities;

namespace Maes.Map.Generators.Patrolling.Partitioning
{
    public interface IPartitionGenerator<TPartitionInfo> where TPartitionInfo : PartitionInfo
    {
        void SetMaps(PatrollingMap patrollingMap, CoarseGrainedMap coarseMap, RobotConstraints robotConstraints);
        Dictionary<int, TPartitionInfo> GeneratePartitions(HashSet<int> robotIds);
    }

    public abstract class BasePartitionGenerator<TPartitionInfo> : IPartitionGenerator<TPartitionInfo> where TPartitionInfo : PartitionInfo
    {
        protected PatrollingMap _patrollingMap = null!;
        protected Bitmap _collisionMap = null!;
        public void SetMaps(PatrollingMap patrollingMap, CoarseGrainedMap coarseMap, RobotConstraints robotConstraints)
        {
            _patrollingMap = patrollingMap;
            _collisionMap = MapUtilities.MapToBitMap(coarseMap);
        }
        public abstract Dictionary<int, TPartitionInfo> GeneratePartitions(HashSet<int> robotIds);
    }
}