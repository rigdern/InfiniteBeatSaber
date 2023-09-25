using System.Collections.Generic;

namespace InfiniteBeatSaber.Extensions
{
    internal static class DictionaryExtensions
    {
        public static void AddIfMissing<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            if (!dictionary.ContainsKey(key))
            {
                dictionary.Add(key, value);
            }
        }
    }
}
