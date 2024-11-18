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

using Maes.Simulation;

using UnityEngine;
using UnityEngine.UI;

namespace Maes.UI
{
    internal class ExplorationStatisticsUIController : MonoBehaviour
    {
        public Image Mask = null!;
        public Text ProgressPercentageText = null!;
        public Text ExplorationRateText = null!;

        private void SetExplorationProgress(float progress)
        {
            Mask.fillAmount = progress;
            ProgressPercentageText.text = $"{(progress * 100f):#.00}%";
        }

        // TODO: Why is this never called?
        public void UpdateStatistics(ExplorationSimulation currentExplorationSimulation)
        {
            SetExplorationProgress(currentExplorationSimulation.ExplorationTracker.ExploredProportion);
            ExplorationRateText.text = "Exploration rate (cells/minute): " +
                                       (currentExplorationSimulation.ExplorationTracker.ExploredTriangles /
                                        currentExplorationSimulation.SimulateTimeSeconds).ToString("#.0")
                                       ;
        }
    }
}