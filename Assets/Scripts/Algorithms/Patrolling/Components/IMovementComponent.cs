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
// Contributors:
// Puvikaran Santhirasegaram
// Mads Beyer Mogensen

using Maes.Map;

using UnityEngine;

namespace Maes.Algorithms.Patrolling.Components
{
    public interface IMovementComponent : IComponent
    {
        public Vector2Int TargetPosition { get; }
        public Vertex ApproachingVertex { get; }
        public AbortingTask? AbortingTask { get; }
        public void AbortCurrentTask(AbortingTask abortingTask);

    }

    public readonly struct AbortingTask
    {
        public AbortingTask(Vertex targetVertex, bool reachedByOther)
        {
            TargetVertex = targetVertex;
            ReachedByOther = reachedByOther;
        }

        public Vertex TargetVertex { get; }
        public bool ReachedByOther { get; }
    }
}