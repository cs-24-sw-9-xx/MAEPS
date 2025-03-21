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