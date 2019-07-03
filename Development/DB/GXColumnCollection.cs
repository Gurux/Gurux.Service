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

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using Gurux.Service.Orm.Settings;
using System.Reflection;
using Gurux.Common.Internal;

namespace Gurux.Service.Orm
{
    /// <summary>
    /// Collection of columns in select expression.
    /// </summary>
    public class GXColumnCollection
    {
        /// <summary>
        /// List of tables and columns to get.
        /// </summary>
        internal Dictionary<Type, List<string>> ColumnList = new Dictionary<Type, List<string>>();

        internal List<LambdaExpression> List = new List<LambdaExpression>();
        internal GXJoinCollection Joins;
        GXSettingsArgs Parent;
        internal bool Updated;
        string sql;


        /// <summary>
        /// Constructor.
        /// </summary>
        internal GXColumnCollection(GXSettingsArgs parent)
        {
            Parent = parent;
        }

        static private bool IsExcluded(string it, List<string> excluded)
        {
            return excluded != null && excluded.Count != 0 && excluded.Contains(it);
        }

        private void GetRelations(Type type, List<GXJoin> joinList, List<Type> tables,
            Dictionary<Type, List<string>> columns, bool getRelations)
        {
            if (!tables.Contains(type))
            {
                //TODO: lisää shared table attribute.
                bool ignorebaseType = false;
                foreach (var it in tables)
                {
                    if (GXDbHelpers.IsSharedTable(it) && type.IsAssignableFrom(it.BaseType))
                    {
                        ignorebaseType = true;
                        break;
                    }
                }
                if (ignorebaseType)
                {
                    return;
                }
                tables.Add(type);

                Type tp;
                bool added;
                List<string> excluded = null;
                List<string> cols = null;
                string tableName = null;
                if (columns != null && columns.ContainsKey(type))
                {
                    cols = columns[type];
                    if (cols.Contains("*"))
                    {
                        cols = null;
                    }
                }
                Dictionary<string, GXSerializedItem> properties = GXSqlBuilder.GetProperties(type);
                foreach (var it in properties)
                {
                    if (cols != null && !cols.Contains(it.Key))
                    {
                        continue;
                    }
                    if (!IsExcluded(it.Key, excluded))
                    {
                        if (it.Value.Relation != null && getRelations)
                        {
                            GXJoin j = new GXJoin();
                            tp = it.Value.Relation.PrimaryTable;
                            j.Table1Type = tp;
                            bool shared = GXDbHelpers.IsSharedTable(tp);
                            if (shared || GXDbHelpers.IsAliasName(tp))
                            {
                                j.Alias1 = GXDbHelpers.OriginalTableName(tp);
                            }
                            j.Table2Type = it.Value.Relation.ForeignTable;
                            j.UpdateTables(tp, it.Value.Relation.ForeignTable);
                            //If nullable.
                            Type tp2 = it.Value.Relation.ForeignId.Type;
                            j.AllowNull1 = !shared && tp2.IsGenericType && tp2.GetGenericTypeDefinition() == typeof(Nullable<>);
                            if (!j.AllowNull1)
                            {
                                tp2 = it.Value.Relation.PrimaryId.Type;
                                j.AllowNull2 = !GXDbHelpers.IsSharedTable(it.Value.Relation.ForeignTable)
                                    && tp2.IsGenericType && tp2.GetGenericTypeDefinition() == typeof(Nullable<>);
                            }
                            if (GXDbHelpers.IsSharedTable(it.Value.Relation.ForeignTable) ||
                                 GXDbHelpers.IsAliasName(it.Value.Relation.ForeignTable))
                            {
                                j.Alias2 = GXDbHelpers.OriginalTableName(it.Value.Relation.ForeignTable);
                            }
                            if (it.Value.Relation.RelationType == RelationType.OneToOne ||
                                it.Value.Relation.RelationType == RelationType.Relation)
                            {
                                j.Column2 = GXDbHelpers.GetColumnName(it.Value.Relation.PrimaryId.Relation.ForeignId.Target as PropertyInfo, Parent.Settings.ColumnQuotation);
                                j.Column1 = GXDbHelpers.GetColumnName(it.Value.Relation.PrimaryId.Target as PropertyInfo, Parent.Settings.ColumnQuotation);
                                if (tables.Contains(it.Value.Relation.ForeignTable))
                                {
                                    continue;
                                }
                                //If nullable.
                                tp2 = it.Value.Relation.PrimaryId.Relation.ForeignId.Type;
                                j.AllowNull1 = !shared && tp2.IsGenericType && tp2.GetGenericTypeDefinition() == typeof(Nullable<>);
                                if (!j.AllowNull1)
                                {
                                    tp2 = it.Value.Relation.PrimaryId.Relation.PrimaryId.Type;
                                    j.AllowNull2 = !GXDbHelpers.IsSharedTable(it.Value.Relation.ForeignTable) &&
                                            tp2.IsGenericType && tp2.GetGenericTypeDefinition() == typeof(Nullable<>);
                                }
                            }
                            else if (it.Value.Relation.RelationType == RelationType.OneToMany)
                            {
                                j.Column1 = GXDbHelpers.GetColumnName(it.Value.Relation.PrimaryId.Relation.ForeignId.Target as PropertyInfo, Parent.Settings.ColumnQuotation);
                                j.Column2 = GXDbHelpers.GetColumnName(it.Value.Relation.PrimaryId.Target as PropertyInfo, Parent.Settings.ColumnQuotation);
                                //If nullable.
                                tp2 = it.Value.Relation.ForeignId.Type;
                                j.AllowNull1 = !shared && tp2.IsGenericType && tp2.GetGenericTypeDefinition() == typeof(Nullable<>);
                                if (!j.AllowNull1)
                                {
                                    tp2 = it.Value.Relation.PrimaryId.Type;
                                    j.AllowNull2 = !GXDbHelpers.IsSharedTable(it.Value.Relation.ForeignTable) &&
                                        tp2.IsGenericType && tp2.GetGenericTypeDefinition() == typeof(Nullable<>);
                                }
                            }
                            else if (it.Value.Relation.RelationType == RelationType.ManyToMany)
                            {
                                j.Table2Type = it.Value.Relation.RelationMapTable.Relation.PrimaryTable;
                                j.UpdateTables(tp, it.Value.Relation.RelationMapTable.Relation.PrimaryTable);
                                j.Column2 = GXDbHelpers.GetColumnName(GXSqlBuilder.FindRelation(it.Value.Relation.RelationMapTable.Relation.PrimaryTable, tp).Target as PropertyInfo, Parent.Settings.ColumnQuotation);
                                j.Column1 = GXDbHelpers.GetColumnName(GXSqlBuilder.FindUnique(tp).Target as PropertyInfo, Parent.Settings.ColumnQuotation);
                                added = false;
                                //Check that join is not added already.
                                string j1 = j.Table1;
                                string j2 = j.Table2;
                                foreach (var it4 in joinList)
                                {
                                    string t1 = it4.Table1;
                                    string t2 = it4.Table2;
                                    if ((j1 == t1 && j2 == t2) || (j2 == t1 && j1 == t2))
                                    {
                                        added = true;
                                        break;
                                    }
                                }
                                if (!added)
                                {
                                    joinList.Add(j);
                                    j = new GXJoin();
                                    Type tmp = it.Value.Type;
                                    j.AllowNull1 = !!GXDbHelpers.IsSharedTable(tmp) && tmp.IsGenericType && tmp.GetGenericTypeDefinition() == typeof(Nullable<>);
                                    j.UpdateTables(it.Value.Relation.RelationMapTable.Relation.PrimaryTable, it.Value.Relation.ForeignTable);
                                    j.Column1 = GXDbHelpers.GetColumnName(it.Value.Relation.RelationMapTable.Target as PropertyInfo, Parent.Settings.ColumnQuotation);
                                    j.Column2 = GXDbHelpers.GetColumnName(it.Value.Relation.ForeignId.Target as PropertyInfo, Parent.Settings.ColumnQuotation);
                                    joinList.Add(j);
                                    tables.Add(it.Value.Relation.RelationMapTable.Relation.PrimaryTable);
                                    if (cols != null)
                                    {
                                        tables.Add(it.Value.Relation.ForeignTable);
                                    }
                                }
                                j = null;
                            }
                            if (j != null)
                            {
                                //Check that join is not added already.
                                added = false;
                                string j1 = j.Table1;
                                string j2 = j.Table2;
                                int index = -1;
                                foreach (var it4 in joinList)
                                {
                                    string t1 = it4.Table1;
                                    string t2 = it4.Table2;
                                    if ((j1 == t1 && j2 == t2) || (j2 == t1 && j1 == t2))
                                    {
                                        added = true;
                                        break;
                                    }
                                    if (j.Alias2 != null && it4.Alias2 == j.Alias2)
                                    {
                                        ++index;
                                    }
                                }
                                //If we have multiple referenced to same table.
                                if (index != -1)
                                {
                                    j.Index = index + 1;
                                }
                                if (!added)
                                {
                                    joinList.Add(j);
                                }
                            }
                            if (cols == null || cols.Contains(it.Key))
                            {
                                GetRelations(it.Value.Relation.ForeignTable, joinList, tables, columns, getRelations);
                            }

                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Column " + tableName + "." + it.Key + " Skipped.");
                    }
                }
            }
        }

        /// <summary>
        /// Get columns that are wanted to execute select query.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="columns"></param>
        /// <param name="tables"></param>
        private void GetColumns(Type type, Dictionary<Type, List<string>> columns, Dictionary<Type, GXSerializedItem> tables)
        {
            if (tables.ContainsKey(type))
            {
                bool exists = columns.ContainsKey(type);
                List<string> list;
                if (exists)
                {
                    list = columns[type];
                }
                else
                {
                    list = new List<string>();
                }
                tables.Remove(type);
                string tableName = null;
                Dictionary<string, GXSerializedItem> properties = GXSqlBuilder.GetProperties(type);
                foreach (var it in properties)
                {
                    if (!list.Contains(it.Key))
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
                                list.Add(it.Key);
                            }
                        }
                        else
                        {
                            list.Add(it.Key);
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Column " + tableName + "." + it.Key + " is excluded.");
                    }
                }
                if (!exists && list.Count != 0)
                {
                    columns.Add(type, list);
                }
            }
        }

        public override string ToString()
        {
            if (Parent.Updated || Updated)
            {
                ColumnList.Clear();
                List<GXJoin> joinList = new List<GXJoin>();
                GXOrderByCollection.UpdateJoins(Parent.Settings, Joins, joinList);
                string[] list;
                StringBuilder sb = new StringBuilder();
                //Get columns.
                Dictionary<string, GXSerializedItem> properties;
                Dictionary<Type, GXSerializedItem> neededTables = new Dictionary<Type, GXSerializedItem>();
                //List of tables that are already go through in join.
                List<Type> joinTables = new List<Type>();
                foreach (var it in List)
                {
                    //No relations.
                    if (!neededTables.ContainsKey(it.Parameters[0].Type))
                    {
                        neededTables.Add(it.Parameters[0].Type, null);
                    }
                    list = GXDbHelpers.GetMembers(Parent.Settings, it, '\0', false);
                    foreach (var it2 in list)
                    {
                        properties = GXSqlBuilder.GetProperties(it.Parameters[0].Type);
                        if (it2 != "*" && ColumnList.ContainsKey(it.Parameters[0].Type))
                        {
                            GXSerializedItem si = properties[it2];
                            if (si.Relation != null)
                            {
                                //Get properties.
                                GetColumns(si.Relation.ForeignTable, ColumnList, neededTables);
                            }
                            if (si.Relation == null || si.Relation.RelationType != RelationType.ManyToMany)
                            {
                                ColumnList[it.Parameters[0].Type].Add(it2);
                            }
                        }
                        else
                        {
                            if (it2 == "*")
                            {
                                GetColumns(it.Parameters[0].Type, ColumnList, neededTables);
                            }
                            else
                            {
                                if (neededTables.ContainsKey(it.Parameters[0].Type))
                                {
                                    neededTables.Remove(it.Parameters[0].Type);
                                }

                                List<string> columns2 = new List<string>();
                                columns2.Add(it2);
                                ColumnList.Add(it.Parameters[0].Type, columns2);
                                if (properties.ContainsKey(it2))
                                {
                                    GXSerializedItem si = properties[it2];
                                    if (si.Relation != null)
                                    {
                                        //Get properties.
                                        GetColumns(si.Relation.ForeignTable, ColumnList, neededTables);
                                    }
                                }
                            }
                        }
                    }
                }
                SelectToString(Parent.Settings, sb, Parent.Distinct, ColumnList, joinList, Parent.Index, Parent.Count);
                sql = sb.ToString();
                Updated = false;
            }
            return sql;
        }

        public void Add<T>()
        {
            Add<T>(q => "*");
        }

        /// <summary>
        /// Add new item to expression list.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="expression"></param>
        public void Add<T>(Expression<Func<T, object>> expression)
        {
            Updated = true;
            List.Add(expression);
        }

        /// <summary>
        /// Clear expression list.
        /// </summary>
        public void Clear()
        {
            Updated = true;
            List.Clear();
        }

        private static string GetNameByAliasName(string aliasName, List<KeyValuePair<string, string>> list)
        {
            foreach (var it in list)
            {
                if (string.Compare(it.Key, aliasName, true) == 0)
                {
                    return it.Value;
                }
            }
            return null;
        }

        /// <summary>
        /// Get as name.
        /// </summary>
        /// <param name="joinList"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        /// <remarks>
        /// As name is used if:
        /// 1. Data from different classes is saved to same table.
        /// 2. Several classes are using same class.
        /// 3. User is defined it using Alias attribute.
        /// </remarks>
        internal static string GetAsName(List<GXJoin> joinList, Type type, bool selectUsingAs)
        {
            //Data from different classes is saved to same table.
            if (selectUsingAs || GXDbHelpers.IsSharedTable(type))
            {
                string name = GXDbHelpers.OriginalTableName(type);
                int cnt = 0;
                GXJoin target = null;
                foreach (GXJoin it in joinList)
                {
                    if (type == it.Table1Type)
                    {
                        target = it;
                    }
                    else if (it.Alias2 == name || it.Table2 == name)
                    {
                        ++cnt;
                        //If we have more than one class that are sharing same table.
                        if (cnt != 1)
                        {
                            break;
                        }
                    }
                }
                if (cnt > 1)
                {
                    name = GXDbHelpers.GetTableName(type, false, '\0', null, false);
                    if (target != null)
                    {
                        target.Alias2 = name;
                    }
                    return name;
                }
                if (selectUsingAs)
                {
                    return name;
                }
            }
            if (GXDbHelpers.IsAliasName(type))
            {
                return GXDbHelpers.OriginalTableName(type);
            }
            return null;
        }

        private static void SelectToString(GXDBSettings settings, StringBuilder sb, bool distinct,
                Dictionary<Type, List<string>> columnList, List<GXJoin> joinList, UInt32 index, UInt32 count)
        {
            Dictionary<Type, string> asTable = new Dictionary<Type, string>();
            string name;
            foreach (var it in columnList)
            {
                name = GetAsName(joinList, it.Key, settings.SelectUsingAs);
                if (name != null)
                {
                    asTable.Add(it.Key, name);
                }
            }
            sb.Append("SELECT ");
            if (distinct)
            {
                sb.Append("DISTINCT ");
            }
            if (index != 0 || count != 0)
            {
                if (index != 0 && count == 0)
                {
                    throw new ArgumentOutOfRangeException("Count can't be zero if index is given.");
                }
                if (index != 0)
                {
                    if (settings.LimitType == LimitType.Top)
                    {
                        sb.Length = 0;
                        sb.Append("SELECT * FROM (SELECT TOP ");
                        sb.Append(count);
                        sb.Append(" GX.* FROM (");
                        sb.Append("SELECT ");
                        if (distinct)
                        {
                            sb.Append("DISTINCT ");
                        }
                        sb.Append("TOP ");
                        sb.Append(index + count);
                        sb.Append(" ");
                    }
                }
                else
                {
                    if (settings.LimitType == LimitType.Top)
                    {
                        sb.Append("TOP ");
                        sb.Append(count);
                        sb.Append(" ");
                    }
                }
            }
            string tableAs, table;
            bool first = true;
            foreach (var it in columnList)
            {
                if (asTable.ContainsKey(it.Key))
                {
                    tableAs = asTable[it.Key];
                }
                else
                {
                    tableAs = null;
                }
                if (GXDbHelpers.IsSharedTable(it.Key) || GXDbHelpers.IsAliasName(it.Key))
                {
                    table = GXDbHelpers.AddQuotes(GXDbHelpers.OriginalTableName(it.Key), settings.TableQuotation);
                }
                else
                {
                    table = GXDbHelpers.GetTableName(it.Key, true, settings.TableQuotation, settings.TablePrefix);
                }

                foreach (var col in it.Value)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        sb.Append(", ");
                    }
                    //Check is this method.
                    int pos = col.LastIndexOf('(');
                    //Table name is not added if only one table.
                    if (pos == -1 && joinList.Count != 0)
                    {
                        if (asTable.Count > 1 && tableAs != null)
                        {
                            sb.Append(tableAs);
                        }
                        else
                        {
                            sb.Append(table);
                        }
                        sb.Append(".");
                    }
                    if (pos == -1) //If table field.
                    {
                        if (col == "*")
                        {
                            sb.Append(col);
                        }
                        else
                        {
                            sb.Append(GXDbHelpers.AddQuotes(col, settings.ColumnQuotation));
                        }
                        if (tableAs != null)
                        {
                            sb.Append(" AS ");
                            sb.Append(GXDbHelpers.AddQuotes(tableAs + "." + col, settings.ColumnQuotation));
                        }
                        else if (settings.SelectUsingAs && index == 0)
                        {
                            sb.Append(" AS ");
                            sb.Append(GXDbHelpers.AddQuotes(tableAs + "." + col, settings.ColumnQuotation));
                        }
                    }
                    else //If method like COUNT(*)
                    {
                        sb.Append(col);
                        if (settings.SelectUsingAs && index == 0)
                        {
                            sb.Append(" AS ");
                            sb.Append(GXDbHelpers.AddQuotes(tableAs + "." + col.Substring(0, pos), settings.ColumnQuotation));
                        }
                    }
                }
            }
            if (columnList.Count == 0)
            {
                sb.Append("*");
            }
            if (joinList.Count == 0)
            {
                sb.Append(" FROM ");
                first = true;
                foreach (var it in columnList)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        sb.Append(", ");
                    }
                    sb.Append(GXDbHelpers.GetTableName(it.Key, settings.UseQuotationWhereColumns, settings.TableQuotation, settings.TablePrefix));
                    if (asTable.ContainsKey(it.Key))
                    {
                        sb.Append(" ");
                        sb.Append(GXDbHelpers.AddQuotes(asTable[it.Key], settings.TableQuotation));
                    }
                }
            }
            else
            {
                //If we are adding relation to same table more than once.
                Dictionary<string, int> list = new Dictionary<string, int>();
                sb.Append(" FROM ");
                first = true;
                if (joinList.Count != 1)
                {
                    for (int pos = 0; pos < joinList.Count; ++pos)
                    {
                        sb.Append("(");
                    }
                }
                foreach (var it in joinList)
                {
                    if (first)
                    {
                        sb.Append(GXDbHelpers.AddQuotes(it.Table1, settings.TableQuotation));
                        if (asTable.ContainsKey(it.Table1Type))
                        {
                            sb.Append(" ");
                            sb.Append(GXDbHelpers.AddQuotes(asTable[it.Table1Type], settings.TableQuotation));
                        }
                        else
                        {
                            if (it.Alias1 != null)
                            {
                                sb.Append(" ");
                                sb.Append(it.Alias1);
                            }
                        }
                        first = false;
                    }

                    switch (it.Type)
                    {
                        case JoinType.Inner:
                            sb.Append(" INNER JOIN ");
                            break;
                        case JoinType.Left:
                            sb.Append(" LEFT OUTER JOIN ");
                            break;
                        case JoinType.Right:
                            sb.Append(" RIGHT OUTER JOIN ");
                            break;
                        default:
                            throw new ArgumentOutOfRangeException("Invalid join type.");
                    }
                    sb.Append(GXDbHelpers.AddQuotes(it.Table2, settings.TableQuotation));
                    //Add alias if used and it is not same as table name.
                    if (asTable.ContainsKey(it.Table2Type))
                    {
                        sb.Append(" ");
                        sb.Append(GXDbHelpers.AddQuotes(asTable[it.Table2Type], settings.TableQuotation));
                    }
                    sb.Append(" ON ");
                    if (asTable.ContainsKey(it.Table1Type))
                    {
                        sb.Append(" ");
                        sb.Append(GXDbHelpers.AddQuotes(asTable[it.Table1Type], settings.TableQuotation));
                    }
                    else
                    {
                        sb.Append(GXDbHelpers.AddQuotes(it.Table1, settings.TableQuotation));
                    }
                    sb.Append('.');
                    sb.Append(it.Column1);
                    sb.Append('=');
                    //Add alias if used.
                    if (asTable.ContainsKey(it.Table2Type))
                    {
                        //sb.Append(it.Table2);
                        sb.Append(GXDbHelpers.AddQuotes(asTable[it.Table2Type], settings.TableQuotation));
                    }
                    else
                    {
                        sb.Append(GXDbHelpers.AddQuotes(it.Table2, settings.TableQuotation));
                        if (it.Alias2 == null && it.Index != 0)
                        {
                            sb.Append(it.Index);
                        }
                    }
                    sb.Append('.');
                    sb.Append(it.Column2);
                    if (it.AllowNull1)
                    {
                        sb.Append(" OR ");
                        //Add alias if used.
                        if (asTable.ContainsKey(it.Table1Type))
                        {
                            sb.Append(GXDbHelpers.AddQuotes(asTable[it.Table1Type], settings.TableQuotation));
                        }
                        else
                        {
                            sb.Append(GXDbHelpers.AddQuotes(it.Table1, settings.TableQuotation));
                        }
                        sb.Append('.');
                        sb.Append(it.Column1);
                        sb.Append(" IS NULL");
                    }

                    if (it.AllowNull2)
                    {
                        sb.Append(" OR ");
                        //Add alias if used.
                        if (asTable.ContainsKey(it.Table2Type))
                        {
                            sb.Append(GXDbHelpers.AddQuotes(asTable[it.Table2Type], settings.TableQuotation));
                        }
                        else
                        {
                            sb.Append(GXDbHelpers.AddQuotes(it.Table2, settings.TableQuotation));
                        }
                        sb.Append('.');
                        sb.Append(it.Column2);
                        sb.Append(" IS NULL");
                    }

                    if (joinList.Count != 1)
                    {
                        sb.Append(")");
                    }
                }
            }
            if (index != 0)
            {
                if (settings.LimitType == LimitType.Top)
                {
                    List<Type> tables = new List<Type>();
                    foreach (var it in columnList)
                    {
                        if (!tables.Contains(it.Key))
                        {
                            tables.Add(it.Key);
                        }
                    }
                    GXSerializedItem si = null;
                    foreach (var it in tables)
                    {
                        if ((si = GXSqlBuilder.FindUnique(it)) != null)
                        {
                            break;
                        }
                    }
                    string id;
                    //Add alias if used.
                    if (asTable.ContainsKey((si.Target as PropertyInfo).ReflectedType))
                    {
                        id = GXDbHelpers.AddQuotes(asTable[(si.Target as PropertyInfo).ReflectedType] + "." + GXDbHelpers.GetColumnName(si.Target as PropertyInfo, '\0'), settings.TableQuotation);
                    }
                    else
                    {
                        id = GXDbHelpers.GetColumnName(si.Target as PropertyInfo, '\0');
                    }

                    sb.Append(string.Format(" ORDER BY {0}) AS GX ORDER BY GX.{0} DESC) AS GX2", id));
                }
            }
        }
    }
}
