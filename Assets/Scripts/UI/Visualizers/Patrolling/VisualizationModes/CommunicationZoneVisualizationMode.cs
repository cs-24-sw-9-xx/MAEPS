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
// Henrik van Peet
// Mads Beyer Mogensen
// Puvikaran Santhirasegaram


namespace Maes.UI.Visualizers.Patrolling.VisualizationModes
{
    public class CommunicationZoneVisualizationMode : IPatrollingVisualizationMode
    {
        private readonly int _selectedVertexId;
        private bool _isFirstUpdate = true;

        public CommunicationZoneVisualizationMode(int selectedVertexId)
        {
            _selectedVertexId = selectedVertexId;
        }
        public void UpdateVisualization(PatrollingVisualizer visualizer, int currentTick)
        {
            // Nothing to update since the visualization does not change
            if (_isFirstUpdate)
            {
                var communicationZone = visualizer.CommunicationZoneVertices.CommunicationZoneTiles[_selectedVertexId];
                visualizer.SetAllColors(communicationZone, PatrollingVisualizer.CommunicationColor, Visualizer.StandardCellColor);

                _isFirstUpdate = false;
            }
        }
    }
}