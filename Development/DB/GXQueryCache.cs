using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Gurux.Service.DB
{
    /// <summary>
    /// Provides a thread-safe cache for database query strings with time-based expiration.
    /// </summary>
    /// <remarks>
    /// This class caches generated SQL query strings to improve performance by avoiding repeated query generation.
    /// Cached items automatically expire after the configured <see cref="CacheTime"/> duration.
    /// The cache performs automatic cleanup every 200 operations to remove expired entries.
    /// </remarks>
    /// <example>
    /// <code>
    /// var cache = new GXQueryCache(TimeSpan.FromMinutes(5));
    /// 
    /// // Build a cache key from query parameters
    /// string key = cache.BuildKey("SelectUser", userId, userName);
    /// 
    /// // Try to get cached query
    /// if (!cache.TryGet(key, out string query))
    /// {
    ///     // Generate new query
    ///     query = GenerateQuery();
    ///     cache.Set(key, query);
    /// }
    /// </code>
    /// </example>
    public class GXQueryCache
    {
        /// <summary>
        /// Provides reference equality comparison for cache keys to detect circular references.
        /// </summary>
        sealed class ReferenceComparer : IEqualityComparer<object>
        {
            /// <summary>
            /// Determines whether the specified objects are the same instance.
            /// </summary>
            /// <param name="x">The first object to compare.</param>
            /// <param name="y">The second object to compare.</param>
            /// <returns>true if the objects are the same instance; otherwise, false.</returns>
            public new bool Equals(object x, object y)
            {
                return ReferenceEquals(x, y);
            }

            /// <summary>
            /// Returns a hash code based on object identity.
            /// </summary>
            /// <param name="obj">The object for which to get a hash code.</param>
            /// <returns>A hash code for the specified object.</returns>
            public int GetHashCode(object obj)
            {
                return RuntimeHelpers.GetHashCode(obj);
            }
        }

        /// <summary>
        /// Represents a cached query string with its expiration time.
        /// </summary>
        sealed class GXCacheItem
        {
            /// <summary>
            /// The cached query string value.
            /// </summary>
            public string Value;

            /// <summary>
            /// The UTC time when this cache entry expires.
            /// </summary>
            public DateTime Expires;

            /// <summary>
            /// The original string key used for hash-collision detection.
            /// </summary>
            public string Key;
        }

        /// <summary>
        /// Thread-safe dictionary storing cached query strings, keyed by the string key's hash code.
        /// </summary>
        readonly ConcurrentDictionary<int, GXCacheItem> Items =
            new ConcurrentDictionary<int, GXCacheItem>();

        /// <summary>
        /// Per-thread reusable StringBuilder to avoid allocations in BuildKey.
        /// </summary>
        [ThreadStatic]
        private static StringBuilder _keyBuilder;

        /// <summary>
        /// Returns the thread-local StringBuilder, cleared and ready for use.
        /// </summary>
        private static StringBuilder AcquireBuilder()
        {
            StringBuilder sb = _keyBuilder;
            if (sb == null)
            {
                sb = new StringBuilder(256);
                _keyBuilder = sb;
            }
            else
            {
                sb.Clear();
            }
            return sb;
        }

        /// <summary>
        /// Counter for tracking operations to trigger periodic cleanup.
        /// </summary>
        int OperationCount;

        /// <summary>
        /// The duration for which cache entries remain valid.
        /// </summary>
        TimeSpan cacheTime = TimeSpan.FromMinutes(10);

        /// <summary>
        /// Initializes a new instance of the <see cref="GXQueryCache"/> class with a default cache time of 10 minutes.
        /// </summary>
        public GXQueryCache()
        {
            CacheTime = TimeSpan.FromMinutes(10);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GXQueryCache"/> class with a cache time.
        /// </summary>
        /// <param name="cacheTime">The cache time.</param>
        public GXQueryCache(TimeSpan cacheTime)
        {
            CacheTime = cacheTime;
        }


        /// <summary>
        /// Removes all entries from the cache.
        /// </summary>
        /// <remarks>
        /// This method is thread-safe and can be called while other operations are in progress.
        /// </remarks>
        public void Clear()
        {
            Items.Clear();
        }

        /// <summary>
        /// Gets or sets the duration for which cache entries remain valid.
        /// </summary>
        /// <value>
        /// A <see cref="TimeSpan"/> representing the cache duration. 
        /// Setting this to <see cref="TimeSpan.Zero"/> disables caching and clears all existing entries.
        /// </value>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the value is negative.
        /// </exception>
        /// <example>
        /// <code>
        /// cache.CacheTime = TimeSpan.FromMinutes(5);  // Cache for 5 minutes
        /// cache.CacheTime = TimeSpan.Zero;            // Disable caching
        /// </code>
        /// </example>
        public TimeSpan CacheTime
        {
            get
            {
                return cacheTime;
            }
            set
            {
                if (value < TimeSpan.Zero)
                {
                    throw new ArgumentOutOfRangeException("CacheTime", "Cache time can't be negative.");
                }
                cacheTime = value;
                if (cacheTime == TimeSpan.Zero)
                {
                    Clear();
                }
            }
        }

        /// <summary>
        /// Attempts to retrieve a cached value for the specified key.
        /// </summary>
        /// <param name="key">The cache key to look up.</param>
        /// <param name="value">
        /// When this method returns, contains the cached value if found and not expired; 
        /// otherwise, null. This parameter is passed uninitialized.
        /// </param>
        /// <returns>
        /// true if the key was found in the cache and has not expired; otherwise, false.
        /// </returns>
        /// <remarks>
        /// If caching is disabled (<see cref="CacheTime"/> is <see cref="TimeSpan.Zero"/>), this method always returns false.
        /// Expired entries are automatically removed from the cache.
        /// </remarks>
        internal bool TryGet(string key, out string value)
        {
            if (CacheTime == TimeSpan.Zero)
            {
                value = null;
                return false;
            }
            CleanupIfNeeded();
            if (key != null)
            {
                int hash = key.GetHashCode();
                if (Items.TryGetValue(hash, out GXCacheItem it) && it.Key == key)
                {
                    if (it.Expires > DateTime.UtcNow)
                    {
                        value = it.Value;
                        return true;
                    }
                    Items.TryRemove(hash, out _);
                }
            }
            value = null;
            return false;
        }

        /// <summary>
        /// Stores a value in the cache with the specified key.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="value">The value to cache.</param>
        /// <remarks>
        /// If caching is disabled (<see cref="CacheTime"/> is <see cref="TimeSpan.Zero"/>), this method does nothing.
        /// If the key is null, the value is not cached.
        /// If the key already exists, its value and expiration time are updated.
        /// </remarks>
        internal void Set(string key, string value)
        {
            if (CacheTime == TimeSpan.Zero)
            {
                return;
            }
            CleanupIfNeeded();
            if (key == null)
            {
                return;
            }
            int hash = key.GetHashCode();
            Items[hash] = new GXCacheItem()
            {
                Key = key,
                Value = value,
                Expires = DateTime.UtcNow.Add(CacheTime)
            };
        }

        /// <summary>
        /// Builds a cache key from a name and a collection of parameter values.
        /// </summary>
        /// <param name="name">The base name for the cache key (typically the query type).</param>
        /// <param name="values">Optional parameters that affect the query generation.</param>
        /// <returns>A string that uniquely identifies the combination of name and parameters.</returns>
        /// <remarks>
        /// The key is constructed by combining the name with signatures of all parameter values,
        /// separated by '|' characters. Complex objects are analyzed recursively to create unique signatures.
        /// </remarks>
        /// <example>
        /// <code>
        /// string key = cache.BuildKey("SelectUser", userId, includeDeleted);
        /// // Result: "SelectUser|f:123|f:True"
        /// </code>
        /// </example>
        internal string BuildKey(string name, params object[] values)
        {
            StringBuilder sb = AcquireBuilder();
            sb.Append(name);
            if (values != null)
            {
                HashSet<object> visited = new HashSet<object>(new ReferenceComparer());
                foreach (object it in values)
                {
                    sb.Append('|');
                    AppendSignature(sb, it, visited, 0);
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Computes a content-based hash code for the specified value without allocating a signature string.
        /// </summary>
        internal int GetHash(object value)
        {
            StringBuilder sb = AcquireBuilder();
            HashSet<object> visited = new HashSet<object>(new ReferenceComparer());
            AppendSignature(sb, value, visited, 0);
            // Compute FNV-1a hash directly over the StringBuilder to avoid a string allocation.
            int hash = unchecked((int)2166136261);
            for (int i = 0; i < sb.Length; i++)
            {
                hash ^= sb[i];
                hash = unchecked(hash * 16777619);
            }
            return hash;
        }

        /// <summary>
        /// Appends a unique signature for <paramref name="value"/> into <paramref name="sb"/>,
        /// reusing the same StringBuilder to avoid intermediate string allocations.
        /// </summary>
        private void AppendSignature(StringBuilder sb, object value, HashSet<object> visited, int depth)
        {
            if (value == null) { sb.Append("null"); return; }
            if (depth > 64) { sb.Append("max-depth"); return; }
            if (value is string s) { sb.Append("s:"); sb.Append(s); return; }
            if (value is Type type) { sb.Append("t:"); sb.Append(type.FullName); return; }
            if (value is MemberInfo member)
            {
                sb.Append("m:");
                sb.Append(member.DeclaringType?.FullName);
                sb.Append('.');
                sb.Append(member.Name);
                return;
            }
            if (value is LambdaExpression lambda)
            {
                sb.Append("l:");
                sb.Append(lambda.ReturnType.FullName);
                sb.Append(':');
                sb.Append(lambda.ToString());
                return;
            }
            if (value is Expression expression)
            {
                sb.Append("e:");
                sb.Append(expression.NodeType);
                sb.Append(':');
                sb.Append(expression.Type.FullName);
                sb.Append(':');
                sb.Append(expression.ToString());
                return;
            }
            Type vt = value.GetType();
            if (!vt.IsValueType && !visited.Add(value))
            {
                sb.Append("ref:");
                sb.Append(RuntimeHelpers.GetHashCode(value).ToString(CultureInfo.InvariantCulture));
                return;
            }
            if (vt.IsGenericType && vt.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
            {
                object kvKey = vt.GetProperty("Key")?.GetValue(value, null);
                object kvVal = vt.GetProperty("Value")?.GetValue(value, null);
                sb.Append("kv(");
                AppendSignature(sb, kvKey, visited, depth + 1);
                sb.Append(',');
                AppendSignature(sb, kvVal, visited, depth + 1);
                sb.Append(')');
                return;
            }
            if (value is IDictionary dictionary)
            {
                // Entries must be sorted for deterministic keys; build each entry as a temp string.
                List<string> entries = new List<string>();
                foreach (DictionaryEntry it in dictionary)
                {
                    StringBuilder tmp = new StringBuilder();
                    AppendSignature(tmp, it.Key, visited, depth + 1);
                    tmp.Append("=>");
                    AppendSignature(tmp, it.Value, visited, depth + 1);
                    entries.Add(tmp.ToString());
                }
                entries.Sort(StringComparer.Ordinal);
                sb.Append("d:[");
                for (int i = 0; i < entries.Count; i++)
                {
                    if (i > 0) sb.Append(';');
                    sb.Append(entries[i]);
                }
                sb.Append(']');
                return;
            }
            if (value is IEnumerable enumerable)
            {
                sb.Append('[');
                bool first = true;
                foreach (object it in enumerable)
                {
                    if (!first) sb.Append(';');
                    first = false;
                    AppendSignature(sb, it, visited, depth + 1);
                }
                sb.Append(']');
                return;
            }
            if (value is IFormattable formattable)
            {
                sb.Append("f:");
                sb.Append(formattable.ToString(null, CultureInfo.InvariantCulture));
                return;
            }
            sb.Append("o:");
            sb.Append(vt.FullName);
            sb.Append(':');
            sb.Append(RuntimeHelpers.GetHashCode(value).ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Computes a hash code for a settings object by examining all its public properties.
        /// </summary>
        /// <param name="settings">The settings object to hash.</param>
        /// <returns>
        /// A hash code representing the combined values of all readable public properties.
        /// Returns 0 if settings is null.
        /// </returns>
        /// <remarks>
        /// Properties are sorted by name before hashing to ensure consistent results.
        /// Properties that cannot be read or throw exceptions are skipped.
        /// Indexed properties are ignored.
        /// </remarks>
        /// <example>
        /// <code>
        /// var settings = new DatabaseSettings { Timeout = 30, RetryCount = 3 };
        /// int hash = cache.GetSettingsHash(settings);
        /// </code>
        /// </example>
        internal int GetSettingsHash(object settings)
        {
            if (settings == null)
            {
                return 0;
            }
            int hash = 23;
            PropertyInfo[] properties = settings.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            Array.Sort(properties, (a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
            foreach (PropertyInfo it in properties)
            {
                if (!it.CanRead || it.GetIndexParameters().Length != 0)
                {
                    continue;
                }
                object value;
                try
                {
                    value = it.GetValue(settings, null);
                }
                catch
                {
                    continue;
                }
                unchecked
                {
                    hash = hash * 31 + GetHash(value);
                }
            }
            return hash;
        }

        /// <summary>
        /// Performs periodic cleanup of expired cache entries.
        /// </summary>
        /// <remarks>
        /// This method is called automatically on every cache operation.
        /// Cleanup is performed every 200 operations to balance performance and memory usage.
        /// All entries with expiration times in the past are removed from the cache.
        /// </remarks>
        private void CleanupIfNeeded()
        {
            int count = System.Threading.Interlocked.Increment(ref OperationCount);
            if (count % 200 != 0)
            {
                return;
            }
            DateTime now = DateTime.UtcNow;
            foreach (var it in Items)
            {
                if (it.Value.Expires <= now)
                {
                    Items.TryRemove(it.Key, out _);
                }
            }
        }
    }
}