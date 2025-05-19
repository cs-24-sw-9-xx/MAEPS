using System;
using System.Collections.Generic;

using Maes.Utilities;

using UnityEngine;

namespace Maes.Map.Generators.Patrolling.Waypoints.Generators
{
    public static class WaypointGeneratorCache<TWaypointGenerator>
    {
        // Collisions may happen but the chance is so low we can ignore it.
        // ReSharper disable once StaticMemberInGenericType
        private static readonly Dictionary<Hash128, HashSet<Vector2Int>> Cache = new();

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

            if (Cache.TryGetValue(hash, out var cachedWaypoints))
            {
                // We have a cache hit!
                // Return a copy
                return new HashSet<Vector2Int>(cachedWaypoints);
            }

            // We don't have a hit.
            // Do the expensive computation and add it to the cache.
            var waypoints = computationFunction();
            Cache.Add(hash, new HashSet<Vector2Int>(waypoints));
            return waypoints;
        }

        private static Hash128 IdentityHash(Hash128 hash)
        {
            return hash;
        }
    }
}