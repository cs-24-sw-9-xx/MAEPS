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
// Contributors: Mads Beyer Mogensen

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Maes.Algorithms.Patrolling.Components
{
    /// <summary>
    /// A component that can be used in <see cref="PatrollingAlgorithm"/> to add functionality.
    /// </summary>
    public interface IComponent
    {
        [SuppressMessage("ReSharper", "IteratorNeverReturns")]
        IEnumerable<ComponentWaitForCondition> PreUpdateLogic()
        {
            while (true)
            {
                yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: true);
            }
        }

        [SuppressMessage("ReSharper", "IteratorNeverReturns")]
        IEnumerable<ComponentWaitForCondition> PostUpdateLogic()
        {
            while (true)
            {
                yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: true);
            }
        }

        void DebugInfo(StringBuilder stringBuilder)
        {
            // Intentionally left blank.
            // Don't force components to implement this.
        }

        /// <summary>
        /// The order the component's <see cref="PreUpdateLogic"/> method is executed in comparison to the other components.
        /// </summary>
        int PreUpdateOrder { get; }

        /// <summary>
        /// The order the component's <see cref="PostUpdateLogic"/> method is executed in comparison to the other components.
        /// </summary>
        int PostUpdateOrder { get; }
    }
}