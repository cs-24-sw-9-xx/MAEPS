using System.Collections.Generic;
using System.Linq;

namespace Maes.Map.Generators.Patrolling.Partitioning
{
    public static class CombinationExtensions
    {
        public static IEnumerable<(T, T)> Combinations<T>(this T[] source)
        {
            for (var i = 0; i < source.Length; i++)
            {
                for (var j = i + 1; j < source.Length; j++)
                {
                    yield return (source[i], source[j]);
                }
            }
        }

        public static IEnumerable<(T, T)> Combinations<T>(this IEnumerable<T> source)
        {
            var sourceArray = source.ToArray();
            return sourceArray.Combinations();
        }
    }
}