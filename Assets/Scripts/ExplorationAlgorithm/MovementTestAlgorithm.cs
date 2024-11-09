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
// Contributors: Rasmus Borrisholt Schmidt, Andreas Sebastian Sørensen, Thor Beregaard
// 
// Original repository: https://github.com/Molitany/MAES

using Maes.Algorithms;
using Maes.Robot;

using UnityEngine;

namespace Maes.ExplorationAlgorithm
{
    public class MovementTestAlgorithm : IExplorationAlgorithm
    {
        // Set by SetController
        private Robot2DController _controller = null!;
        
        private readonly Vector2Int _targetTile;
        
        public MovementTestAlgorithm(Vector2Int targetTile)
        {
            _targetTile = targetTile;
        }

        public string GetDebugInfo()
        {
            return _controller.GetStatus().ToString();
        }

        public void SetController(Robot2DController controller)
        {
            _controller = controller;
        }

        public void UpdateLogic()
        {
            _controller.MoveTo(_targetTile);
        }
    }
}