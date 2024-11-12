using System.Collections.Generic;

namespace Maes.Utilities
{
    public static class DictionaryExtensions
    {
        public static TValue? GetValueOrNull<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key)
            where TValue : struct
        {
            return dictionary.TryGetValue(key, out var value) ? value : null;
        }
    }
}