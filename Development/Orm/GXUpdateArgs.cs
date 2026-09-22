//
// --------------------------------------------------------------------------
//  Gurux Ltd
//
//
//
// Filename:        $HeadURL$
//
// Version:         $Revision$,
//                  $Date$
//                  $Author$
//
// Copyright (c) Gurux Ltd
//
//---------------------------------------------------------------------------
//
//  DESCRIPTION
//
// This file is a part of Gurux Device Framework.
//
// Gurux Device Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Device Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
//
// This code is licensed under the GNU General Public License v2.
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------

using Gurux.Common.Internal;
using Gurux.Orm.Internal.Enums;
using Gurux.Service.DB;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Model;
using Gurux.Service.Orm.Internal;
using Gurux.Service.Orm.Settings;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Gurux.Service.Orm
{
    /// <summary>
    /// Select arguments.
    /// </summary>
    public class GXUpdateArgs
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal GXSettingsArgs Parent = new GXSettingsArgs();

        /// <summary>
        /// List of values to update.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal List<KeyValuePair<object, LambdaExpression?>> Values = new();

        /// <summary>
        /// List of values to exlude from update.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal List<KeyValuePair<Type, LambdaExpression>> Excluded = new List<KeyValuePair<Type, LambdaExpression>>();

        /// <summary>
        /// Constructor.
        /// </summary>
        private GXUpdateArgs()
        {
            Joins = new GXJoinCollection(Parent);
            Where = new GXWhereCollection(Parent, Joins);
        }

        /// <summary>
        /// Clear all update settings.
        /// </summary>
        public void Clear()
        {
            Parent.Clear();
            Values.Clear();
            Joins.List.Clear();
            Where.Clear();
        }

        /// <summary>
        /// Database settings.
        /// </summary>
        public GXDBSettings Settings
        {
            get
            {
                return Parent.Settings;
            }
            internal set
            {
                Parent.Settings = value;
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return ToString(true);
        }

        /// <summary>
        /// Update argument as SQL string.
        /// </summary>
        /// <param name="addGenerationTime">Is SQL generation time included in the output.</param>
        /// <returns>The generated SQL update statement.</returns>
        public string ToString(bool addGenerationTime)
        {
            string sql;
            string cacheKey = Parent.QueryCache.BuildKey(
                Parent.Settings.Type,
                Values,
                Excluded,
                Where.GetItemHash(),
                Joins.GetItemHash(),
                Count);
            if (Parent.QueryCache.TryGet(cacheKey, out string? cachedSql, out int generationTime))
            {
                GenerationTime = generationTime;
                Debug.WriteLine($"Cached SQL: {GenerationTime} ms {cachedSql}");
                sql = cachedSql!;
            }
            else
            {
                if (!Values.Any())
                {
                    return "";
                }
                var sw = Stopwatch.StartNew();
                GXGetMembersArgs args = new GXGetMembersArgs(Parent.Settings, TargetType.Column)
                {
                    StringBuilder = new StringBuilder(),
                    SingleTable = true,
                };
                Type? table = null;
                List<string>? excluded = null;
                sql = Where.ToString();
                foreach (KeyValuePair<object, LambdaExpression> it in Values)
                {
                    if (it.Key is GXSelectArgs)
                    {
                        //Get type.
                        Type type = GXDbHelpers.GetType(it.Value);
                        table = type;
                    }
                    else
                    {
                        table = it.Key.GetType();
                    }
                    excluded = GXDbHelpers.ExcludedProperties(Excluded, args, table);
                    string tmp = GXDbHelpers.ConvertToString(Parent.Settings, TargetType.Table, null, table, null);
                    if (args.StringBuilder.Length != 0)
                    {
                        //Add space after the first row.
                        args.StringBuilder.Append(' ');
                    }
                    args.StringBuilder.Append("UPDATE ");
                    args.StringBuilder.Append(tmp);
                    args.StringBuilder.Append(" SET ");
                    Dictionary<string, GXSerializedItem> properties = GXSqlBuilder.GetProperties(table);
                    string[]? members;
                    if (it.Value != null)
                    {
                        //Add selected values.
                        args.Expression = it.Value;
                        members = GXDbHelpers.GetMemberList(args);
                        if (members?.Any() != true)
                        {
                            return "";
                        }
                    }
                    else
                    {
                        members = properties.Keys.ToArray();
                    }
                    foreach (var name in members)
                    {
                        object? value;
                        var p = properties[name];
                        //Primary key is not updated.
                        if ((p.Attributes & (Attributes.PrimaryKey | Attributes.AutoIncrement | Attributes.NewGuid | Attributes.CurrentTimestamp)) == 0 &&
                            p.Relation?.RelationType != RelationType.OneToMany &&
                            excluded!.Contains(name) != true)
                        {
                            GXSelectArgs? a = it.Key as GXSelectArgs;
                            if (a == null)
                            {
                                value = p.Get(it.Key);
                                if (value != null && p.Relation?.RelationType == RelationType.OneToOne &&
                                    !GXInternal.IsGenericDataType(value.GetType()))
                                {
                                    value = p.Relation.ForeignId.Get(value);
                                }
                                if (value == null && (p.Attributes & Attributes.DefaultValue) != 0)
                                {
                                    //Use default value if value is null and default value is defined.
                                    value = p.DefaultValue;
                                }
                            }
                            else
                            {
                                value = a.ToString(false);
                            }
                            tmp = GXDbHelpers.ConvertToString(Parent.Settings, TargetType.Column, null, name, null);
                            args.StringBuilder.Append(tmp);
                            args.StringBuilder.Append(" = ");
                            bool empty = false;
                            if (value is IEnumerable e &&
                                !GXInternal.IsGenericDataType(value.GetType()))
                            {
                                empty = true;
                                foreach (var item in e)
                                {
                                    if (item != null)
                                    {
                                        var s = GXSqlBuilder.FindUnique(item.GetType());
                                        var tmp3 = s.Get(item);
                                        if (!GXDbHelpers.IsZero(tmp3))
                                        {
                                            tmp = GXDbHelpers.ConvertToString(Parent.Settings, TargetType.Value, null, tmp3, null);
                                            empty = false;
                                        }
                                    }
                                }
                                if (empty)
                                {
                                    excluded!.Add(name);
                                }
                            }
                            else
                            {
                                if (a == null)
                                {
                                    if (value == null)
                                    {
                                        tmp = "NULL";
                                    }
                                    else
                                    {
                                        tmp = GXDbHelpers.ConvertToString(Parent.Settings, TargetType.Value, null, value, null);
                                    }
                                    args.StringBuilder.Append(tmp);
                                    args.StringBuilder.Append(", ");
                                }
                            }
                        }
                    }
                    args.StringBuilder.Length -= 2;
                    if (string.IsNullOrEmpty(sql) && !(it.Key is GXSelectArgs))
                    {
                        //Get ID if where expression is not defined.
                        var s = GXSqlBuilder.FindUnique(it.Key.GetType());
                        if (s != null)
                        {
                            var tmp3 = s.Get(it.Key);
                            args.StringBuilder.Append(" WHERE ");
                            tmp = GXDbHelpers.ConvertToString(Parent.Settings, TargetType.Column, null, s.Target, null);
                            args.StringBuilder.Append(tmp);
                            args.StringBuilder.Append(" = ");
                            tmp = GXDbHelpers.ConvertToString(Parent.Settings, TargetType.Value, null, tmp3, null);
                            args.StringBuilder.Append(tmp);
                        }
                    }
                }
                if (!string.IsNullOrEmpty(sql))
                {
                    args.StringBuilder.Append(" ");
                    args.StringBuilder.Append(sql);
                }
                sql = args.StringBuilder.ToString();
                sw.Stop();
                GenerationTime = (int)sw.ElapsedMilliseconds;
                if (!string.IsNullOrEmpty(sql))
                {
                    Parent.QueryCache.Set(cacheKey, sql, GenerationTime);
                    Debug.WriteLine($"New SQL: {GenerationTime} ms {sql}");
                }
            }
            if (addGenerationTime)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("Generation time: ");
                sb.Append(GenerationTime);
                sb.Append(" ms. ");
                sb.Append(Environment.NewLine);
                sb.Append(sql);
                sql = sb.ToString();
            }
            return sql;
        }

        /// <summary>
        /// Set query cache.
        /// </summary>
        /// <param name="queryCache">Query cache to use.</param>
        /// <returns>Update arguments.</returns>
        public GXUpdateArgs UseQueryCache(GXQueryCache queryCache)
        {
            Parent.QueryCache = queryCache ?? Parent.QueryCache ?? new GXQueryCache();
            Parent.Settings = GXSqlBuilder.CreateSettings(Parent.QueryCache.DatabaseType);
            return this;
        }

        /// <summary>
        /// SQL generation time in ms.
        /// </summary>
        public int GenerationTime
        {
            get;
            internal set;
        }

        private static List<GXColumnSchema> GetSchemaUpdateColumns(Type type,
            IEnumerable<GXColumnSchema> schema)
        {
            List<GXColumnSchema> columns = schema
                .Where(it => it != null)
                .OrderBy(it => it.Ordinal == 0 ? int.MaxValue : it.Ordinal)
                .ToList();
            foreach (GXColumnSchema column in columns)
            {
                GetSchemaColumnMember(type, column);
            }
            return columns;
        }

        private static LambdaExpression CreateSchemaExpression(Type type,
            IEnumerable<GXColumnSchema> columns)
        {
            ParameterExpression parameter = Expression.Parameter(type, "it");
            List<Expression> expressions = [];
            foreach (GXColumnSchema column in columns)
            {
                MemberInfo member = GetSchemaColumnMember(type, column);
                Expression value = member switch
                {
                    PropertyInfo property => Expression.Property(parameter, property),
                    FieldInfo field => Expression.Field(parameter, field),
                    _ => throw new ArgumentException(
                        $"Column {column.Name} was not found from {type.Name}.")
                };
                expressions.Add(Expression.Convert(value, typeof(object)));
            }
            if (expressions.Count == 1)
            {
                Type func = typeof(Func<,>).MakeGenericType(type, typeof(object));
                return Expression.Lambda(func, expressions[0], parameter);
            }
            Type arrayFunc = typeof(Func<,>).MakeGenericType(type, typeof(object[]));
            return Expression.Lambda(arrayFunc,
                Expression.NewArrayInit(typeof(object), expressions),
                parameter);
        }

        private static MemberInfo GetSchemaColumnMember(Type type,
            GXColumnSchema column)
        {
            PropertyInfo? property = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .SingleOrDefault(it => string.Equals(it.Name, column.Name,
                    StringComparison.OrdinalIgnoreCase));
            if (property != null)
            {
                return property;
            }
            FieldInfo? field = type.GetFields(BindingFlags.Public | BindingFlags.Instance)
                .SingleOrDefault(it => string.Equals(it.Name, column.Name,
                    StringComparison.OrdinalIgnoreCase));
            if (field != null)
            {
                return field;
            }
            throw new ArgumentException(
                $"Column {column.Name} was not found from {type.Name}.");
        }

        /// <summary>
        /// Create new update expression.
        /// </summary>
        /// <param name="value">Updated value.</param>
        /// <returns>Created update attribute.</returns>
        public static GXUpdateArgs Update<T>(T value)
        {
            return Update<T>(value, (Expression<Func<T, object>>)null);
        }

        /// <summary>
        /// Create new update expression.
        /// </summary>
        /// <param name="value">Updated value.</param>
        /// <param name="queryCache">Query cache to use.</param>
        /// <returns>Created update attribute.</returns>
        public static GXUpdateArgs Update<T>(T value, GXQueryCache queryCache)
        {
            return Update<T>(value, (Expression<Func<T, object>>)null).UseQueryCache(queryCache);
        }

        /// <summary>
        /// Create new update expression with schema.
        /// </summary>
        /// <typeparam name="T">The mapped entity type.</typeparam>
        /// <param name="value">The entity whose values are used by this operation.</param>
        /// <param name="schema">Column metadata used to construct the query.</param>
        /// <returns>The update arguments built from the entity and column metadata.</returns>
        /// <exception cref="ArgumentNullException">The value or schema is null.</exception>
        public static GXUpdateArgs Update<T>(T value, params IEnumerable<GXColumnSchema> schema)
        {
            if (value == null)
            {
                throw new ArgumentNullException("Invalid value");
            }
            if (value is IEnumerable && value is not string)
            {
                throw new ArgumentException("Use UpdateRange to update a collection.");
            }
            ArgumentNullException.ThrowIfNull(schema);
            Type type = typeof(T) == typeof(object) ? value.GetType() : typeof(T);
            List<GXColumnSchema> columns = GetSchemaUpdateColumns(type, schema);
            if (columns.Count == 0)
            {
                return Update(value, (Expression<Func<T, object>>?)null);
            }
            if (value is GXTableBase tb)
            {
                tb.BeforeUpdate();
            }
            GXUpdateArgs args = new GXUpdateArgs();
            args.Values.Add(new KeyValuePair<object, LambdaExpression?>(
                value,
                CreateSchemaExpression(type, columns)));
            args.Where.And<T>(q => value);
            return args;
        }

        /// <summary>
        /// Create new update expression.
        /// </summary>
        /// <param name="value">Updated value.</param>
        /// <param name="columns">Updated columns.</param>
        /// <returns>Created update attribute.</returns>
        public static GXUpdateArgs Update<T>(T value, Expression<Func<T, object>>? columns)
        {
            if (value == null)
            {
                throw new ArgumentNullException("Invalid value");
            }
            if (value is IEnumerable)
            {
                throw new ArgumentException("Use UpdateRange to update a collection.");
            }
            if (value is GXTableBase tb)
            {
                tb.BeforeUpdate();
            }
            GXUpdateArgs args = new GXUpdateArgs();
            args.Values.Add(new(value, columns));
            args.Where.And<T>(q => value);
            return args;
        }

        /// <summary>
        /// Create new update expression.
        /// </summary>
        /// <param name="value">Updated value.</param>
        /// <param name="columns">Updated columns.</param>
        /// <param name="queryCache">Query cache to use.</param>
        /// <returns>Created update attribute.</returns>
        public static GXUpdateArgs Update<T>(T value, Expression<Func<T, object>> columns, GXQueryCache queryCache)
        {
            return Update<T>(value, columns).UseQueryCache(queryCache);
        }


        /// <summary>
        /// Create new update expression with schema.
        /// </summary>
        /// <typeparam name="T">The mapped entity type.</typeparam>
        /// <param name="collection">Updated values.</param>
        /// <param name="schema">Column metadata used to construct the query.</param>
        /// <returns>Created update attribute.</returns>
        public static GXUpdateArgs UpdateRange<T>(IEnumerable<T> collection, params IEnumerable<GXColumnSchema> schema)
        {
            if (collection == null)
            {
                throw new ArgumentNullException("Invalid value");
            }
            ArgumentNullException.ThrowIfNull(schema);
            List<T> items = collection.ToList();
            GXUpdateArgs args = new GXUpdateArgs();
            if (items.Count == 0)
            {
                return args;
            }
            Type type = typeof(T) == typeof(object) ?
                items[0]!.GetType() :
                typeof(T);
            if (typeof(T) == typeof(object) &&
                items.Any(it => it == null || it.GetType() != type))
            {
                throw new ArgumentException("All updated schema values must have the same type.");
            }
            List<GXColumnSchema> columns = GetSchemaUpdateColumns(type, schema);
            LambdaExpression? expression = columns.Count == 0 ?
                null :
                CreateSchemaExpression(type, columns);
            foreach (T it in items)
            {
                if (it == null)
                {
                    throw new ArgumentNullException("Invalid value");
                }
                if (it is GXTableBase tb)
                {
                    tb.BeforeUpdate();
                }
                args.Values.Add(new KeyValuePair<object, LambdaExpression?>(
                    it,
                    expression));
            }
            return args;
        }

        /// <summary>
        /// Create new update expression for a collection.
        /// </summary>
        /// <param name="collection">Updated values.</param>
        /// <returns>Created update attribute.</returns>
        public static GXUpdateArgs UpdateRange<T>(IEnumerable<T> collection)
        {
            return UpdateRange(collection, (Expression<Func<T, object>>?)null);
        }

        /// <summary>
        /// Create new update expression for a collection.
        /// </summary>
        /// <param name="collection">Updated values.</param>
        /// <param name="queryCache">Query cache to use.</param>
        /// <returns>Created update attribute.</returns>
        public static GXUpdateArgs UpdateRange<T>(IEnumerable<T> collection, GXQueryCache queryCache)
        {
            return UpdateRange(collection, (Expression<Func<T, object>>)null).UseQueryCache(queryCache);
        }

        /// <summary>
        /// Create new update expression for a collection.
        /// </summary>
        /// <param name="collection">Updated values.</param>
        /// <param name="columns">Updated columns.</param>
        /// <returns>Created update attribute.</returns>
        public static GXUpdateArgs UpdateRange<T>(IEnumerable<T> collection,
            Expression<Func<T, object>>? columns)
        {
            GXUpdateArgs args = new GXUpdateArgs();
            foreach (var it in collection)
            {
                if (it is GXTableBase tb)
                {
                    tb.BeforeUpdate();
                }
                args.Values.Add(new(it, columns));
            }
            return args;
        }

        /// <summary>
        /// Create new update expression for a collection.
        /// </summary>
        /// <param name="collection">Updated values.</param>
        /// <param name="columns">Updated columns.</param>
        /// <param name="queryCache">Query cache to use.</param>
        /// <returns>Created update attribute.</returns>
        public static GXUpdateArgs UpdateRange<T>(IEnumerable<T> collection, Expression<Func<T, object>> columns, GXQueryCache queryCache)
        {
            return UpdateRange(collection, columns).UseQueryCache(queryCache);
        }

        /// <summary>
        /// Add new item to update.
        /// </summary>
        /// <typeparam name="T">The mapped entity type.</typeparam>
        /// <param name="value">The entity whose values are used by this operation.</param>
        /// <param name="columns">The columns selected by this operation.</param>
        public void Add<T>(T value, Expression<Func<T, object>> columns)
        {
            //Clear previous values if values collection is empty.
            if (Values.Count == 1 && Values[0].Value == null)
            {
                Values.Clear();
            }
            Values.Add(new KeyValuePair<object, LambdaExpression>(value, columns));
        }

        /// <summary>
        /// Where expression.
        /// </summary>
        public GXWhereCollection Where
        {
            get;
        }

        /// <summary>
        /// Where expression.
        /// </summary>
        public GXJoinCollection Joins
        {
            get;
        }

        /// <summary>
        /// Exclude columns from the update.
        /// </summary>
        /// <param name="columns">Excluded columns.</param>
        public void Exclude<T>(Expression<Func<T, object>> columns)
        {
            Excluded.Add(new KeyValuePair<Type, LambdaExpression>(typeof(T), columns));
        }

        /// <summary>
        /// The maximum number of items that can be updated.
        /// </summary>
        /// <remarks>
        /// If value is zero there are no limitations.
        /// </remarks>
        public UInt32 Count
        {
            get
            {
                return Parent.Count;
            }
            set
            {
                Parent.Count = value;
            }
        }

        /// <summary>
        /// Only changed values are updated.
        /// </summary>
        /// <param name="obj1">The original object.</param>
        /// <param name="obj2">The object to compare with the original object.</param>
        /// <param name="differences ">List to store property differences.</param>
        /// <returns>Created update attribute.</returns>
        public static GXUpdateArgs UpdateChangedOnly<T>(T obj1, T obj2, List<GXPropertyDifference>? differences)
        {
            return UpdateChangedOnly(obj1, obj2, differences);
        }

        /// <summary>
        /// Only changed values are updated.
        /// </summary>
        /// <param name="obj1">The original object.</param>
        /// <param name="obj2">The object to compare with the original object.</param>
        /// <param name="queryCache">Query cache to use.</param>
        /// <returns>Created update attribute.</returns>
        public static GXUpdateArgs UpdateChangedOnly<T>(T obj1, T obj2, GXQueryCache? queryCache)
        {
            return UpdateChangedOnly(obj1, obj2, null, queryCache);
        }

        /// <summary>
        /// Only changed values are updated.
        /// </summary>
        /// <param name="obj1">The original object.</param>
        /// <param name="obj2">The object to compare with the original object.</param>
        /// <param name="differences ">List to store property differences.</param>
        /// <param name="queryCache">Query cache to use.</param>
        /// <returns>Created update attribute.</returns>
        public static GXUpdateArgs UpdateChangedOnly<T>(T obj1, T obj2, List<GXPropertyDifference>? differences = null, GXQueryCache? queryCache = null)
        {
            ArgumentNullException.ThrowIfNull(obj1);
            ArgumentNullException.ThrowIfNull(obj2);
            GXUpdateArgs args = new GXUpdateArgs();
            if (queryCache != null)
            {
                args.UseQueryCache(queryCache);
            }
            List<object> targets = new List<object>();
            Dictionary<string, GXSerializedItem> properties = GXSqlBuilder.GetProperties(GXInternal.GetPropertyType(typeof(T)));

            foreach (var property in properties)
            {
                object? oldValue = property.Value.Get(obj1);
                object? newValue = property.Value.Get(obj2);
                if (!Equals(oldValue, newValue))
                {
                    targets.Add(property.Key);
                    differences?.Add(new GXPropertyDifference
                    {
                        Name = property.Key,
                        OldValue = oldValue,
                        NewValue = newValue
                    });
                }
            }
            var arr = targets.ToArray();
            args.Add(obj2, x => arr);
            return args;
        }
    }
}
