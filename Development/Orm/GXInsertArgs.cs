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
using Gurux.Service.Orm.Common.Enums;
using Gurux.Service.Orm.Common.Model;
using Gurux.Service.Orm.Internal;
using Gurux.Service.Orm.Settings;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Gurux.Service.Orm
{
    class GXUpdateItem
    {
        /// <summary>
        /// List of columns.
        /// </summary>
        public List<string> Columns;

        /// <summary>
        /// List of columns objects and serialized items.
        /// </summary>
        public List<List<KeyValuePair<object, GXSerializedItem>>> Rows;

        /// <summary>
        /// List of inset where items.
        /// </summary>
        internal List<string> Where = new List<string>();

        /// <summary>
        /// Is item inserted.
        /// </summary>
        /// <remarks>
        /// This is used in update.
        /// </remarks>
        public bool Inserted;

        public GXUpdateItem()
        {
            Columns = new List<string>();
            Rows = new List<List<KeyValuePair<object, GXSerializedItem>>>();
        }
    }

    readonly record struct GXInsertItem(
        Type Type,
        KeyValuePair<string, GXSerializedItem>? ValuePair);

    /// <summary>
    /// Select arguments.
    /// </summary>
    public class GXInsertArgs
    {
        /// <summary>
        /// List of values to insert.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal List<KeyValuePair<object?, LambdaExpression?>> Values = new List<KeyValuePair<object?, LambdaExpression?>>();

        /// <summary>
        /// List of values to exlude from insert.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal List<KeyValuePair<Type, LambdaExpression>> Excluded = new List<KeyValuePair<Type, LambdaExpression>>();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal GXSettingsArgs Parent = new GXSettingsArgs();

        /// <summary>
        /// Target ID column.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal GXInsertItem? Id = null;

        /// <summary>
        /// Constructor.
        /// </summary>
        private GXInsertArgs()
        {
        }

        /// <summary>
        /// Clear all insert settings.
        /// </summary>
        public void Clear()
        {
            Values.Clear();
            Parent.Clear();
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

        /// <inheritdoc/>
        /// <param name="addGenerationTime">Is SQL generation time included in the output.</param>
        public string ToString(bool addGenerationTime)
        {
            string sql;
            string cacheKey = Parent.QueryCache.BuildKey(Parent.Settings.Type, Values, Excluded);
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
                bool modified = false;
                var sw = Stopwatch.StartNew();
                GXGetMembersArgs args = new GXGetMembersArgs(Parent.Settings, TargetType.Column)
                {
                    StringBuilder = new StringBuilder(),
                    SingleTable = true,
                };
                Dictionary<Type, HashSet<string>> replacedColumns = new Dictionary<Type, HashSet<string>>();
                foreach (KeyValuePair<object?, LambdaExpression?> it in Values)
                {
                    if (it.Key is GXSelectArgs && it.Value != null)
                    {
                        args.Expression = it.Value;
                        Type type = GXDbHelpers.GetType(it.Value);
                        string[]? list = GXDbHelpers.GetMemberList(args);
                        if (list != null)
                        {
                            if (!replacedColumns.TryGetValue(type, out HashSet<string>? columns))
                            {
                                columns = new HashSet<string>();
                                replacedColumns[type] = columns;
                            }
                            foreach (string name in list)
                            {
                                columns.Add(name);
                            }
                        }
                    }
                }
                args.Expression = null;
                StringBuilder values = new StringBuilder();
                Type? table = null;
                List<string>? excluded = null;
                //Select is used in insert. In this case, we don't need to add values.
                bool select = false;
                bool newRow = false;
                bool multipleRows = Values.Count > 1;
                bool init = true;
                foreach (KeyValuePair<object?, LambdaExpression?> it in Values)
                {
                    if (table == null || table != it.Key?.GetType())
                    {
                        bool changed = table != it.Key!.GetType();
                        if (it.Key is GXSelectArgs)
                        {
                            if (it.Value == null)
                            {
                                throw new ArgumentNullException("Columns must be specified when inserting from select.");
                            }
                            //Get type.
                            Type type = GXDbHelpers.GetType(it.Value);
                            changed = table != type;
                            table = type;
                        }
                        else if (it.Key is IEnumerable<GXColumnSchema> columns)
                        {
                            init = args.StringBuilder.Length == 0;
                            string tmp;
                            if (args.StringBuilder.Length != 0 &&
                                Parent.Settings.Type != DatabaseType.Oracle &&
                                Parent.Settings.Type != DatabaseType.SapHana)
                            {
                                values.Length -= 2;
                                values.Append("), (");
                            }
                            else
                            {
                                if (!init && Parent.Settings.Type == DatabaseType.Oracle)
                                {
                                    args.StringBuilder.Append("VALUES(");
                                    values.Length -= 2;
                                    args.StringBuilder.Append(values);
                                    args.StringBuilder.Append(") ");
                                    values.Length = 0;
                                    args.StringBuilder.Append("INTO ");
                                }
                                else if (!init && Parent.Settings.Type == DatabaseType.SapHana)
                                {
                                    args.StringBuilder.Append("SELECT ");
                                    values.Length -= 2;
                                    args.StringBuilder.Append(values);
                                    args.StringBuilder.Append(" FROM DUMMY UNION ALL ");
                                    values.Length = 0;
                                }
                                else if (Parent.Settings.Type == DatabaseType.Oracle &&
                                    Values.Count != 1)
                                {
                                    if (init)
                                    {
                                        args.StringBuilder.Append("INSERT ALL INTO ");
                                    }
                                    else
                                    {
                                        args.StringBuilder.Append("INTO ");
                                    }
                                }
                                else
                                {
                                    args.StringBuilder.Append("INSERT INTO ");
                                }
                                if (Parent.Settings.Type != DatabaseType.SapHana ||
                                    init)
                                {
                                    tmp = GXDbHelpers.ConvertToString(Parent.Settings, TargetType.Table, null, columns.First().Parent.Name, null);
                                    args.StringBuilder.Append(tmp);
                                    args.StringBuilder.Append(" (");
                                    foreach (var c in columns)
                                    {
                                        tmp = GXDbHelpers.ConvertToString(Parent.Settings, TargetType.Column, null, c.Name, null);
                                        args.StringBuilder.Append(tmp);
                                        args.StringBuilder.Append(", ");
                                    }
                                    args.StringBuilder.Length -= 2;
                                    args.StringBuilder.Append(") ");
                                }
                            }
                            args.Expression = it.Value;
                            var old = args.StringBuilder;
                            try
                            {
                                args.StringBuilder = values;
                                GXDbHelpers.GetMembers(args);
                                values.Append(", ");
                                if (it.Value?.ReturnType == typeof(GXSelectArgs))
                                {
                                    //Is values are copied with select, we don't need to add values.
                                    select = true;
                                    //Remove SELECT from the beginning of the string.
                                    string str = values.ToString().Substring(7);
                                    values.Length = 0;
                                    values.Append(str);
                                }
                            }
                            finally
                            {
                                args.StringBuilder = old;
                            }
                            continue;
                        }
                        else
                        {
                            table = it.Key.GetType();
                        }
                        if (changed)
                        {
                            excluded = GXDbHelpers.ExcludedProperties(Excluded, args, table!);
                            if (multipleRows)
                            {
                                excluded.AddRange(GXDbHelpers.GetAlwaysNullProperties(table!, Values.Where(w => w.Key != null).Select(v => v.Key!)));
                            }
                            string tmp = GXDbHelpers.ConvertToString(Parent.Settings, TargetType.Table, null, table, null);
                            if (Parent.Settings.Type == DatabaseType.Oracle &&
                                Values.Count != 1)
                            {
                                args.StringBuilder.Append("INSERT ALL INTO ");
                            }
                            else
                            {
                                args.StringBuilder.Append("INSERT INTO ");
                            }
                            args.StringBuilder.Append(tmp);
                            args.StringBuilder.Append(" (");
                        }
                    }
                    else
                    {
                        newRow = table == it.Key.GetType();
                        if (newRow)
                        {
                            values.Length -= 2;
                            if (Parent.Settings.Type == DatabaseType.Oracle)
                            {
                                string tmp = GXDbHelpers.ConvertToString(Parent.Settings, TargetType.Table, null, table, null);
                                values.Append(")");
                                args.StringBuilder.Length -= 2;
                                args.StringBuilder.Append(") VALUES(");
                                args.StringBuilder.Append(values);
                                values.Length = 0;
                                args.StringBuilder.Append(" INTO ");
                                args.StringBuilder.Append(tmp);
                                args.StringBuilder.Append(" (");
                                newRow = false;
                            }
                            else if (Parent.Settings.Type == DatabaseType.SapHana)
                            {
                                if (args.StringBuilder[args.StringBuilder.Length - 2] == ',' &&
                                    args.StringBuilder[args.StringBuilder.Length - 1] == ' ')
                                {
                                    //Remove the last comma and space for the first row.
                                    args.StringBuilder.Length -= 2;
                                    args.StringBuilder.Append(')');
                                }
                                args.StringBuilder.Append(" SELECT ");
                                args.StringBuilder.Append(values);
                                values.Length = 0;
                                args.StringBuilder.Append(" FROM DUMMY UNION ALL");
                            }
                            else
                            {
                                values.Append("), (");
                            }
                        }
                    }
                    Dictionary<string, GXSerializedItem>? properties = null;
                    if (table != null)
                    {
                        properties = GXSqlBuilder.GetProperties(table);
                    }
                    string[]? members;
                    if (it.Value != null || properties == null)
                    {
                        //Add selected values.
                        args.Expression = it.Value;
                        members = GXDbHelpers.GetMemberList(args);
                    }
                    else
                    {
                        members = properties.Keys.ToArray();
                    }
                    foreach (var name in members)
                    {
                        object? value;
                        var p = properties != null ? properties[name] : null;
                        var att = Attributes.AutoIncrement | Attributes.CurrentTimestamp;
                        GXSelectArgs? a = it.Key as GXSelectArgs;
                        if (p != null && (p.Attributes & (att)) == 0 &&
                            (p.Relation?.RelationType != RelationType.OneToMany || a != null) &&
                            excluded!.Contains(name) != true &&
                            (a != null || !replacedColumns.TryGetValue(table!, out HashSet<string>? columns) || !columns.Contains(name)))
                        {
                            if (a == null)
                            {
                                value = p.Get(it.Key!);
                                if ((p.Attributes & (Attributes.PrimaryKey | Attributes.ConcurrencyCheck)) != 0 &&
                                    GXDbHelpers.IsZero(value))
                                {
                                    //Generate new guid for primary key if it is not set and type is guid.
                                    if (p.Type == typeof(Guid))
                                    {
                                        modified = true;
                                        value = Guid.CreateVersion7();
                                        p.Set(it.Key!, value);
                                    }
                                    else if (p.Type == typeof(string))
                                    {
                                        modified = true;
                                        value = Guid.CreateVersion7().ToString();
                                        p.Set(it.Key!, value);
                                    }
                                    else
                                    {
                                        throw new ArgumentException($"Primary key {name} is not set.");
                                    }
                                }
                                if (p.Relation?.RelationType != RelationType.ManyToMany &&
                                    !GXInternal.IsGenericDataType(p.Type))
                                {
                                    var s = GXSqlBuilder.FindUnique(p.Type);
                                    if (s != null)
                                    {
                                        if (value is null)
                                        {
                                            if ((p.Attributes & Attributes.ForeignKey) != 0 &&
                                                (p.Attributes & Attributes.AllowNull) == 0)
                                            {
                                                throw new Exception(name + " foreign key is not set.");
                                            }
                                        }
                                        else
                                        {
                                            value = s.Get(value);
                                            if ((p.Attributes & Attributes.ForeignKey) != 0 &&
                                                GXDbHelpers.IsZero(value))
                                            {
                                                throw new Exception(name + " foreign key is not set.");
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                value = a.ToString(false);
                            }
                            if (multipleRows || value != null)
                            {
                                bool empty = false;
                                if (value is IEnumerable e &&
                                    !GXSqlBuilder.IsSimpleType(value.GetType()))
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
                                                string tmp = GXDbHelpers.ConvertToString(Parent.Settings, TargetType.Value, null, tmp3, null);
                                                values.Append(tmp);
                                                values.Append(", ");
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
                                    string tmp;
                                    if (a == null)
                                    {
                                        tmp = GXDbHelpers.ConvertToString(Parent.Settings, TargetType.Value, null, value, null);
                                        values.Append(tmp);
                                        values.Append(", ");
                                    }
                                    else if (!select)
                                    {
                                        //If we have already added a select statement, we don't need to add another one.
                                        select = true;
                                        tmp = (string)value;
                                        //Remove "SELECT " from the beginning of the string.
                                        tmp = tmp.Substring(6).Trim();
                                        if (values.Length != 0)
                                        {
                                            tmp = values.ToString() + tmp;
                                            values.Length = 0;
                                        }
                                        values.Append(tmp);
                                        values.Append(", ");
                                    }
                                }
                                if (!empty && !newRow)
                                {
                                    string tmp = GXDbHelpers.ConvertToString(Parent.Settings, TargetType.Column, null, name, null);
                                    args.StringBuilder.Append(tmp);
                                    args.StringBuilder.Append(", ");
                                }
                            }
                        }
                    }
                }
                values.Length -= 2;
                if (select)
                {
                    args.StringBuilder.Length -= 2;
                    args.StringBuilder.Append(") SELECT ");
                }
                else if (Parent.Settings.Type == DatabaseType.SapHana &&
                    Values.Count != 1)
                {
                    args.StringBuilder.Append(" SELECT ");
                    args.StringBuilder.Append(values);
                    values.Length = 0;
                    args.StringBuilder.Append(" FROM DUMMY");
                    //Ignore '(' char from the eol.
                    select = true;
                }
                else
                {
                    args.StringBuilder.Length -= 2;
                    args.StringBuilder.Append(") VALUES(");
                }
                args.StringBuilder.Append(values);
                if (!select)
                {
                    args.StringBuilder.Append(")");
                }

                if (Parent.Settings.Type == DatabaseType.Oracle &&
                    Values.Count != 1)
                {
                    args.StringBuilder.Append(" SELECT 1 FROM DUAL");
                }
                sql = args.StringBuilder.ToString();
                sw.Stop();
                GenerationTime = (int)sw.ElapsedMilliseconds;
                if (!string.IsNullOrEmpty(sql))
                {
                    if (modified)
                    {
                        cacheKey = Parent.QueryCache.BuildKey(Parent.Settings.Type, Values, Excluded);
                    }
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
        /// Sets the query cache to use for this insert operation.
        /// </summary>
        /// <param name="queryCache">The query cache instance to use.</param>
        /// <returns>This <see cref="GXInsertArgs"/> instance.</returns>
        public GXInsertArgs UseQueryCache(GXQueryCache queryCache)
        {
            Parent.Settings = GXSqlBuilder.CreateSettings(queryCache.DatabaseType);
            Parent.QueryCache = queryCache ?? Parent.QueryCache ?? new GXQueryCache();
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

        /// <summary>
        /// Insert a value into the table.
        /// </summary>
        /// <typeparam name="T">Type of the value to insert.</typeparam>
        /// <param name="value">Value to insert.</param>
        public static GXInsertArgs Insert<T>(T value)
        {
            return Insert<T>(value, (Expression<Func<T, object>>?)null);
        }

        /// <summary>
        /// Insert value to the database using column schema.
        /// </summary>
        /// <param name="value">The value to insert.</param>
        /// <param name="schemas">The column schemas to use for the insert.</param>
        /// <returns>This <see cref="GXInsertArgs"/> instance.</returns>
        public static GXInsertArgs Insert<T>(T value, params IEnumerable<GXColumnSchema> schemas)
        {
            if (value == null)
            {
                throw new ArgumentNullException("Inserted item can't be null.");
            }
            if (value is GXInsertArgs)
            {
                throw new ArgumentException("Can't insert GXInsertArgs.");
            }
            ArgumentNullException.ThrowIfNull(schemas);
            if (value is GXTableBase tb)
            {
                tb.BeforeAdd();
            }
            GXInsertArgs args = new GXInsertArgs();
            if (!typeof(T).IsClass || value.GetType() == typeof(string))
            {
                var sa = schemas.FirstOrDefault();
                if (sa == null)
                {
                    throw new ArgumentException("Only one column can be specified for primitive types.");
                }
                ParameterExpression parameter = Expression.Parameter(typeof(string), "p");
                var body = Expression.Constant(value, value.GetType());
                LambdaExpression e = Expression.Lambda(body, parameter);
                args.Values.Add(new KeyValuePair<object?, LambdaExpression?>(schemas, e));
            }
            else if (value is GXSelectArgs sa)
            {
                ParameterExpression parameter = Expression.Parameter(typeof(GXSelectArgs), "p");
                var body = Expression.Constant(sa, typeof(GXSelectArgs));
                LambdaExpression e = Expression.Lambda(body, parameter);
                args.Values.Add(new KeyValuePair<object?, LambdaExpression?>(schemas, e));
            }
            else
            {
                Dictionary<string, GXSerializedItem> properties = GXSqlBuilder.GetProperties(typeof(T));
                if (!properties.Any())
                {
                    List<object> list = [value as IEnumerable];
                    var body = Expression.NewArrayInit(
                        typeof(object),
                        list.Select(x =>
                            Expression.Convert(
                                Expression.Constant(x),
                                typeof(object))));
                    args.Values.Add(new(schemas, Expression.Lambda(body)));
                }
                else
                {
                    List<string> names = new List<string>();
                    foreach (var column in schemas)
                    {
                        if (column.IsAutoIncrement ||
                            column.IsIdentity ||
                            column.IsGenerated)
                        {
                            continue;
                        }
                        var property = properties.Where(w => string.Compare(w.Key, column.Name, true) == 0).SingleOrDefault();
                        if (property.Key == null)
                        {
                            throw new ArgumentException(
                                $"Column {column.Name} was not found from {column.Type.Name}.");
                        }
                        names.Add(property.Key);
                    }
                    var body = Expression.NewArrayInit(typeof(string), names.Select(x => Expression.Constant(x)));
                    args.Values.Add(new KeyValuePair<object?, LambdaExpression?>(value, Expression.Lambda(body)));
                }
            }
            return args;
        }

        /// <summary>
        /// Insert value to the database using column schema.
        /// </summary>
        /// <param name="values">The values to insert.</param>
        /// <param name="columns">The column schema to use for the insert.</param>
        /// <returns>This <see cref="GXInsertArgs"/> instance.</returns>
        public static GXInsertArgs Insert(IEnumerable<GXColumnSchema> columns,
            IEnumerable<object?> values)
        {
            if (columns == null)
            {
                throw new ArgumentNullException("Inserted columns can't be null.");
            }
            if (values == null)
            {
                throw new ArgumentNullException("Inserted items can't be null.");
            }
            if (columns.Count() != values.Count())
            {
                throw new ArgumentException("Inserted columns and items must have the same count.");
            }
            GXInsertArgs args = new GXInsertArgs();
            for (int index = 0; index != values.Count(); ++index)
            {
                var col = columns.ElementAt(index);
                var value = values.ElementAt(index);
                if (value != null && value.GetType() != col.Type)
                {
                    value = Convert.ChangeType(value, col.Type);
                }
                var parameter = Expression.Parameter(col.Type, "q");
                var body = Expression.Constant(value, col.Type);
                LambdaExpression e = Expression.Lambda(body, parameter);
                args.Values.Add(new KeyValuePair<object?, LambdaExpression?>(value, e));
            }
            return args;
        }

        /// <summary>
        /// Insert a value into the table and use the query cache.
        /// </summary>
        /// <typeparam name="T">Type of the value to insert.</typeparam>
        /// <param name="value">Value to insert.</param>
        /// <param name="queryCache">The query cache instance to use.</param>
        public static GXInsertArgs Insert<T>(T value, GXQueryCache queryCache)
        {
            return Insert<T>(value, (Expression<Func<T, object>>?)null).UseQueryCache(queryCache);
        }

        /// <summary>
        /// Copy rows from the same table.
        /// </summary>
        /// <typeparam name="T">Table type to insert into.</typeparam>
        /// <param name="select">Select arguments defining the rows to copy.</param>
        public static GXInsertArgs Insert<T>(GXSelectArgs select)
        {
            GXInsertArgs args = new GXInsertArgs();
            select.Parent.Settings = args.Parent.Settings;
            Expression<Func<T, object>> expression = _ => typeof(T);
            args.Values.Add(new KeyValuePair<object?, LambdaExpression?>(select, expression));
            return args;
        }

        /// <summary>
        /// Copy rows from the same table and use the query cache.
        /// </summary>
        /// <typeparam name="T">Table type to insert into.</typeparam>
        /// <param name="select">Select arguments defining the rows to copy.</param>
        /// <param name="queryCache">The query cache instance to use.</param>
        public static GXInsertArgs Insert<T>(GXSelectArgs select, GXQueryCache queryCache)
        {
            return Insert<T>(select).UseQueryCache(queryCache);
        }

        /// <summary>
        /// Copy rows from the table into selected columns.
        /// </summary>
        /// <typeparam name="T">Table type to insert into.</typeparam>
        /// <param name="select">Select arguments defining the rows to copy.</param>
        /// <param name="columns">Columns to insert into.</param>
        public static GXInsertArgs Insert<T>(GXSelectArgs select, Expression<Func<T, object>> columns)
        {
            GXInsertArgs args = new GXInsertArgs();
            args.Parent.Settings = select.Parent.Settings;
            args.Add(select, columns);
            return args;
        }

        /// <summary>
        /// Copy rows from the table into selected columns and use the query cache.
        /// </summary>
        /// <typeparam name="T">Table type to insert into.</typeparam>
        /// <param name="select">Select arguments defining the rows to copy.</param>
        /// <param name="columns">Columns to insert into.</param>
        /// <param name="queryCache">The query cache instance to use.</param>
        public static GXInsertArgs Insert<T>(GXSelectArgs select, Expression<Func<T, object>> columns, GXQueryCache queryCache)
        {
            return Insert<T>(select, columns).UseQueryCache(queryCache);
        }

        /// <summary>
        /// Add a value with specific columns to insert.
        /// </summary>
        /// <typeparam name="T">Type of the value to insert.</typeparam>
        /// <param name="value">Value to insert.</param>
        /// <param name="columns">Columns to insert into.</param>
        public void Add<T>(T value, Expression<Func<T, object>> columns)
        {
            Values.Add(new KeyValuePair<object?, LambdaExpression?>(value, columns));
        }

        /// <summary>
        /// Add select-based rows with specific columns to insert.
        /// </summary>
        /// <typeparam name="T">Type of the target table.</typeparam>
        /// <param name="select">Select arguments defining the rows to copy.</param>
        /// <param name="columns">Columns to insert into.</param>
        public void Add<T>(GXSelectArgs select, Expression<Func<T, object>> columns)
        {
            Values.Add(new KeyValuePair<object?, LambdaExpression?>(select, columns));
        }

        /// <summary>
        /// Insert an array of values into selected columns.
        /// </summary>
        /// <typeparam name="T">Type of the values to insert.</typeparam>
        /// <param name="value">Array of values to insert.</param>
        /// <param name="columns">Columns to insert into.</param>
        public static GXInsertArgs Insert<T>(T[] value, Expression<Func<T, object>> columns)
        {
            return InsertRange<T>(value, columns);
        }

        /// <summary>
        /// Insert an array of values into selected columns and use the query cache.
        /// </summary>
        /// <typeparam name="T">Type of the values to insert.</typeparam>
        /// <param name="value">Array of values to insert.</param>
        /// <param name="columns">Columns to insert into.</param>
        /// <param name="queryCache">The query cache instance to use.</param>
        public static GXInsertArgs Insert<T>(T[] value, Expression<Func<T, object>> columns, GXQueryCache queryCache)
        {
            return InsertRange<T>(value, columns).UseQueryCache(queryCache);
        }

        /// <summary>
        /// Insert a value into selected columns.
        /// </summary>
        /// <typeparam name="T">Type of the value to insert.</typeparam>
        /// <param name="value">Value to insert.</param>
        /// <param name="columns">Columns to insert into.</param>
        public static GXInsertArgs Insert<T>(T value,
            Expression<Func<T, object>>? columns)
        {
            if (value is GXSelectArgs a)
            {
                return Insert<T>(a);
            }
            if (value is GXInsertArgs)
            {
                throw new ArgumentException("Can't insert GXInsertArgs.");
            }
            if (value == null)
            {
                throw new ArgumentNullException("Inserted item can't be null.");
            }
            if (value is IEnumerable)
            {
                throw new ArgumentException("Use InsertRange to add a collection.");
            }
            if (value is GXTableBase tb)
            {
                tb.BeforeAdd();
            }
            GXInsertArgs args = new GXInsertArgs();
            args.Values.Add(new KeyValuePair<object?, LambdaExpression?>(value, columns));
            //Add ID columns to the insert if they are not already added.
            //This is needed to update new ID.
            Type type = typeof(T);
            var s = GXSqlBuilder.FindUnique2(type);
            if (s != null)
            {
                if (s.Value.Value.Type == typeof(Guid))
                {
                    object? tmp = s.Value.Value.Get(value);
                    if (GXDbHelpers.IsZero(tmp))
                    {
                        //Generate new guid for primary key if it is not set and type is guid.
                        Guid id = Guid.CreateVersion7();
                        s.Value.Value.Set(value, id);
                    }
                }
                else if (s.Value.Value.Type == typeof(string))
                {
                    object? tmp = s.Value.Value.Get(value);
                    if (GXDbHelpers.IsZero(tmp))
                    {
                        //Generate new guid for primary key if it is not set and type is guid.
                        Guid id = Guid.CreateVersion7();
                        s.Value.Value.Set(value, id.ToString());
                    }
                }
                else
                {
                    object? tmp = s.Value.Value.Get(value);
                    if (GXDbHelpers.IsZero(tmp))
                    {
                        args.Id = new GXInsertItem(type, s);
                    }
                }
            }
            return args;
        }

        /// <summary>
        /// Insert a value into selected columns and use the query cache.
        /// </summary>
        /// <typeparam name="T">Type of the value to insert.</typeparam>
        /// <param name="value">Value to insert.</param>
        /// <param name="columns">Columns to insert into.</param>
        /// <param name="queryCache">The query cache instance to use.</param>
        public static GXInsertArgs Insert<T>(T value,
            Expression<Func<T, object>>? columns,
            GXQueryCache queryCache)
        {
            return Insert(value, columns).UseQueryCache(queryCache);
        }

        /// <summary>
        /// Insert a collection of values into the table.
        /// </summary>
        /// <typeparam name="T">Type of the values to insert.</typeparam>
        /// <param name="collection">Collection of values to insert.</param>
        public static GXInsertArgs InsertRange<T>(IEnumerable<T> collection)
        {
            return InsertRange(collection, (Expression<Func<T, object>>?)null);
        }

        /// <summary>
        /// Insert a collection of values into the table and use the query cache.
        /// </summary>
        /// <typeparam name="T">Type of the values to insert.</typeparam>
        /// <param name="collection">Collection of values to insert.</param>
        /// <param name="queryCache">The query cache instance to use.</param>
        public static GXInsertArgs InsertRange<T>(IEnumerable<T> collection, GXQueryCache queryCache)
        {
            return InsertRange(collection, (Expression<Func<T, object>>?)null).UseQueryCache(queryCache);
        }

        /// <summary>
        /// Insert a collection of values into the table using column schema.
        /// </summary>
        /// <param name="values">Collection of values to insert.</param>
        /// <param name="schemas">Column schemas to use for the insert.</param>
        /// <returns>GXInsertArgs instance representing the insert operation.</returns>
        public static GXInsertArgs InsertRange<T>(IEnumerable<T> values, params IEnumerable<GXColumnSchema> schemas)
        {
            LambdaExpression e;
            Expression body;
            ArgumentNullException.ThrowIfNull(values);
            ArgumentNullException.ThrowIfNull(schemas);
            List<string> names = new List<string>();
            Type? type = values.First()?.GetType();
            GXInsertArgs args = new GXInsertArgs();
            if (type == null || !type.IsClass || type == typeof(string))
            {
                schemas = schemas.Where(w => !w.IsAutoIncrement && !w.IsGenerated);
                foreach (var value in values)
                {
                    if (value == null)
                    {
                        throw new ArgumentNullException("Inserted item can't be null.");
                    }
                    if (value is GXInsertArgs)
                    {
                        throw new ArgumentException("Can't insert GXInsertArgs.");
                    }
                    if (value is GXTableBase tb)
                    {
                        tb.BeforeAdd();
                    }
                    ParameterExpression parameter = Expression.Parameter(typeof(string), "p");
                    body = Expression.Constant(value, value.GetType());
                    e = Expression.Lambda(body, parameter);
                    args.Values.Add(new KeyValuePair<object?, LambdaExpression?>(schemas, e));
                }
            }
            else
            {
                Dictionary<string, GXSerializedItem> properties = GXSqlBuilder.GetProperties(typeof(T));
                foreach (var column in schemas)
                {
                    if (column.IsAutoIncrement ||
                        column.IsIdentity ||
                        column.IsGenerated)
                    {
                        continue;
                    }
                    var property = properties.Where(w => string.Compare(w.Key, column.Name, true) == 0).SingleOrDefault();
                    if (property.Key == null)
                    {
                        throw new ArgumentException(
                            $"Column {column.Name} was not found from {column.Type.Name}.");
                    }
                    names.Add(property.Key);
                }
                body = Expression.NewArrayInit(typeof(string), names.Select(x => Expression.Constant(x)));
                e = Expression.Lambda(body);
                foreach (var value in values)
                {
                    if (value == null)
                    {
                        throw new ArgumentNullException("Inserted item can't be null.");
                    }
                    if (value is GXInsertArgs)
                    {
                        throw new ArgumentException("Can't insert GXInsertArgs.");
                    }
                    if (value is GXTableBase tb)
                    {
                        tb.BeforeAdd();
                    }
                    args.Values.Add(new KeyValuePair<object?, LambdaExpression?>(value, e));
                }
            }
            return args;
        }

        /// <summary>
        /// Insert value to the database using column schema.
        /// </summary>
        /// <param name="values">The values to insert.</param>
        /// <param name="columns">The column schema to use for the insert.</param>
        /// <returns>This <see cref="GXInsertArgs"/> instance.</returns>
        public static GXInsertArgs InsertRange(IEnumerable<GXColumnSchema> columns,
            IEnumerable<IEnumerable<object?>> values)
        {
            if (columns == null)
            {
                throw new ArgumentNullException("Inserted columns can't be null.");
            }
            if (values == null)
            {
                throw new ArgumentNullException("Inserted items can't be null.");
            }
            GXInsertArgs args = new GXInsertArgs();
            foreach (var it in values)
            {
                var parameter = Expression.Parameter(typeof(IEnumerable<object>), "q");
                var body = Expression.Constant(it, typeof(IEnumerable<object>));
                LambdaExpression e = Expression.Lambda(body, parameter);
                args.Values.Add(new KeyValuePair<object?, LambdaExpression?>(it, e));
            }
            return args;
        }

        /// <summary>
        /// Insert a collection of values into selected columns.
        /// </summary>
        /// <typeparam name="T">Type of the values to insert.</typeparam>
        /// <param name="collection">Collection of values to insert.</param>
        /// <param name="columns">Columns to insert into.</param>
        public static GXInsertArgs InsertRange<T>(IEnumerable<T> collection,
            Expression<Func<T, object>>? columns)
        {
            if (collection == null)
            {
                throw new ArgumentNullException("Inserted item can't be null.");
            }
            GXInsertArgs args = new GXInsertArgs();
            //Add ID columns to the insert if they are not already added.
            //This is needed to update new ID.
            Type type = typeof(T);
            var s = GXSqlBuilder.FindUnique2(type);
            if (s != null)
            {
                if (s.Value.Value.Type == typeof(Guid) ||
                    s.Value.Value.Type == typeof(string))
                {
                    //Generate new guid for primary key if it is not set and type is guid.
                    foreach (var it in collection)
                    {
                        Guid id = Guid.CreateVersion7();
                        if (s.Value.Value.Type == typeof(string))
                        {
                            s.Value.Value.Set(it!, id.ToString());
                        }
                        else
                        {
                            s.Value.Value.Set(it!, id);
                        }
                    }
                }
                else
                {
                    foreach (var it in collection)
                    {
                        object? tmp = s.Value.Value.Get(it!);
                        if (GXDbHelpers.IsZero(tmp))
                        {
                            args.Id = new GXInsertItem(type, s);
                        }
                        break;
                    }
                }
            }
            foreach (var it in collection)
            {
                if (it is GXTableBase tb)
                {
                    tb.BeforeAdd();
                }
                args.Values.Add(new KeyValuePair<object?, LambdaExpression?>(it, columns));
            }
            return args;
        }

        /// <summary>
        /// Insert a collection of values into selected columns and use the query cache.
        /// </summary>
        /// <typeparam name="T">Type of the values to insert.</typeparam>
        /// <param name="collection">Collection of values to insert.</param>
        /// <param name="columns">Columns to insert into.</param>
        /// <param name="queryCache">The query cache instance to use.</param>
        public static GXInsertArgs InsertRange<T>(IEnumerable<T> collection,
            Expression<Func<T, object>> columns,
            GXQueryCache queryCache)
        {
            return InsertRange(collection, columns).UseQueryCache(queryCache);
        }

        /// <summary>
        /// Add the association between an item and a collection of destination items.
        /// </summary>
        /// <typeparam name="TItem">Type of the source item.</typeparam>
        /// <typeparam name="TDestination">Type of the destination items.</typeparam>
        /// <param name="item">Source item.</param>
        /// <param name="collections">Destination items to associate.</param>
        public static GXInsertArgs Add<TItem, TDestination>(TItem item, TDestination[] collections)
        {
            return Add([item], collections);
        }

        /// <summary>
        /// Add the association between a collection of items and a destination item.
        /// </summary>
        /// <typeparam name="TItem">Type of the source items.</typeparam>
        /// <typeparam name="TDestination">Type of the destination item.</typeparam>
        /// <param name="items">Source items.</param>
        /// <param name="collection">Destination item to associate.</param>
        public static GXInsertArgs Add<TItem, TDestination>(TItem[] items, TDestination collection)
        {
            return Add(items, [collection]);
        }

        /// <summary>
        /// Add the associations between collections of items and destination items.
        /// </summary>
        /// <typeparam name="TItem">Type of the source items.</typeparam>
        /// <typeparam name="TDestination">Type of the destination items.</typeparam>
        /// <param name="items">Source items.</param>
        /// <param name="collections">Destination items to associate.</param>
        public static GXInsertArgs Add<TItem, TDestination>(TItem[] items, TDestination[] collections)
        {
            object? collectionId, id;
            if (items == null || collections == null || items.Length == 0 || collections.Length == 0)
            {
                throw new ArgumentNullException("Invalid value");
            }
            Type itemType = typeof(TItem);
            Type collectionType = typeof(TDestination);
            GXSerializedItem si = GXSqlBuilder.FindRelation(itemType, collectionType);
            if (si.Relation == null || si.Relation.RelationMapTable == null)
            {
                throw new ArgumentNullException("Invalid collection");
            }
            GXInsertArgs args = new GXInsertArgs();
            GXSerializedItem siItem = GXSqlBuilder.FindRelation(collectionType, itemType);
            foreach (TDestination c in collections)
            {
                //Get collection id.
                collectionId = si.Relation.RelationMapTable.Relation.ForeignId.Get(c!);
                foreach (TItem it in items)
                {
                    object target = GXInternal.CreateClass(si.Relation.RelationMapTable.Relation.PrimaryTable);
                    si.Relation.RelationMapTable.Relation.PrimaryId.Set(target, collectionId);
                    //Get item id.
                    id = siItem.Relation.RelationMapTable.Relation.ForeignId.Get(it!);
                    siItem.Relation.RelationMapTable.Relation.PrimaryId.Set(target, id);
                    args.Values.Add(new KeyValuePair<object?, LambdaExpression?>(target, null));
                }
            }
            return args;
        }

        /// <summary>
        /// Add given item to the n:n collection.
        /// </summary>
        /// <typeparam name="TItem">Type of the source item.</typeparam>
        /// <typeparam name="TDestination">Type of the destination item.</typeparam>
        /// <param name="item">Source item.</param>
        /// <param name="collection">Destination item to associate.</param>
        public static GXInsertArgs Add<TItem, TDestination>(TItem item, TDestination collection)
        {
            return Add([item], new TDestination[] { collection });
        }

        /// <summary>
        /// Remove item from the n:n collection.
        /// </summary>
        /// <typeparam name="TItem">Type of the source item.</typeparam>
        /// <typeparam name="TDestination">Type of the destination item.</typeparam>
        /// <param name="item">Source item.</param>
        /// <param name="collection">Destination item to disassociate.</param>
        public static GXInsertArgs Remove<TItem, TDestination>(TItem item, TDestination collection)
        {
            return null;
        }

        /// <summary>
        /// Exclude columns from the insert.
        /// </summary>
        /// <typeparam name="T">Table type.</typeparam>
        /// <param name="columns">Columns to exclude.</param>
        public void Exclude<T>(Expression<Func<T, object>> columns)
        {
            Excluded.Add(new KeyValuePair<Type, LambdaExpression>(typeof(T), columns));
        }
    }
}
