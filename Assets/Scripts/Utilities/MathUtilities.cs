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

using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace Maes.Utilities
{
    public static class MathUtilities
    {
        public static T Clamp<T>(T val, T min, T max) where T : IComparable<T>
        {
            if (val.CompareTo(min) < 0)
            {
                return min;
            }

            if (val.CompareTo(max) > 0)
            {
                return max;
            }

            return val;
        }

        public static double StandardDeviation(Dictionary<(Vector2Int, Vector2Int), int> distanceMatrix)
        {
            var distances = distanceMatrix.Values.ToList();
            return Math.Sqrt(distances.Average(d => Math.Pow(d - distances.Average(), 2)));
        }

        public static double GaussianKernel(double distance, double sigma)
        {
            return Math.Exp(-Math.Pow(distance, 2) / (2 * Math.Pow(sigma, 2)));
        }
    }
}