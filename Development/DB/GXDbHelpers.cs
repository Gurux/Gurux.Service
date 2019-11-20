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
using System.Linq;
using System.Text;
using Gurux.Service.Orm.Settings;
using System.Linq.Expressions;
using Gurux.Common;
using Gurux.Common.Internal;
using System.Runtime.Serialization;
using System.Reflection;
using System.Collections;

namespace Gurux.Service.Orm
{
    static class GXDbHelpers
    {
        /// <summary>
        /// Add quotes around the value.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="quoteSeparator"></param>
        /// <returns></returns>
        internal static string AddQuotes(string value, char quoteSeparator)
        {
            if (quoteSeparator != '\0')
            {
                if (quoteSeparator == '[')
                {
                    return '[' + value + ']';
                }
                return quoteSeparator + value + quoteSeparator;
            }
            return value;
        }

        private static string GetQuetedValue(string value)
        {
            return '\'' + value + '\'';
        }


        internal static string GetTableName(Type type, bool addQuoteSeparator, GXDBSettings settings)
        {
            if (settings == null)
            {
                return GetTableName(type, addQuoteSeparator, '\0', null);
            }
            return GetTableName(type, addQuoteSeparator, settings.TableQuotation, settings.TablePrefix);
        }

        internal static string OriginalTableName(Type type)
        {
            AliasAttribute[] alias = (AliasAttribute[])type.GetCustomAttributes(typeof(AliasAttribute), true);
            if (alias.Length != 0 && alias[0].Name != null)
            {
                return alias[0].Name;
            }
            DataContractAttribute[] attr = (DataContractAttribute[])type.GetCustomAttributes(typeof(DataContractAttribute), true);
            if (attr.Length == 0 || attr[0].Name == null)
            {
                if (type.BaseType != typeof(object) && type.BaseType.GetCustomAttributes(typeof(DataContractAttribute), true).Length != 0)
                {
                    return OriginalTableName(type.BaseType);
                }
                return type.Name;
            }
            return attr[0].Name;
        }

        internal static bool IsAliasName(Type type)
        {
            return type.GetCustomAttributes(typeof(AliasAttribute), true).Length != 0;
        }

        internal static bool IsSharedTable(Type type)
        {
            return type.BaseType != typeof(object) && type.BaseType.GetCustomAttributes(typeof(DataContractAttribute), true).Length != 0;
        }

        internal static string GetTableName(Type type, bool addQuotation, char tableQuotation, string tablePrefix)
        {
            return GetTableName(type, addQuotation, tableQuotation, tablePrefix, true);
        }

        internal static string GetTableName(Type type, bool addQuotation, char tableQuotation, string tablePrefix, bool allowSharedTables)
        {
            if (allowSharedTables && type.BaseType != typeof(object) && type.BaseType.GetCustomAttributes(typeof(DataContractAttribute), true).Length != 0)
            {
                return GetTableName(type.BaseType, addQuotation, tableQuotation, tablePrefix, allowSharedTables);
            }
            DataContractAttribute[] attr = (DataContractAttribute[])type.GetCustomAttributes(typeof(DataContractAttribute), true);
            if (attr.Length == 0 || attr[0].Name == null)
            {
                if (!addQuotation)
                {
                    return tablePrefix + type.Name;
                }
                return GXDbHelpers.AddQuotes(tablePrefix + type.Name, tableQuotation);
            }
            if (!addQuotation)
            {
                return tablePrefix + attr[0].Name;
            }
            return GXDbHelpers.AddQuotes(tablePrefix + attr[0].Name, tableQuotation);
        }

        /// <summary>
        /// Convert value to string that can be add to DB.
        /// </summary>
        /// <param name="value">Converter value.</param>
        /// <param name="useEpochTimeFormat">Is epoch time format used.</param>
        /// <param name="useEnumStringValue">Is Enum value saved by name or by integer value.</param>
        /// <returns>Converted value.</returns>
        static internal string ConvertToString(object value, GXDBSettings settings, bool where)
        {
            string str;
            if (value == null)
            {
                str = "NULL";
            }
            else if (value is string)
            {
                str = value as string;
                str = str.Replace("\\", "\\\\").Replace("'", @"\''");
                str = GetQuetedValue(str);
            }
            else if (value is Type)
            {
                str = GetQuetedValue(((Type)value).FullName);
            }
            else if (value is float || value is double || value is System.Decimal)
            {
                str = settings.ConvertToString(value, where);
            }
            else if (value.GetType().IsEnum)
            {
                if (settings.UseEnumStringValue)
                {
                    str = GetQuetedValue(Convert.ToString(value));
                }
                else
                {
                    str = Convert.ToString((int)value);
                }
            }
            else if (value is TimeSpan)
            {
                str = ((TimeSpan)value).TotalMilliseconds.ToString();
            }
            else if (value is bool)
            {
                str = (bool)value ? "1" : "0";
            }
            else if (value is Guid)
            {
                str = GetQuetedValue(((Guid)value).ToString().Replace("-", ""));
            }
            else if (value is char)
            {
                str = GetQuetedValue(Convert.ToString(value));
            }
            else if (value is byte[])
            {
                str = GetQuetedValue(GXCommon.ToHex(value as byte[], false));
            }
            else if (value is DateTime)
            {
                if (settings.UseEpochTimeFormat)
                {
                    DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    str = (((DateTime)value).ToUniversalTime() - epoch).TotalSeconds.ToString();
                }
                else
                {
                    DateTime tm = (DateTime)value;
                    if (settings.UniversalTime && tm != DateTime.MinValue && tm != DateTime.MaxValue)
                    {
                        tm = tm.ToUniversalTime();
                    }
                    str = settings.ConvertToString(tm, where);
                }
            }
            else if (value is DateTimeOffset)
            {
                DateTimeOffset dt = (DateTimeOffset)value;
                if (settings.UniversalTime && dt != DateTime.MinValue && dt != DateTime.MaxValue)
                {
                    str = settings.ConvertToString(dt.ToUniversalTime(), where);
                }
                else
                {
                    str = settings.ConvertToString(dt, where);
                }
            }
            else if (value is System.Collections.IEnumerable)//If collection
            {
                StringBuilder sb = new StringBuilder();
                foreach (object it in (System.Collections.IEnumerable)value)
                {
                    sb.Append(it.ToString());
                    sb.Append(';');
                }
                if (sb.Length != 0)
                {
                    sb.Length = sb.Length - 1;
                }
                str = GetQuetedValue(sb.ToString());
            }
            else
            {
                str = Convert.ToString(value);
            }
            return str;
        }

        internal static string GetColumnName(MemberInfo info, char quoteSeparator)
        {
            DataMemberAttribute[] attr = (DataMemberAttribute[])info.GetCustomAttributes(typeof(DataMemberAttribute), true);
            if (attr.Length == 0 || attr[0].Name == null)
            {
                return GXDbHelpers.AddQuotes(info.Name, quoteSeparator);
            }
            return GXDbHelpers.AddQuotes(attr[0].Name, quoteSeparator);
        }

        /// <summary>
        /// Get all queries to execute.
        /// </summary>
        /// <param name="queries"></param>
        internal static void GetQueries(bool insert, GXDBSettings settings, List<KeyValuePair<object, LambdaExpression>> values, List<string> queries)
        {
            List<KeyValuePair<Type, GXUpdateItem>> list = new List<KeyValuePair<Type, GXUpdateItem>>();
            foreach (var it in values)
            {
                GetValues(it.Key, null, it.Value, list, insert, false, settings.ColumnQuotation, false);
            }
            foreach (var table in list)
            {
                if (insert)
                {
                    GetInsertQuery(table, settings, queries);
                }
                else
                {
                    GetUpdateQuery(table, settings, queries);
                }
            }
        }

        internal static void GetUpdateQuery(KeyValuePair<Type, GXUpdateItem> table, GXDBSettings settings, List<string> queries)
        {
            if (!table.Value.Inserted)
            {
                StringBuilder sb = new StringBuilder();
                int index = 0;
                object value;
                sb.Length = 0;
                string tableName = GXDbHelpers.GetTableName(table.Key, true, settings);
                bool first = true;
                foreach (var col in table.Value.Rows)
                {
                    sb.Append("UPDATE ");
                    sb.Append(tableName);
                    sb.Append(" SET ");
                    index = 0;
                    first = true;
                    foreach (var it in col)
                    {
                        if (first)
                        {
                            first = false;
                        }
                        else
                        {
                            sb.Append(", ");
                        }
                        sb.Append(GXDbHelpers.AddQuotes(table.Value.Columns[index], settings.ColumnQuotation));
                        sb.Append(" = ");
                        GXSerializedItem row = it.Value as GXSerializedItem;
                        if (row != null)
                        {
                            if (row.Get != null)
                            {
                                value = row.Get(it.Key);
                            }
                            else
                            {
                                value = GXInternal.GetValue(it.Key, row.Target);
                            }
                            if (row.Relation != null && value != null)
                            {
                                if (!GXInternal.IsGenericDataType(row.Type))
                                {
                                    GXSerializedItem si = GXSqlBuilder.FindUnique(row.Type);
                                    if (si != null)
                                    {
                                        value = si.Get(value);
                                    }
                                    else
                                    {
                                        value = null;
                                    }
                                }
                            }
                        }
                        else
                        {
                            value = it;
                        }
                        sb.Append(GXDbHelpers.ConvertToString(value, settings, false));
                        ++index;
                    }
                    if (!string.IsNullOrEmpty(table.Value.Where))
                    {
                        sb.Append(" WHERE ");
                        sb.Append(table.Value.Where);
                    }
                    queries.Add(sb.ToString());
                    sb.Length = 0;
                }
            }
        }

        private static void GetInsert(GXDBSettings settings, bool first, StringBuilder sb)
        {
            switch (settings.Type)
            {
                case DatabaseType.Oracle:
                    if (first)
                    {
                        sb.Append("INSERT ALL INTO ");
                    }
                    else
                    {
                        sb.Append(" INTO ");
                    }
                    break;
                default:
                    sb.Append("INSERT INTO ");
                    break;
            }
        }

        private static void GetInsertColumns(GXDBSettings settings, bool first, KeyValuePair<Type, GXUpdateItem> table, StringBuilder sb)
        {
            string tableName = GXDbHelpers.GetTableName(table.Key, true, settings);
            GetInsert(settings, first, sb);
            sb.Append(tableName);
            sb.Append(" (");
            bool empty = true;
            foreach (var col in table.Value.Columns)
            {
                if (empty)
                {
                    empty = false;
                }
                else
                {
                    sb.Append(", ");
                }
                sb.Append(GXDbHelpers.AddQuotes(col, settings.ColumnQuotation));
            }
            sb.Append(") VALUES(");
        }

        internal static int GetInsertQuery(KeyValuePair<Type, GXUpdateItem> table, GXDBSettings settings, List<string> queries)
        {
            StringBuilder sb = new StringBuilder();
            object value;
            sb.Length = 0;
            bool firstRow = true, first = true;
            GetInsertColumns(settings, true, table, sb);
            int rowCnt = 1;
            foreach (var col in table.Value.Rows)
            {
                if (firstRow)
                {
                    firstRow = false;
                    rowCnt = 1;
                }
                else
                {
                    if (settings.Type == DatabaseType.Oracle)
                    {
                        GetInsertColumns(settings, false, table, sb);
                    }
                    else
                    {
                        sb.Append(", (");
                    }
                    ++rowCnt;
                }
                first = true;
                foreach (var it in col)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        sb.Append(", ");
                    }
                    GXSerializedItem row = it.Value as GXSerializedItem;
                    if (row != null)
                    {
                        if (row.Get != null)
                        {
                            value = row.Get(it.Key);
                        }
                        else
                        {
                            value = GXInternal.GetValue(it.Key, row.Target);
                        }
                        if (row.Relation != null && value != null)
                        {
                            if (!GXInternal.IsGenericDataType(row.Type))
                            {
                                GXSerializedItem si = GXSqlBuilder.FindUnique(row.Type);
                                if (si != null)
                                {
                                    value = si.Get(value);
                                }
                                else
                                {
                                    value = null;
                                }
                            }
                        }
                    }
                    else
                    {
                        value = it;
                    }
                    sb.Append(GXDbHelpers.ConvertToString(value, settings, false));
                }
                sb.Append(")");
                //If all rows can't insert with one query.
                if (rowCnt > settings.MaximumRowUpdate)
                {
                    if (settings.Type == DatabaseType.Oracle)
                    {
                        sb.Append(" SELECT 1 FROM DUAL");
                    }
                    queries.Add(sb.ToString());
                    sb.Length = 0;
                    GetInsertColumns(settings, true, table, sb);
                    firstRow = true;
                }
            }
            if (!firstRow)
            {
                if (settings.Type == DatabaseType.Oracle)
                {
                    sb.Append(" SELECT 1 FROM DUAL");
                }
                queries.Add(sb.ToString());
            }
            return table.Value.Rows.Count;
        }

        /// <summary>
        /// Get added or updated values.
        /// </summary>
        /// <param name="value">Value to insert or update.</param>
        /// <param name="columns">Columns to update or insert.</param>
        /// <param name="itemsList">List of items to update.</param>
        internal static void GetValues(object value, object parent, LambdaExpression columns, List<KeyValuePair<Type, GXUpdateItem>> itemsList,
            bool insert, bool mapTable, char columnQuotation, bool updating)
        {
            bool inserted = false;
            object tmp;
            GXSerializedItem si = null;
            if (value != null)
            {
                Type type = value.GetType();
                si = GXSqlBuilder.FindUnique(type);
                object target;
                GXUpdateItem u = null;
                List<KeyValuePair<object, GXSerializedItem>> row = new List<KeyValuePair<object, GXSerializedItem>>();
                if (!mapTable)
                {
                    if (typeof(IEnumerable).IsAssignableFrom(type))
                    {
                        foreach (object v in (IEnumerable)value)
                        {
                            GetValues(v, parent, columns, itemsList, insert, false, columnQuotation, updating);
                        }
                        return;
                    }
                    //For relation map table do not have Id.
                    if (si == null)
                    {
                        mapTable = true;
                    }
                    else
                    {
                        tmp = si.Get(value);
                        //If ID is zero do not update item if it's auto increment value.
                        if ((si.Attributes & Attributes.AutoIncrement) != 0)
                        {
                            if (tmp.Equals(Convert.ChangeType(0, (si.Target as PropertyInfo).PropertyType)))
                            {
                                if (!insert)
                                {
                                    inserted = true;
                                }
                            }
                            //Do not add item if it's already inserted but loop through all relation tables to add them if needed.
                            else
                            {
                                if (insert)
                                {
                                    inserted = true;
                                }
                                else
                                {
                                    inserted = false;
                                }
                            }
                        }
                        else if (insert && updating) //On update we do not want to save values that do not have ID twice.
                        {
                            return;
                        }
                    }
                }
                Dictionary<string, GXSerializedItem> properties = GXSqlBuilder.GetProperties(type);
                //If we are adding new row columns are not need to update.
                bool update = false;
                //Check is table added already.
                foreach (var it in itemsList)
                {
                    if (it.Key == type)
                    {
                        u = it.Value;
                        if (!inserted)
                        {
                            u.Rows.Add(row);
                        }
                        update = true;
                        break;
                    }
                }
                if (u == null)
                {
                    u = new GXUpdateItem();
                    if (!inserted)
                    {
                        itemsList.Add(new KeyValuePair<Type, GXUpdateItem>(type, u));
                        u.Rows.Add(row);
                    }
                }
                if (!insert && !inserted)
                {
                    u.Where = GetColumnName(si.Target as PropertyInfo, columnQuotation) + " = " + si.Get(value).ToString();
                }

                //Get inserted column names.
                if (columns != null && !update)
                {
                    u.Columns.AddRange(GXDbHelpers.GetMembers(null, columns.Body, '\0', false));
                }
                else
                {
                    foreach (var it in properties)
                    {
                        if (it.Value.Relation != null && it.Value.Relation.ForeignTable != type)
                        {
                            if (it.Value.Relation.RelationType != RelationType.OneToOne)
                            {
                                if (it.Value.Get != null)
                                {
                                    target = it.Value.Get(value);
                                }
                                else
                                {
                                    target = GXInternal.GetValue(value, it.Value.Target);
                                }
                                if (GXInternal.IsGenericDataType(it.Value.Type))
                                {
                                    if (!update)
                                    {
                                        u.Columns.Add(it.Key);
                                    }
                                }
                                //Relations are not inserted. They are expected to be in DB already.
                                else if (it.Value.Relation.RelationType != RelationType.Relation)
                                {
                                    GetValues(target, value, null, itemsList, insert, mapTable, columnQuotation, updating);
                                }
                            }
                            else if (!update)
                            {
                                u.Columns.Add(it.Key);
                            }
                            if (it.Value.Relation.RelationMapTable != null)
                            {
                                object relations = GXInternal.GetValue(value, it.Value.Target);
                                if (relations != null)
                                {
                                    GXSerializedItem r2 = GXSqlBuilder.FindUnique(type);
                                    object id2 = r2.Get(value);
                                    //Create relation table(s).
                                    foreach (var r in (IList)relations)
                                    {
                                        //Add map row.
                                        GXUpdateItem m = new GXUpdateItem();
                                        m.Columns.Add(GetColumnName(it.Value.Relation.RelationMapTable.Relation.PrimaryId.Target as PropertyInfo, '\0'));
                                        m.Columns.Add(GetColumnName(GXSqlBuilder.FindRelation(it.Value.Relation.RelationMapTable.Relation.PrimaryTable, type).Target as PropertyInfo, '\0'));
                                        itemsList.Add(new KeyValuePair<Type, GXUpdateItem>((it.Value.Relation.RelationMapTable.Target as PropertyInfo).DeclaringType, m));
                                        List<KeyValuePair<object, GXSerializedItem>> mr = new List<KeyValuePair<object, GXSerializedItem>>();
                                        m.Rows.Add(mr);
                                        mr.Add(new KeyValuePair<object, GXSerializedItem>(r, it.Value.Relation.RelationMapTable.Relation.ForeignId));
                                        mr.Add(new KeyValuePair<object, GXSerializedItem>(value, r2));
                                    }
                                }
                            }
                        }
                        //Do not try to add or update auto increment value.
                        else if (!update && (it.Value.Attributes & Attributes.AutoIncrement) == 0)
                        {
                            u.Columns.Add(it.Key);
                        }
                    }
                }

                //Get values.
                foreach (string it in u.Columns)
                {
                    GXSerializedItem item = properties[it];
                    if (item.Relation != null && item.Relation.ForeignTable != type &&
                        item.Relation.RelationMapTable == null &&
                        //If relation is to the class not Id.
                        !GXInternal.IsGenericDataType(item.Type))
                    {
                        if (parent != null && parent.GetType() == item.Relation.ForeignTable)
                        {
                            item = GXSqlBuilder.FindUnique(parent.GetType());
                            if (item.Get != null)
                            {
                                target = item.Get(parent);
                            }
                            else
                            {
                                target = GXInternal.GetValue(parent, item.Target);
                            }
                            row.Add(new KeyValuePair<object, GXSerializedItem>(parent, item));
                        }
                        else if (item.Relation.RelationType == RelationType.OneToOne)
                        {
                            if (item.Get != null)
                            {
                                target = item.Get(value);
                            }
                            else
                            {
                                target = GXInternal.GetValue(value, item.Target);
                            }
                            if (target != null && !mapTable)
                            {
                                if (typeof(IEnumerable).IsAssignableFrom(item.Type))
                                {
                                    si = GXSqlBuilder.FindUnique(GXInternal.GetPropertyType(item.Type));
                                }
                                else
                                {
                                    si = GXSqlBuilder.FindUnique(item.Type);
                                }
                                tmp = si.Get(target);
                                //Add item if not insert yet.
                                if (tmp.Equals(Convert.ChangeType(0, (si.Target as PropertyInfo).PropertyType)))
                                {
                                    Dictionary<Type, GXUpdateItem> tmpList = new Dictionary<Type, GXUpdateItem>();
                                    tmpList = itemsList.Concat(tmpList).ToDictionary(x => x.Key, x => x.Value);
                                    itemsList.Clear();
                                    GetValues(target, parent, null, itemsList, insert, mapTable, columnQuotation, updating);
                                    foreach (var it2 in tmpList)
                                    {
                                        itemsList.Add(new KeyValuePair<Type, GXUpdateItem>(it2.Key, it2.Value));
                                    }
                                    row.Add(new KeyValuePair<object, GXSerializedItem>(value, item));
                                }
                                else
                                {
                                    row.Add(new KeyValuePair<object, GXSerializedItem>(value, item));
                                }
                            }
                            else
                            {
                                row.Add(new KeyValuePair<object, GXSerializedItem>(value, item));
                            }
                        }
                        else if (item.Relation.RelationType == RelationType.OneToMany)
                        {
                            if (item.Get != null)
                            {
                                target = item.Get(value);
                            }
                            else
                            {
                                target = GXInternal.GetValue(value, item.Target);
                            }
                            si = GXSqlBuilder.FindUnique(GXInternal.GetPropertyType(item.Type));
                            foreach (object v in (IEnumerable)target)
                            {
                                tmp = si.Get(v);
                                GetValues(v, parent, null, itemsList, insert, mapTable, columnQuotation, updating);
                            }
                            row.Add(new KeyValuePair<object, GXSerializedItem>(value, item));
                        }
                        else if (item.Relation.RelationType == RelationType.Relation)
                        {
                            row.Add(new KeyValuePair<object, GXSerializedItem>(value, item));
                        }
                    }
                    else if (!inserted)
                    {
                        row.Add(new KeyValuePair<object, GXSerializedItem>(value, item));
                    }
                }
            }
        }

        internal static string[] GetMembers(GXDBSettings settings, Expression expression, char quoteSeparator, bool where)
        {
            return GetMembers(settings, expression, quoteSeparator, where, false);
        }

        internal static string[] HandleMethod(GXDBSettings settings, UnaryExpression unaryExpression, MethodCallExpression m, char quoteSeparator, bool where, bool getValue)
        {
            if (m.Method.DeclaringType == typeof(GXSql))
            {
                if (m.Method.Name == "Count")
                {
                    string value = null;
                    if (m.Arguments[0].NodeType != ExpressionType.Parameter)
                    {
                        value = GetMembers(settings, m.Arguments[0], quoteSeparator, where)[0];
                    }
                    if (value == null || value == AddQuotes("*", quoteSeparator))
                    {
                        return new string[] { "COUNT(1)" };
                    }
                    return new string[] { "COUNT(" + value + ")" };
                }
                if (m.Method.Name == "In")
                {
                    string[] list = GetMembers(settings, m.Arguments[1], quoteSeparator, where, true);
                    string tmp = "(" + GetMembers(settings, m.Arguments[0], quoteSeparator, where, false)[0];
                    if (unaryExpression != null && unaryExpression.NodeType == ExpressionType.Not)
                    {
                        tmp += " NOT IN (";
                    }
                    else
                    {
                        tmp += " IN (";
                    }
                    tmp += string.Join(", ", list) + "))";
                    return new string[] { tmp };
                }
                if (m.Method.Name == "Exists")
                {
                    if (m.Arguments.Count == 3)
                    {
                        string tmp;
                        if (unaryExpression != null && unaryExpression.NodeType == ExpressionType.Not)
                        {
                            tmp = "(NOT EXISTS (";
                        }
                        else
                        {
                            tmp = "(EXISTS (";
                        }
                        tmp += GetMembers(settings, m.Arguments[2], quoteSeparator, where, false)[0] + " AND " +
                                  GetMembers(settings, m.Arguments[1], quoteSeparator, where, false)[0] + " = " +
                                  GetMembers(settings, m.Arguments[0], quoteSeparator, where, false)[0] + "))";
                        return new string[] { tmp };
                    }
                    else
                    {
                        throw new ArgumentOutOfRangeException("Exist failed.");
                    }
                }
            }
            if (m.Method.DeclaringType == typeof(System.Linq.Enumerable) && m.Method.Name == "Contains")
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("(" + GetMembers(settings, m.Arguments[1], quoteSeparator, where)[0]);
                if (unaryExpression.NodeType == ExpressionType.Not)
                {
                    sb.Append(" NOT IN (");
                }
                else
                {
                    sb.Append(" IN (");
                }
                foreach (string it in GetMembers(settings, m.Arguments[0], quoteSeparator, where))
                {
                    sb.Append(it);
                    sb.Append(", ");
                }
                sb.Length -= 2;
                sb.Append("))");
                return new string[] { sb.ToString() };
            }
            if (m.Method.DeclaringType == typeof(string) && m.Method.Name == "StartsWith")
            {
                return new string[] {"(" + GetMembers(settings, m.Object, quoteSeparator, where)[0] + " LIKE('" +
                            GetMembers(settings, m.Arguments[0], '\0', where)[0] + "%'))"};
            }
            if (m.Method.DeclaringType == typeof(string) && m.Method.Name == "EndsWith")
            {
                return new string[] {"(" + GetMembers(settings, m.Object, quoteSeparator, where)[0] + " LIKE('%" +
                            GetMembers(settings, m.Arguments[0], '\0', where)[0] + "'))"};
            }
            if (m.Method.DeclaringType == typeof(string) && m.Method.Name == "Contains")
            {
                return new string[] {"(" + GetMembers(settings, m.Object, quoteSeparator, where)[0] + " LIKE('%" +
                            GetMembers(settings, m.Arguments[0], '\0', where)[0] + "%'))"};
            }
            if (m.Method.Name == "Equals")
            {
                if (m.Method.DeclaringType == typeof(string))
                {
#if !NETCOREAPP2_0 && !NETCOREAPP2_1
                    if (settings.Type == DatabaseType.Access)
                    {
                        return new string[] {"(" + GetMembers(settings, m.Object, quoteSeparator, where)[0] + " LIKE('" +
                                GetMembers(settings, m.Arguments[0], '\0', where, true)[0] + "'))"};
                    }
#endif //!NETCOREAPP2_0 && !NETCOREAPP2_1
                    string tmp = GetMembers(settings, m.Arguments[0], '\0', where, true)[0];
                    if (tmp == null)
                    {
                        tmp = GetMembers(settings, m.Object, quoteSeparator, where)[0];
                        return new string[] { "(" + tmp + " IS NULL)" };
                    }
                    tmp = tmp.ToUpper();
                    if (tmp[0] != '\'')
                    {
                        tmp = "'" + tmp + "'";
                    }
                    return new string[] {"(UPPER(" + GetMembers(settings, m.Object, quoteSeparator, where)[0] + ") LIKE(" +
                                 tmp + "))"};
                }
                else
                {
                    return new string[] {"(" + GetMembers(settings, m.Object, quoteSeparator, where)[0] + "=" +
                            GetMembers(settings, m.Arguments[0], '\0', where, true)[0].ToUpper() + ")"};
                }
            }
            if (m.Method.DeclaringType == typeof(string) && m.Method.Name == "IsNullOrEmpty")
            {
                string tmp = GetMembers(settings, m.Arguments[0], quoteSeparator, where)[0];
                if (settings.Type == DatabaseType.Oracle)
                {
                    if (unaryExpression.NodeType == ExpressionType.Not)
                    {
                        return new string[] { "((" + tmp + " IS NOT NULL))" };
                    }
                    return new string[] { "((" + tmp + " IS NULL))" };
                }
                else
                {
                    if (unaryExpression.NodeType == ExpressionType.Not)
                    {
                        return new string[] { "((" + tmp + " IS NOT NULL AND " + tmp + " <> ''))" };
                    }
                    return new string[] { "((" + tmp + " IS NULL OR " + tmp + " = ''))" };
                }
            }
            throw new ArgumentOutOfRangeException("Unknown SQL command: " + m.Method.Name + ".");
        }

        internal static string[] GetMembers(GXDBSettings settings, Expression expression, char quoteSeparator, bool where, bool getValue)
        {
            if (expression == null)
            {
                throw new ArgumentException("The expression cannot be null.");
            }

            if (expression is LambdaExpression)
            {
                LambdaExpression lambdaEx = expression as LambdaExpression;
                return GetMembers(settings, lambdaEx.Body, quoteSeparator, where);
            }

            if (expression is MemberExpression)
            {
                // Reference type property or field
                var memberExpression = (MemberExpression)expression;
                Expression e = memberExpression.Expression;
                if (e == null)
                {
                    var member = Expression.Convert(memberExpression, typeof(object));
                    var lambda = Expression.Lambda<Func<object>>(member);
                    var getter = lambda.Compile();
                    object value = getter();
                    return new string[] { ConvertToString(value, settings, where) };
                }
                //Get member name.
                if (e.NodeType == ExpressionType.Parameter)
                {
                    //In where get table type and column name.
                    if (where && memberExpression.Member.DeclaringType.IsClass)
                    {
                        if (GXDbHelpers.IsAliasName(memberExpression.Expression.Type))
                        {
                            return new string[] { GXDbHelpers.OriginalTableName(memberExpression.Expression.Type) +
                            "." + GetColumnName(memberExpression.Member, settings.ColumnQuotation) };
                        }
                        return new string[] { GXDbHelpers.GetTableName(memberExpression.Expression.Type, true, settings) +
                            "." + GetColumnName(memberExpression.Member, settings.ColumnQuotation) };
                    }
                    else
                    {
                        return new string[] { GetColumnName(memberExpression.Member, quoteSeparator) };
                    }
                }
                //Get property value.
                if (e.NodeType == ExpressionType.MemberAccess)
                {
                    if (!where)
                    {
                        return new string[] { GetColumnName(memberExpression.Member, quoteSeparator) };
                    }
                    var member = Expression.Convert(memberExpression, typeof(object));
                    var lambda = Expression.Lambda<Func<object>>(member);
                    var getter = lambda.Compile();
                    object value = getter();
                    if (value != null && value.GetType().IsEnum)
                    {
                        //Convert enum value to integer value.
                        if (!settings.UseEnumStringValue)
                        {
                            value = Convert.ToInt64(value);
                        }
                    }
                    else if (value is string)
                    {
                        //Do nothing.
                    }
                    else if (value is IEnumerable)
                    {
                        StringBuilder sb = new StringBuilder();
                        bool first = true;
                        bool stringValue = true;
                        foreach (object it in value as IEnumerable)
                        {
                            if (first)
                            {
                                stringValue = it is string;
                                first = false;
                            }
                            else
                            {
                                sb.Append(", ");
                            }
                            sb.Append(Convert.ToString(it));
                        }
                        if (!stringValue)
                        {
                            return new string[] { sb.ToString() };
                        }
                    }
                    return new string[] { ConvertToString(value, settings, where) };
                }
                if (e.NodeType == ExpressionType.Call)
                {
                    return new string[] { ConvertToString(Expression.Lambda(memberExpression).Compile().DynamicInvoke(), settings, where) };
                }
                if (memberExpression.NodeType == ExpressionType.MemberAccess && e.NodeType == ExpressionType.Constant)
                {
                    bool first = true;
                    StringBuilder sb = new StringBuilder();
                    object value;
                    var target = Expression.Lambda(expression).Compile().DynamicInvoke();
                    if (target == null)
                    {
                        return new string[] { null };
                    }
                    Dictionary<string, GXSerializedItem> properties;
                    if (target is IList)
                    {
                        properties = GXSqlBuilder.GetProperties(GXInternal.GetPropertyType(target.GetType()));
                        //If this is a basic type list. example int[].
                        if (properties.Count == 0)
                        {
                            IEnumerator e2 = (target as IEnumerable).GetEnumerator();
                            first = true;
                            while (e2.MoveNext())
                            {
                                if (first)
                                {
                                    first = false;
                                }
                                else
                                {
                                    sb.Append(", ");
                                }
                                sb.Append(e2.Current);
                            }
                            return new string[] { sb.ToString() };
                        }
                    }
                    else if (target is string || target is GXSelectArgs || !target.GetType().IsClass)//String is class.
                    {
                        if (target is GXSelectArgs)
                        {
                            ((GXSelectArgs)target).Settings = settings;
                        }
                        string str = Convert.ToString(target);
                        if (where && getValue && target is string)
                        {
                            return new string[] { "'" + str + "'" };
                        }
                        if (where && getValue && target is Guid)
                        {
                            return new string[] { "'" + str.Replace("-", "") + "'" };
                        }
                        return new string[] { str };
                    }
                    else
                    {
                        properties = GXSqlBuilder.GetProperties(target.GetType());
                    }
                    //If primary key is used.
                    foreach (var it in properties)
                    {
                        if ((it.Value.Attributes & Attributes.AutoIncrement) != 0)
                        {
                            //If collection
                            if (target is IEnumerable)
                            {
                                sb.Append('(');
                                sb.Append(GetColumnName(it.Value.Target as PropertyInfo, settings.ColumnQuotation));
                                sb.Append(" IN(");
                                IEnumerator e2 = (target as System.Collections.IEnumerable).GetEnumerator();
                                first = true;
                                while (e2.MoveNext())
                                {
                                    if (it.Value.Get != null)
                                    {
                                        value = it.Value.Get(e2.Current);
                                    }
                                    else
                                    {
                                        value = GXInternal.GetValue(e2.Current, it.Value);
                                    }
                                    if (first)
                                    {
                                        first = false;
                                    }
                                    else
                                    {
                                        sb.Append(", ");
                                    }
                                    sb.Append(value.ToString());
                                }
                                sb.Append("))");
                                return new string[] { sb.ToString() };
                            }
                            if (it.Value.Get != null)
                            {
                                value = it.Value.Get(target);
                            }
                            else
                            {
                                value = GXInternal.GetValue(target, it.Value);
                            }
                            if (value == null && where)
                            {
                                return new string[] { "(" + GetColumnName(it.Value.Target as PropertyInfo, settings.ColumnQuotation) + " IS NULL)" };
                            }
                            else
                            {
                                return new string[] { "(" + GetColumnName(it.Value.Target as PropertyInfo, settings.ColumnQuotation) + " = " + value.ToString() + ")" };
                            }
                        }
                    }
                    //If primary key is not used.
                    //If collection
                    if (target is IEnumerable)
                    {
                        Type itemType = GXInternal.GetPropertyType(target.GetType());
                        sb.Append('(');
                        bool firstRow = true;
                        IEnumerator e2 = (target as IEnumerable).GetEnumerator();
                        while (e2.MoveNext())
                        {
                            if (firstRow)
                            {
                                firstRow = false;
                            }
                            else
                            {
                                sb.Append(" OR ");
                            }
                            sb.Append('(');
                            foreach (var it in GXSqlBuilder.GetProperties(itemType))
                            {
                                if (it.Value.Get != null)
                                {
                                    value = it.Value.Get(e2.Current);
                                }
                                else
                                {
                                    value = GXInternal.GetValue(e2.Current, it.Value);
                                }
                                if (first)
                                {
                                    first = false;
                                }
                                else
                                {
                                    sb.Append(" AND ");
                                }
                                if (value == null && where)
                                {
                                    sb.Append(it.Key);
                                    sb.Append(" IS NULL ");
                                }
                                else
                                {
                                    sb.Append(it.Key);
                                    sb.Append(" = ");
                                    sb.Append(ConvertToString(value, settings, where));
                                }
                            }
                            sb.Append(")");
                            first = true;
                        }
                        sb.Append(")");
                        return new string[] { sb.ToString() };
                    }
                    sb.Append('(');
                    foreach (var it in GXSqlBuilder.GetProperties(target.GetType()))
                    {
                        if (it.Value.Get != null)
                        {
                            value = it.Value.Get(target);
                        }
                        else
                        {
                            value = GXInternal.GetValue(target, it.Value);
                        }
                        if (first)
                        {
                            first = false;
                        }
                        else
                        {
                            sb.Append(" AND ");
                        }
                        if (value == null && where)
                        {
                            sb.Append(it.Key);
                            sb.Append(" IS NULL ");
                        }
                        else
                        {
                            if (settings.UseQuotationWhereColumns)
                            {
                                sb.Append(GXDbHelpers.AddQuotes(it.Key, settings.ColumnQuotation));
                            }
                            else
                            {
                                sb.Append(it.Key);
                            }
                            sb.Append(" = ");
                            sb.Append(ConvertToString(value, settings, where));
                        }
                    }
                    sb.Append(')');
                    return new string[] { sb.ToString() };
                }
                throw new Exception("Invalid expression.");
            }

            if (expression is MethodCallExpression)
            {
                // Reference type method
                var methodCallExpression = (MethodCallExpression)expression;
                if (methodCallExpression.Arguments.Count != 0 && methodCallExpression.Arguments[0].NodeType == ExpressionType.MemberAccess)
                {
                    return HandleMethod(settings, null, methodCallExpression, quoteSeparator, where, getValue);
                }
                object value = Expression.Lambda(methodCallExpression).Compile().DynamicInvoke();
                if (where && getValue && value is string)
                {
                    return new string[] { AddQuotes((string)value, '\'') };
                }
                else if (where && getValue && value is Guid)
                {
                    return new string[] { AddQuotes(value.ToString().Replace("-", ""), '\'') };
                }
                else if (where && getValue && value is DateTime)
                {
                    return new string[] { settings.ConvertToString(value, where) };
                }
                return new string[] { Convert.ToString(value) };
            }

            if (expression is UnaryExpression)
            {
                // Property, field of method returning value type
                var unaryExpression = (UnaryExpression)expression;
                MethodCallExpression m = unaryExpression.Operand as MethodCallExpression;
                if (m != null)
                {
                    return HandleMethod(settings, unaryExpression, m, quoteSeparator, where, getValue);
                }
                return GetMembers(settings, unaryExpression.Operand, quoteSeparator, where);
            }

            if (expression is NewExpression)
            {
                // Property, field of method returning value type
                var newExpression = (NewExpression)expression;
                List<string> list = new List<string>();
                foreach (var it in newExpression.Arguments)
                {
                    list.AddRange(GetMembers(settings, it, quoteSeparator, where));
                }
                return list.ToArray();
            }

            if (expression is NewArrayExpression)
            {
                // Property, field of method returning value type
                var newExpression = (NewArrayExpression)expression;
                List<string> list = new List<string>();
                foreach (var it in newExpression.Expressions)
                {
                    foreach (string var in GetMembers(settings, it, quoteSeparator, where))
                    {
                        list.Add(var.ToString());
                    }
                }
                return list.ToArray();
            }

            if (expression is BinaryExpression)
            {
                // Property, field of method returning value type
                var newExpression = (BinaryExpression)expression;
                List<string> list = new List<string>();
                string op = "";
                switch (expression.NodeType)
                {
                    case ExpressionType.Add:
                    case ExpressionType.AddChecked:
                        op = " + ";
                        break;
                    case ExpressionType.And:
                        op = " AND ";
                        break;
                    case ExpressionType.AndAlso:
                        list.Add("(" + GetMembers(settings, newExpression.Left, quoteSeparator, where)[0] + " AND " +
                            GetMembers(settings, newExpression.Right, quoteSeparator, where)[0] + ")");
                        return list.ToArray();
                    case ExpressionType.Coalesce:
                        op = " COALESCE ";
                        break;
                    case ExpressionType.Divide:
                        op = " / ";
                        break;
                    case ExpressionType.Equal:
                        op = " = ";
                        break;
                    case ExpressionType.GreaterThan:
                        op = " > ";
                        break;
                    case ExpressionType.GreaterThanOrEqual:
                        op = " >= ";
                        break;
                    case ExpressionType.LessThan:
                        op = " < ";
                        break;
                    case ExpressionType.LessThanOrEqual:
                        op = " <= ";
                        break;
                    case ExpressionType.Modulo:
                        op = " MOD ";
                        break;
                    case ExpressionType.Multiply:
                    case ExpressionType.MultiplyChecked:
                        op = " * ";
                        break;
                    case ExpressionType.Negate:
                    case ExpressionType.NegateChecked:
                        op = " - ";
                        break;
                    case ExpressionType.Not:
                        op = " !";
                        break;
                    case ExpressionType.NotEqual:
                        op = " <> ";
                        break;
                    case ExpressionType.Or:
                    case ExpressionType.OrElse:
                        op = " OR ";
                        break;
                    case ExpressionType.Power:
                        list.Add("POWER(" + GetMembers(settings, newExpression.Left, quoteSeparator, where)[0] + ", " +
                            GetMembers(settings, newExpression.Right, quoteSeparator, where)[0] + ")");
                        return list.ToArray();
                    case ExpressionType.LeftShift:
                        op = " << ";
                        break;
                    case ExpressionType.RightShift:
                        op = " >> ";
                        break;
                    case ExpressionType.Subtract:
                    case ExpressionType.SubtractChecked:
                        op = " - ";
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("Unknown SQL command.");
                }
                UnaryExpression u = newExpression.Left as UnaryExpression;
                bool isenum = u != null && u.Operand.Type.IsEnum;
                string tmp;
                if (isenum)
                {
                    tmp = GetMembers(settings, newExpression.Right, quoteSeparator, where)[0];
                    if (settings.UseEnumStringValue)
                    {
                        tmp = Enum.Parse(u.Operand.Type, tmp, true).ToString();
                        tmp = AddQuotes(tmp, '\'');
                    }
                }
                else
                {
                    tmp = GetMembers(settings, newExpression.Right, quoteSeparator, where, true)[0];
                }
                //Where string is empty is not working with oracle DB. We must use where string is null expression.
                if (where && (tmp == null || ((settings.Type == DatabaseType.Oracle) && tmp == "''")))
                {
                    tmp = null;
                    if (expression.NodeType == ExpressionType.NotEqual)
                    {
                        op = " IS NOT NULL";
                    }
                    else if (expression.NodeType == ExpressionType.Equal)
                    {
                        op = " IS NULL";
                    }
                    else
                    {
                        throw new ArgumentException("Argument is null.");
                    }
                }
                else if (newExpression.Right.NodeType == ExpressionType.Constant && newExpression.Right.Type == typeof(string))
                {
                    tmp = "'" + tmp + "'";
                }
                list.Add("(" + GetMembers(settings, newExpression.Left, quoteSeparator, where)[0] + op + tmp + ")");
                return list.ToArray();
            }

            if (expression is ConstantExpression)
            {
                var newExpression = (ConstantExpression)expression;
                if (newExpression.Value is string)
                {
                    if ("*".Equals(newExpression.Value))
                    {
                        return new string[] { (string)newExpression.Value };
                    }
                    return new string[] { AddQuotes((string)newExpression.Value, quoteSeparator) };
                }
                else if (newExpression.Value == null)
                {
                    return new string[] { null };
                }
                else if (newExpression.Value is bool)
                {
                    return new string[] { ((bool)newExpression.Value) ? "1" : "0" };
                    //return new string[] { AddQuotes(newExpression.Value.ToString(), quoteSeparator) };
                }
                else
                {
                    return new string[] { newExpression.Value.ToString() };
                }
            }

            throw new ArgumentException("Invalid expression");
        }
    }
}
