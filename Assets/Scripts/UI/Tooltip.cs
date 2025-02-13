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

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace Maes.UI
{
    internal class Tooltip : MonoBehaviour
    {
        // Set by Awake
        private Label _tooltip = null!;

        private static Tooltip s_instance = null!;

        private void Awake()
        {
            s_instance = this;
            _tooltip = GetComponent<UIDocument>().rootVisualElement.Q<Label>("Tooltip");
            HideTooltip();
        }

        private void ShowTooltip(string text)
        {
            _tooltip.visible = true;
            _tooltip.text = text;
        }

        private void HideTooltip()
        {
            _tooltip.visible = false;
        }

        private void Update()
        {
            // Have the tooltip follow the mouse-pointer around.
            var mousePosition = Mouse.current.position.ReadValue();
            _tooltip.style.top = Screen.height - mousePosition.y;
            _tooltip.style.left = mousePosition.x;
        }

        public static void ShowTooltip_Static(string text)
        {
            s_instance.ShowTooltip(text);
        }

        public static void HideTooltip_Static()
        {
            s_instance.HideTooltip();
        }
    }
}