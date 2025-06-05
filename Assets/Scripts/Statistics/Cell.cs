// Copyright 2022 MAES
// 
// This file is part of MAES
// 
// MAES is free software: you can redistribute it and/or modify it under
// the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your option)
// any later version.
// 
// MAES is distributed in the hope that it will be useful, but WITHOUT
// ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
// or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General
// Public License for more details.
// 
// You should have received a copy of the GNU General Public License along
// with MAES. If not, see http://www.gnu.org/licenses/.
// 
// Contributors: Malte Z. Andreasen, Philip I. Holler and Magnus K. Jensen
// 
// Original repository: https://github.com/MalteZA/MAES

using System;

namespace Maes.Statistics
{
    public sealed class Cell
    {
        // --- Exploration over time --- 
        public readonly bool IsExplorable;
        public bool IsExplored { get; private set; }

        // --- Coverage over time  ---
        public bool IsCovered { get; private set; }
        public bool CanBeCovered { get; set; } = true;

        //  --- Heatmap ---
        public int LastExplorationTimeInTicks; // The last time that this cell was seen by a robot 
        public int LastCoverageTimeInTicks; // The last time that this cell was covered by a robot

        /// <summary>
        /// Called to register that a robot has seen this tile this tick
        /// </summary>
        public void RegisterExploration(int currentTimeTicks)
        {
#if DEBUG
            if (!IsExplorable)
            {
                throw new InvalidOperationException("Registered exploration for a tile that was marked not explorable");
            }
#endif

            LastExplorationTimeInTicks = currentTimeTicks;
            IsExplored = true;
        }

        public void RegisterCoverage(int currenTimeTicks)
        {
#if DEBUG
            if (!CanBeCovered)
            {
                throw new InvalidOperationException("Registered coverage for a tile that was marked not coverable");
            }
#endif

            LastCoverageTimeInTicks = currenTimeTicks;
            IsCovered = true;
        }

        public Cell(bool isExplorable)
        {
            IsExplorable = isExplorable;
        }
    }
}