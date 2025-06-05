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
// Henrik van Peet,
// Mads Beyer Mogensen,
// Puvikaran Santhirasegaram
// Casper Nyvang Sørensen,
// Christian Ziegler Sejersen,
// Jakob Meyer Olsen

using Maes.Map;
using Maes.Map.Generators;

namespace Tests.PlayModeTests.Utilities.MapInterpreter.MapBuilder
{
    public class SimulationMapBuilder : BaseSimulationMapBuilder<SimulationMap<Tile>>
    {
        public SimulationMapBuilder(string map) : base(map)
        {

        }

        protected override SimulationMap<Tile> BuildResult(SimulationMap<Tile> map)
        {
            return map;
        }
    }
}