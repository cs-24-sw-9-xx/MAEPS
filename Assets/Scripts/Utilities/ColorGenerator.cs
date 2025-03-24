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
// Contributors: Puvikaran Santhirasegaram

using System.Linq;

using UnityEngine;

namespace Maes.Utilities
{
    public static class ColorGenerator
    {
        public static Color[] GenerateColors(int count)
        {
            var hueStep = 360f / count; // Evenly space hues

            return Enumerable.Range(0, count).Select(i =>
            {
                var hue = (i * hueStep) % 360;
                return Color.HSVToRGB(hue / 360f, 0.8f, 0.9f);
            }).ToArray();
        }
    }
}