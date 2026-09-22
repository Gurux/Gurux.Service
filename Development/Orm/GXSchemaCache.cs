using Gurux.Service.Orm.Common.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Gurux.Service.DB
{
    /// <summary>
    /// Caches table descriptions for a database connection with time-based expiration.
    /// </summary>
    /// <remarks>
    /// Schema managers using the same connection share this cache. Returned metadata
    /// is copied so callers can modify it. Changes made outside the schema manager
    /// require <see cref="Clear"/> or expiration before they become visible.
    /// Closing or reopening the connection clears cached descriptions.
    /// </remarks>
    public sealed class GXSchemaCache
    {
        private static readonly ConditionalWeakTable<DbConnection, GXSchemaCache> Caches = new();
        private readonly object gate = new();
        private readonly Dictionary<string, (GXTableSchema Schema, long Created)> items = new(StringComparer.Ordinal);
        private readonly HashSet<IDbTransaction> transactions = new();
        private TimeSpan cacheTime = TimeSpan.FromMinutes(10);
        private int operations;

        /// <summary>Initializes a cache with a lifetime of ten minutes.</summary>
        public GXSchemaCache() { }

        /// <summary>Initializes a cache with the specified lifetime.</summary>
        /// <param name="cacheTime">The nonnegative lifetime; zero disables caching.</param>
        /// <exception cref="ArgumentOutOfRangeException">The lifetime is negative.</exception>
        public GXSchemaCache(TimeSpan cacheTime) => CacheTime = cacheTime;

        /// <summary>Gets or sets the cache lifetime. Zero disables caching and clears entries.</summary>
        /// <exception cref="ArgumentOutOfRangeException">The lifetime is negative.</exception>
        public TimeSpan CacheTime
        {
            get { lock (gate) return cacheTime; }
            set
            {
                if (value < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(value));
                lock (gate)
                {
                    cacheTime = value;
                    items.Clear();
                }
            }
        }

        /// <summary>Removes all cached table descriptions.</summary>
        public void Clear()
        {
            lock (gate) items.Clear();
        }

        internal static GXSchemaCache ForConnection(DbConnection connection) =>
            Caches.GetValue(connection, static value =>
            {
                var cache = new GXSchemaCache();
                value.StateChange += (_, _) => cache.Clear();
                return cache;
            });

        internal void Invalidate(IDbTransaction? transaction)
        {
            lock (gate)
            {
                items.Clear();
                if (transaction != null) transactions.Add(transaction);
            }
        }

        internal GXTableSchema GetOrAdd(string key, Func<GXTableSchema> describe)
        {
            lock (gate)
            {
                // Do not retain metadata read after uncommitted DDL. It may be rolled back.
                if (transactions.Count != 0)
                {
                    items.Clear();
                    transactions.RemoveWhere(transaction => transaction.Connection == null);
                    if (transactions.Count != 0) return describe();
                }
                if (cacheTime == TimeSpan.Zero) return describe();
                long now = Stopwatch.GetTimestamp();
                bool Expired(long created) => (now - created) / (double)Stopwatch.Frequency >= cacheTime.TotalSeconds;
                if (++operations >= 200)
                {
                    operations = 0;
                    foreach (string expired in items.Where(item => Expired(item.Value.Created)).Select(item => item.Key).ToArray())
                        items.Remove(expired);
                }
                if (items.TryGetValue(key, out var entry) && !Expired(entry.Created)) return Copy(entry.Schema);
                items.Remove(key);
                GXTableSchema schema = describe();
                items[key] = (Copy(schema), Stopwatch.GetTimestamp());
                return schema;
            }
        }

        private static GXTableSchema Copy(GXTableSchema source)
        {
            var copy = new GXTableSchema
            {
                Catalog = source.Catalog, Schema = source.Schema, Name = source.Name,
                TableType = source.TableType, Comment = source.Comment
            };
            foreach (GXColumnSchema column in source.Columns)
            {
                copy.Columns.Add(new GXColumnSchema
                {
                    Name = column.Name, Ordinal = column.Ordinal, DbType = column.DbType, Type = column.Type,
                    MaxLength = column.MaxLength, Precision = column.Precision, Scale = column.Scale,
                    DateTimePrecision = column.DateTimePrecision, IsNullable = column.IsNullable,
                    IsPrimaryKey = column.IsPrimaryKey, IsUnique = column.IsUnique, IsGenerated = column.IsGenerated,
                    IsIdentity = column.IsIdentity, IsAutoIncrement = column.IsAutoIncrement, IsComputed = column.IsComputed,
                    DefaultValue = column.DefaultValue is Array array ? array.Clone() : column.DefaultValue,
                    ComputedExpression = column.ComputedExpression, Collation = column.Collation,
                    Comment = column.Comment, Parent = copy
                });
            }
            foreach (GXIndex index in source.Indexes)
            {
                copy.Indexes.Add(new GXIndex
                {
                    Name = index.Name, Unique = index.Unique,
                    Columns = index.Columns.Select(column => new GXIndexColumn
                    {
                        Name = column.Name, Order = column.Order, Position = column.Position
                    }).ToList()
                });
            }
            foreach (GXForeignKeySchema key in source.ForeignKeys)
            {
                copy.ForeignKeys.Add(new GXForeignKeySchema
                {
                    Name = key.Name, ReferencedSchema = key.ReferencedSchema, ReferencedTable = key.ReferencedTable,
                    OnDelete = key.OnDelete, OnUpdate = key.OnUpdate,
                    Columns = key.Columns.Select(column => new GXForeignKeyColumnSchema
                    {
                        Column = column.Column, ReferencedColumn = column.ReferencedColumn, Position = column.Position
                    }).ToList()
                });
            }
            return copy;
        }
    }
}
