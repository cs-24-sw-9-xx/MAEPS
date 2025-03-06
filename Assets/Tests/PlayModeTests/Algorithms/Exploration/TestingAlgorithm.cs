// Copyright 2024 MAES
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
// Contributors: Rasmus Borrisholt Schmidt, Andreas Sebastian Sørensen, Thor Beregaard, Malte Z. Andreasen, Philip I. Holler and Magnus K. Jensen,
// 
// Original repository: https://github.com/Molitany/MAES

using System.Collections.Generic;

using Maes.Algorithms;
using Maes.Algorithms.Exploration;
using Maes.Robot;

namespace Tests.PlayModeTests.Algorithms.Exploration
{
    public class TestingAlgorithm : IExplorationAlgorithm
    {
        public int Tick { get; private set; }
        public Robot2DController Controller;
        public CustomUpdateFunction UpdateFunction = (_, _) => { };

        public delegate void CustomUpdateFunction(int tick, Robot2DController controller);

        public IEnumerable<WaitForCondition> PreUpdateLogic()
        {
            while (true)
            {
                yield return WaitForCondition.ContinueUpdateLogic();
            }
        }

        public IEnumerable<WaitForCondition> UpdateLogic()
        {
            while (true)
            {
                UpdateFunction(Tick, Controller);
                Tick++;

                yield return WaitForCondition.WaitForLogicTicks(1);
            }
        }

        public void SetController(Robot2DController controller)
        {
            Controller = controller;
        }

        public string GetDebugInfo()
        {
            return "";
        }
    }
}