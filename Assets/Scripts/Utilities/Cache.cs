using System;
using System.Collections.Generic;

using UnityEngine;

namespace Maes.Utilities
{
    public static class Cache<TCached, TKey>
    {
        private static readonly Dictionary<Hash128, TCached> CacheDictionary = new Dictionary<Hash128, TCached>();

        public static TCached GetOrInsert(Func<TCached> factory, Hash128 hash)
        {
            if (CacheDictionary.TryGetValue(hash, out var cached))
            {
                return cached;
            }

            var valueToCache = factory();
            CacheDictionary.Add(hash, valueToCache);

            return valueToCache;
        }
    }
}