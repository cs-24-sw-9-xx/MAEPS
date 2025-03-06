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

using Maes.Simulation.Exploration;
using Maes.UI;

using QuickOutline;

using UnityEngine;
using UnityEngine.EventSystems;

namespace Maes.Map
{
    public class VisibleTagInfoHandler : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public Outline outline = null!;
        public ExplorationSimulation Simulation = null!;

        // Set by SetTag
        private EnvironmentTag _environmentTag = null!;
        // Set by SetTag
        private OnVisibleTagSelectedDelegate _onVisibleTagSelected = null!;

        private delegate void OnVisibleTagSelectedDelegate(VisibleTagInfoHandler t);

        public void SetTag(EnvironmentTag t)
        {
            _environmentTag = t;
            _onVisibleTagSelected = Simulation.SetSelectedTag;
        }

        public string GetDebugInfo()
        {
            return _environmentTag.GetDebugInfo();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            _onVisibleTagSelected(this);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            Tooltip.ShowTooltip_Static(_environmentTag.ToString());
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Tooltip.HideTooltip_Static();
        }
    }
}