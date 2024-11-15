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
    public abstract class Cell
    {
        // --- Exploration over time --- 
        public bool IsExplorable { get; }
        public bool IsExplored { get; private set; }

        // --- Coverage over time  ---
        public bool IsCovered { get; private set; }
        public bool CanBeCovered { get; set; } = true;

        // --- Redundant Coverage ---
        public int CoverageTimeInTicks; // The amount of ticks that this cell has been covered
        public int ExplorationTimeInTicks; // The amount of ticks that this has been explored

        //  --- Heatmap ---
        public int LastExplorationTimeInTicks; // The last time that this cell was seen by a robot 
        public int LastCoverageTimeInTicks; // The last time that this cell was covered by a robot

        /// <summary>
        /// Called to register that a robot has seen this tile this tick
        /// </summary>
        public void RegisterExploration(int currentTimeTicks)
        {
            if (!IsExplorable)
            {
                throw new Exception("Registered exploration for a tile that was marked not explorable");
            }

            ExplorationTimeInTicks += 1;
            LastExplorationTimeInTicks = currentTimeTicks;
            IsExplored = true;
        }

        public void RegisterCoverage(int currenTimeTicks)
        {
            if (!CanBeCovered)
            {
                throw new Exception("Registered coverage for a tile that was marked not coverable");
            }

            CoverageTimeInTicks += 1;
            LastCoverageTimeInTicks = currenTimeTicks;
            IsCovered = true;
        }

        protected Cell(bool isExplorable)
        {
            IsExplorable = isExplorable;
        }
    }

    public class ExplorationCell : Cell
    {
        public ExplorationCell(bool isExplorable) : base(isExplorable)
        {
        }
    }

    public class PatrollingCell : Cell
    {
        public PatrollingCell(bool isExplorable) : base(isExplorable)
        {
        }
    }
}