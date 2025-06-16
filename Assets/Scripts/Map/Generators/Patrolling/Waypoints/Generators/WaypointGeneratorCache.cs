using System;
using System.Collections.Generic;

using Maes.Utilities;

using UnityEngine;

namespace Maes.Map.Generators.Patrolling.Waypoints.Generators
{
    public static class WaypointGeneratorCache<TWaypointGenerator>
    {
        /// <summary>
        /// Retrieve from or compute and add to the cache.
        /// </summary>
        /// <param name="bitmap">The map use to generate waypoints.</param>
        /// <param name="computationFunction">The function used to compute the waypoints if it isn't in the cache.</param>
        /// <param name="extraHash">Extra hashing, useful for parameters to the generator.</param>
        /// <returns>The cached or computed waypoints.</returns>
        public static HashSet<Vector2Int> Cached(Bitmap bitmap, Func<HashSet<Vector2Int>> computationFunction, Func<Hash128, Hash128>? extraHash = null)
        {
            extraHash ??= IdentityHash;
            var hash = extraHash(bitmap.Hash());

            return Cache<HashSet<Vector2Int>, TWaypointGenerator>.GetOrInsert(computationFunction, hash);
        }

        private static Hash128 IdentityHash(Hash128 hash)
        {
            return hash;
        }
    }
}