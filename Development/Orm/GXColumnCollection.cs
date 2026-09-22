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
using Gurux.Service.Orm.Common.Model;
using Gurux.Service.Orm.Enums;
using Gurux.Service.Orm.Internal;
using Gurux.Service.Orm.Settings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Gurux.Service.Orm
{
    /// <summary>
    /// Collection of columns in select expression.
    /// </summary>
    public class GXColumnCollection
    {
        internal bool Insert = false;
        /// <summary>
        /// Target columns.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal List<KeyValuePair<Type, GXSerializedItem>> Columns = new List<KeyValuePair<Type, GXSerializedItem>>();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal Dictionary<string, string> Maps = new Dictionary<string, string>();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal List<KeyValuePair<LambdaExpression, LambdaExpression?>> List = new List<KeyValuePair<LambdaExpression, LambdaExpression?>>();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal List<GXColumnSchema> SchemaColumns = new List<GXColumnSchema>();
        /// <summary>
        /// List of values to exlude from update.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal List<KeyValuePair<Type, LambdaExpression>> Excluded = new List<KeyValuePair<Type, LambdaExpression>>();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal readonly GXJoinCollection Joins;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        GXSettingsArgs Parent;

        /// <summary>
        /// Constructor.
        /// </summary>
        internal GXColumnCollection(GXSettingsArgs parent, GXJoinCollection joins)
        {
            Parent = parent;
            Joins = joins;
        }

        /// <summary>
        /// Get columns that are wanted to execute select query.
        /// </summary>
        /// <param name="type">The mapped CLR type.</param>
        /// <param name="columns">The columns selected by this operation.</param>
        /// <param name="tables">The table types referenced by the selection.</param>
        private void GetColumns(
            Type type,
            Dictionary<Type, List<(string, Type)>> columns,
            Dictionary<Type, GXSerializedItem> tables)
        {
            if (type == null)
            {
                throw new AccessViolationException("Type can't be null.");
            }
            if (tables.ContainsKey(type))
            {
                bool exists = columns.ContainsKey(type);
                List<(string, Type)> list;
                if (exists)
                {
                    list = columns[type];
                }
                else
                {
                    list = new List<(string, Type)>();
                }
                tables.Remove(type);
                string tableName = null;
                Dictionary<string, GXSerializedItem> properties = GXSqlBuilder.GetProperties(type);
                foreach (var it in properties)
                {
                    if (!list.Where(w => w.Item1 == it.Key).Any())
                    {
                        if (it.Value.Relation != null)
                        {
                            if (it.Value.Relation.RelationType == RelationType.ManyToMany)
                            {
                                GetColumns(it.Value.Relation.RelationMapTable.Relation.PrimaryTable, columns, tables);
                            }
                            else
                            {
                                GetColumns(it.Value.Relation.ForeignTable, columns, tables);
                            }
                            if (it.Value.Relation.RelationType == RelationType.OneToOne ||
                                it.Value.Relation.RelationType == RelationType.Relation)
                            {
                                list.Add((it.Key, it.Value.Type));
                            }
                        }
                        else
                        {
                            list.Add((it.Key, it.Value.Type));
                        }
                    }
                    else
                    {
                        Debug.WriteLine("Column " + tableName + "." + it.Key + " is excluded.");
                    }
                }
                if (!exists && list.Count != 0)
                {
                    columns.Add(type, list);
                }
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            string? post = null;
            return ToString(ref post);
        }

        internal string ToString(ref string? post)
        {
            string cacheKey = Parent.QueryCache.BuildKey(
                Parent.Settings.Type,
                List,
                Parent.QueryCache.GetHash(SchemaColumns),
                Excluded,
                Joins != null ? Joins.GetItemHash() : 0,
                Parent.QueryCache.GetHash(Maps),
                Parent.Distinct,
                Parent.Index,
                Parent.Count,
                Insert);
            if (Parent.QueryCache.TryGet(cacheKey, out string? cachedSql, out int generationTime))
            {
                Debug.WriteLine("Cache SQL: " + cachedSql);
                return cachedSql!;
            }
            List<GXJoin> joinList = new List<GXJoin>();
            GXOrderByCollection.UpdateJoins(Parent.Settings, Joins, joinList);
            GXGetMembersArgs args = new GXGetMembersArgs(Parent.Settings, TargetType.Column | TargetType.Plain)
            {
                StringBuilder = new StringBuilder(),
                SingleTable = !Joins.List.Any()
            };
            Columns.Clear();
            args.StringBuilder.Append("SELECT ");
            if (Parent.Distinct)
            {
                args.StringBuilder.Append("DISTINCT ");
            }
            if (Parent.Index != 0 || Parent.Count != 0)
            {
                if (Parent.Index != 0 && Parent.Count == 0)
                {
                    throw new ArgumentOutOfRangeException("Count can't be zero if index is given.");
                }
                if (Parent.Index != 0)
                {
                    if (Parent.Settings.LimitType == LimitType.Top)
                    {
                        args.StringBuilder.Length = 0;
                        args.StringBuilder.Append("SELECT * FROM (SELECT TOP ");
                        args.StringBuilder.Append(Parent.Count);
                        args.StringBuilder.Append(" GX.* FROM (");
                        args.StringBuilder.Append("SELECT ");
                        if (Parent.Distinct)
                        {
                            args.StringBuilder.Append("DISTINCT ");
                        }
                        args.StringBuilder.Append("TOP ");
                        args.StringBuilder.Append(Parent.Index + Parent.Count);
                        args.StringBuilder.Append(" ");
                    }
                }
                else
                {
                    if (Parent.Settings.LimitType == LimitType.Top)
                    {
                        args.StringBuilder.Append("TOP ");
                        args.StringBuilder.Append(Parent.Count);
                        args.StringBuilder.Append(" ");
                    }
                }
            }
            bool first = true;
            string table, name;
            if (SchemaColumns.Count != 0)
            {
                GXTableSchema? schema = SchemaColumns[0].Parent;
                if (schema == null)
                {
                    throw new ArgumentException("Column schema does not define parent table.");
                }
                foreach (GXColumnSchema it in SchemaColumns.OrderBy(c => c.Ordinal == 0 ? int.MaxValue : c.Ordinal))
                {
                    if (string.IsNullOrWhiteSpace(it.Name))
                    {
                        throw new ArgumentException("Column name is empty.");
                    }
                    if (!ReferenceEquals(schema, it.Parent))
                    {
                        throw new ArgumentException("All columns must belong to the same table schema.");
                    }
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        args.StringBuilder.Append(", ");
                    }
                    name = GXDbHelpers.ConvertToString(Parent.Settings, TargetType.Column, null, it.Name, null);
                    args.StringBuilder.Append(name);
                }
                args.StringBuilder.Append(" FROM ");
                args.StringBuilder.Append(GetSchemaTableName(schema));
                string schemaSql = args.StringBuilder.ToString();
                if (!string.IsNullOrEmpty(schemaSql))
                {
                    Parent.QueryCache.Set(cacheKey, schemaSql, 0);
                    Debug.WriteLine("New SQL: " + schemaSql);
                }
                return schemaSql;
            }
            foreach (var e in List)
            {
                args.Expression = e.Key;
                var excluded = GXDbHelpers.ExcludedProperties(Excluded, args, e.Key.Parameters[0].Type);
                string[] list = GXDbHelpers.GetMemberList(args);
                foreach (var col in list)
                {
                    int pos = col.LastIndexOf('(');
                    if (pos == -1)
                    {
                        Dictionary<string, GXSerializedItem> properties = GXSqlBuilder.GetProperties(e.Key.Parameters[0].Type);
                        if (col == "*")
                        {
                            foreach (var p in properties)
                            {
                                if ((p.Value.Attributes & Attributes.ForeignKey) != 0 &&
                                    p.Value.Relation?.RelationType != RelationType.OneToOne)
                                {
                                    continue;
                                }
                                if (!excluded.Contains(p.Key))
                                {
                                    if (first)
                                    {
                                        first = false;
                                    }
                                    else
                                    {
                                        args.StringBuilder.Append(", ");
                                    }
                                    Columns.Add(new KeyValuePair<Type, GXSerializedItem>(e.Key.Parameters[0].Type, p.Value));
                                    if (Joins.List.Any())
                                    {
                                        table = GXDbHelpers.ConvertToString(Parent.Settings, TargetType.Table, null, e.Key.Parameters[0].Type, null);
                                        name = GXDbHelpers.ConvertToString(Parent.Settings, TargetType.Column, table, p.Value.Target as PropertyInfo, null);
                                        args.StringBuilder.Append(name);
                                    }
                                    else
                                    {
                                        name = GXDbHelpers.ConvertToString(Parent.Settings, TargetType.Column, null, p.Key, null);
                                        args.StringBuilder.Append(name);
                                    }
                                }
                            }
                        }
                        else
                        {
                            var p = properties.Where(w => w.Key == col).Single();
                            if (!excluded.Contains(col))
                            {
                                if (first)
                                {
                                    first = false;
                                }
                                else
                                {
                                    args.StringBuilder.Append(", ");
                                }
                                Columns.Add(new KeyValuePair<Type, GXSerializedItem>(e.Key.Parameters[0].Type, p.Value));
                                if (Joins.List.Any())
                                {
                                    table = GXDbHelpers.ConvertToString(Parent.Settings, TargetType.Table, null, e.Key.Parameters[0].Type, null);
                                    name = GXDbHelpers.ConvertToString(Parent.Settings, TargetType.Column, table, p.Value.Target as PropertyInfo, null);
                                    args.StringBuilder.Append(name);
                                }
                                else
                                {
                                    name = GXDbHelpers.ConvertToString(Parent.Settings, TargetType.Column, null, p.Key, null);
                                    args.StringBuilder.Append(name);
                                }
                            }
                        }
                    }
                    else //If method like COUNT(*)
                    {
                        if (col == "1()")
                        {
                            args.StringBuilder.Append('1');
                        }
                        else if (col == "COUNT(1())")
                        {
                            args.StringBuilder.Append("COUNT(1)");
                            pos -= 2;
                        }
                        else if (col == "COUNT(1)")
                        {
                            args.StringBuilder.Append("COUNT(1)");
                        }
                        else
                        {
                            if (!joinList.Any())
                            {
                                args.StringBuilder.Append(col);
                            }
                            else
                            {
                                if (col.StartsWith("COUNT(DISTINCT"))
                                {
                                    pos = 14;
                                }
                                name = col.Substring(pos + 1, col.Length - pos - 2);
                                name = col.Substring(0, pos + 1) + name + ")";
                                args.StringBuilder.Append(name);
                            }
                        }
                    }
                }
            }
            args.StringBuilder.Append(" FROM ");
            if (!joinList.Any())
            {
                Type tmp = List.First().Key.Parameters[0].Type;
                args.StringBuilder.Append(GXDbHelpers.ConvertToString(Parent.Settings, TargetType.Table, null, tmp, null));
            }
            else
            {
                first = true;
                foreach (var it in joinList)
                {
                    if (first)
                    {
                        first = false;
                        args.StringBuilder.Append(GXDbHelpers.ConvertToString(Parent.Settings, TargetType.Table, null, it.Table1, null));
                    }
                    switch (it.Type)
                    {
                        case JoinType.Inner:
                            args.StringBuilder.Append(" INNER JOIN ");
                            break;
                        case JoinType.Left:
                            args.StringBuilder.Append(" LEFT OUTER JOIN ");
                            break;
                        case JoinType.Right:
                            args.StringBuilder.Append(" RIGHT OUTER JOIN ");
                            break;
                        case JoinType.Full:
                            args.StringBuilder.Append(" FULL OUTER JOIN ");
                            break;
                        default:
                            throw new ArgumentOutOfRangeException("Invalid join type.");
                    }
                    args.StringBuilder.Append(GXDbHelpers.ConvertToString(Parent.Settings, TargetType.Table, null, it.Table2, null));
                    args.StringBuilder.Append(" ON ");
                    table = GXDbHelpers.ConvertToString(Parent.Settings, TargetType.Table, null, it.Table1, null);
                    args.StringBuilder.Append(GXDbHelpers.ConvertToString(Parent.Settings, TargetType.Column, table, it.Column1, null));
                    args.StringBuilder.Append(" = ");
                    table = GXDbHelpers.ConvertToString(Parent.Settings, TargetType.Table, null, it.Table2, null);
                    args.StringBuilder.Append(GXDbHelpers.ConvertToString(Parent.Settings, TargetType.Column, table, it.Column2, null));
                    args.StringBuilder.Append(' ');
                }
                --args.StringBuilder.Length;
            }
            if (args.Post != null)
            {
                post = args.Post;
                args.Post = null;
            }
            string sql = args.StringBuilder.ToString();
            if (!string.IsNullOrEmpty(sql))
            {
                Parent.QueryCache.Set(cacheKey, sql, 0);
                Debug.WriteLine("New SQL: " + sql);
            }
            return sql;
        }

        /// <summary>
        /// Select all columns from the table.
        /// </summary>
        /// <typeparam name="T">Table type.</typeparam>
        public void Add<T>()
        {
            Add<T>(_ => "*");
        }

        /// <summary>
        /// Add new item to expression list.
        /// </summary>
        /// <typeparam name="T">The mapped entity type.</typeparam>
        /// <param name="expression">The expression identifying the selected members.</param>
        public void Add<T>(Expression<Func<T, object>> expression)
        {
            List.Add(new(expression, null));
        }

        /// <summary>
        /// Add columns from table schema.
        /// </summary>
        /// <param name="columns">Schema columns.</param>
        internal void Add(IEnumerable<GXColumnSchema> columns)
        {
            SchemaColumns.AddRange(columns);
        }

        private string GetSchemaTableName(GXTableSchema schema)
        {
            if (string.IsNullOrWhiteSpace(schema.Name))
            {
                throw new ArgumentException("Table name is empty.");
            }
            string tableName = Parent.Settings.EscapeIdentifier(Parent.Settings.TablePrefix, schema.Name);
            if (string.IsNullOrWhiteSpace(schema.Schema))
            {
                return tableName;
            }
            return Parent.Settings.EscapeIdentifier(null, schema.Schema) + "." + tableName;
        }

        /// <summary>
        /// Add new item to expression list where result is saved to target property.
        /// </summary>
        /// <remarks>
        /// This can be used when items count is read from the database and it's saved to the variable.
        /// </remarks>
        /// <typeparam name="T">The mapped entity type.</typeparam>
        /// <param name="expression">Lambda expression.</param>
        /// <param name="target">Target where read data is saved.</param>
        /// <example>
        /// <code>
        /// GXSelectArgs arg = GXSelectArgs.Select&lt;TestClass&gt;(q => q.Text);
        /// arg.Columns.Add&lt;TestClass&gt;(q => GXSql.Count(q), n => n.IntTest);
        /// </code>
        /// </example>
        public void Add<T>(Expression<Func<T, object>> expression, Expression<Func<T, object>> target)
        {
            List?.Add(new KeyValuePair<LambdaExpression, LambdaExpression>(expression, target));
        }

        /// <summary>
        /// Clear expression list.
        /// </summary>
        public void Clear()
        {
            List.Clear();
            SchemaColumns.Clear();
        }

        /// <summary>
        /// Exclude columns from the query or update.
        /// </summary>
        /// <typeparam name="T">Object where columns are excluded.</typeparam>
        /// <param name="columns">Excluded columns.</param>
        public void Exclude<T>(Expression<Func<T, object>> columns)
        {
            Excluded.Add(new KeyValuePair<Type, LambdaExpression>(typeof(T), columns));
        }
    }
}
