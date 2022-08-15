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
using System.Globalization;
using Gurux.Common.Db;

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
                    Type type = Enum.GetUnderlyingType(value.GetType());
                    str = Convert.ToString(Convert.ChangeType(value, type), CultureInfo.InvariantCulture);
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
                str = GetQuetedValue(((Guid)value).ToString());
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
            else if (value is DateTimeOffset?)
            {
                DateTimeOffset? dt = (DateTimeOffset?)value;
                if (settings.UniversalTime && dt != DateTime.MinValue && dt != DateTime.MaxValue)
                {
                    str = settings.ConvertToString(dt.Value.ToUniversalTime(), where);
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
                    sb.Append(Convert.ToString(it, CultureInfo.InvariantCulture));
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
                str = Convert.ToString(value, CultureInfo.InvariantCulture);
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
        internal static void GetQueries(
            bool insert,
            GXDBSettings settings,
            List<KeyValuePair<object, LambdaExpression>> values,
            List<KeyValuePair<Type, LambdaExpression>> excluded,
            List<string> queries,
            GXWhereCollection where,
            List<object> insertedObjects)
        {
            List<KeyValuePair<Type, GXUpdateItem>> list = new List<KeyValuePair<Type, GXUpdateItem>>();
            List<object> handledObjects = new List<object>();
            foreach (KeyValuePair<object, LambdaExpression> it in values)
            {
                GetValues(settings, it.Key, null, it.Value, list, excluded, insert, false,
                    settings.ColumnQuotation, false, where, handledObjects, insertedObjects);
            }
            foreach (KeyValuePair<Type, GXUpdateItem> table in list)
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

        internal static void GetUpdateQuery(
            KeyValuePair<Type, GXUpdateItem> table,
            GXDBSettings settings,
            List<string> queries)
        {
            if (!table.Value.Inserted)
            {
                StringBuilder sb = new StringBuilder();
                int index = 0;
                object value;
                sb.Length = 0;
                string tableName = GXDbHelpers.GetTableName(table.Key, true, settings);
                bool first;
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
                    if (table.Value.Where.Count != 0)
                    {
                        sb.Append(" ");
                        sb.Append(table.Value.Where[0]);
                        table.Value.Where.RemoveAt(0);
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

        private static void GetInsertColumns(
            GXDBSettings settings,
            bool first,
            KeyValuePair<Type, GXUpdateItem> table,
            StringBuilder sb,
            bool select)
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
            if (select)
            {
                sb.Append(") SELECT ");
            }
            else
            {
                sb.Append(") VALUES(");
            }
        }

        static bool IsZeroOrEmpty(object o1)
        {
            bool ret = true;
            object ZeroValue = 0;

            if (o1 != null)
            {
                if (o1.GetType() == typeof(Guid))
                {
                    ret = o1.Equals(Guid.Empty);
                }
                else if (o1.GetType().IsValueType)
                {
                    ret = (o1 as System.ValueType).Equals(Convert.ChangeType(ZeroValue, o1.GetType()));
                }
                else if (o1.GetType() == typeof(string))
                {
                    ret = o1.Equals(string.Empty);
                }
                else
                {
                    ret = false;
                }
            }
            return ret;
        }

        internal static int GetInsertQuery(
            KeyValuePair<Type, GXUpdateItem> table,
            GXDBSettings settings,
            List<string> queries)
        {
            StringBuilder sb = new StringBuilder();
            object value;
            sb.Length = 0;
            bool firstRow = true, first, select = false;
            foreach (var col in table.Value.Rows)
            {
                foreach (var it in col)
                {
                    if (it.Key is GXSelectArgs)
                    {
                        select = true;
                        break;
                    }
                }
                break;
            }
            GetInsertColumns(settings, true, table, sb, select);
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
                        GetInsertColumns(settings, false, table, sb, select);
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
                        if (it.Key is GXSelectArgs)
                        {
                            //Remove VALUES( or SELECT
                            sb.Length -= 7;
                            sb.Append(it.Key.ToString());
                            break;
                        }
                        first = false;
                    }
                    else
                    {
                        sb.Append(", ");
                    }
                    GXSerializedItem row = it.Value as GXSerializedItem;
                    if (row != null)
                    {
                        if (it.Key is GXSelectArgs s)
                        {
                            s.Columns.Insert = true;
                            try
                            {
                                string sql = it.Key.ToString();
                                if (select)
                                {
                                    //Remove duplicate select
                                    sql = sql.Substring(7);
                                }
                                sb.Append(sql);
                            }
                            finally
                            {
                                s.Columns.Insert = false;
                            }
                            continue;
                        }
                        else
                        {
                            if (row.Get != null)
                            {
                                value = row.Get(it.Key);
                                if ((row.Attributes & Attributes.ForeignKey) != 0 &&
                                    IsZeroOrEmpty(value))
                                {
                                    if ((row.Attributes & Attributes.AllowNull) == 0)
                                    {
                                        throw new ArgumentException("Foreign key can't be null.");
                                    }
                                }
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
                    }
                    else
                    {
                        value = it;
                    }
                    sb.Append(GXDbHelpers.ConvertToString(value, settings, false));
                }
                if (!select)
                {
                    sb.Append(")");
                }
                //If all rows can't insert with one query.
                if (rowCnt > settings.MaximumRowUpdate)
                {
                    if (settings.Type == DatabaseType.Oracle)
                    {
                        sb.Append(" SELECT 1 FROM DUAL");
                    }
                    queries.Add(sb.ToString());
                    sb.Length = 0;
                    GetInsertColumns(settings, true, table, sb, select);
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
        /// <param name="excluded">Excluded columns.</param>
        internal static void GetValues(
            GXDBSettings settings,
            object value,
            object parent,
            LambdaExpression columns,
            List<KeyValuePair<Type, GXUpdateItem>> itemsList,
            List<KeyValuePair<Type, LambdaExpression>> excluded,
            bool insert,
            bool mapTable,
            char columnQuotation,
            bool updating,
            GXWhereCollection where,
            List<object> handledObjects,
            List<object> insertedObjects)
        {
            //Check if value is already added.
            if (handledObjects != null)
            {
                if (handledObjects.Contains(value))
                {
                    return;
                }
            }
            bool inserted = false;
            object tmp;
            GXSerializedItem si = null;
            if (value != null)
            {
                Type type;
                if (value is GXSelectArgs arg)
                {
                    arg.Settings = settings;
                    if (columns.Body is ConstantExpression c)
                    {
                        //Data is copy from one table to other.
                        type = ((Type)c.Value).UnderlyingSystemType;
                        columns = null;
                    }
                    else if (columns.Body is MemberExpression m)
                    {
                        type = m.Expression.Type;
                    }
                    else if (columns.Body is NewExpression newExpression)
                    {
                        type = ((MemberExpression)newExpression.Arguments[0]).Member.DeclaringType;
                    }
                    else
                    {
                        throw new ArgumentOutOfRangeException("Invalid GXSelectArgs parameter.");
                    }
                }
                else
                {
                    type = value.GetType();
                }
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
                            GetValues(settings, v, parent, columns, itemsList, excluded, insert, false,
                                columnQuotation, updating, where, handledObjects, insertedObjects);
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
                        if (value is GXSelectArgs sel)
                        {
                            //Auto increment keys are added to excluded list.
                            PropertyInfo pi = (si.Target as PropertyInfo);
                            string name;
                            DataMemberAttribute[] attr = (DataMemberAttribute[])pi.GetCustomAttributes(typeof(DataMemberAttribute), true);
                            if (attr.Length == 0 || attr[0].Name == null)
                            {
                                name = pi.Name;
                            }
                            else
                            {
                                name = attr[0].Name;
                            }
                            Expression<Func<object, object>> expression = q => name;
                            sel.Columns.Excluded.Add(new KeyValuePair<Type, LambdaExpression>(type, expression));
                        }
                        else
                        {
                            //If ID is zero do not update item if it's auto increment value.
                            if ((si.Attributes & (Attributes.Id)) != 0)
                            {
                                tmp = si.Get(value);
                                //If Id is not autoincrement.
                                if (IsZeroOrEmpty(tmp))
                                {
                                    if (!insert && where == null)
                                    {
                                        inserted = true;
                                    }
                                    else if (si.Type == typeof(Guid))
                                    {
                                        //Generate new Guid if it's used as ID.
                                        si.Set(value, Guid.NewGuid());
                                        if (insertedObjects != null)
                                        {
                                            insertedObjects.Add(value);
                                        }
                                    }
                                    else if (si.Type == typeof(string))
                                    {
                                        //Generate new Guid if it's used as ID.
                                        si.Set(value, Guid.NewGuid().ToString());
                                        if (insertedObjects != null)
                                        {
                                            insertedObjects.Add(value);
                                        }
                                    }
                                }
                                //Do not add item if it's already inserted but loop through all relation tables to add them if needed.
                                else
                                {
                                    if (insertedObjects != null && insertedObjects.Contains(value))
                                    {
                                        inserted = false;
                                    }
                                    else
                                    {
                                        if (insert && (updating || si.Type != typeof(string)))
                                        {
                                            inserted = true;
                                        }
                                        else
                                        {
                                            inserted = false;
                                        }
                                    }
                                }
                            }
                            else if (insert && updating) //On update we do not want to save values that do not have ID twice.
                            {
                                return;
                            }
                        }
                    }
                }
                string[] updatedProperty = null;
                Dictionary<string, GXSerializedItem> properties = GXSqlBuilder.GetProperties(type);
                //If we are adding a new row columns are not need to update.
                bool update = false;
                //Check is table added already.
                foreach (var it in itemsList)
                {
                    if (it.Key == type)
                    {
                        u = it.Value;
                        if (!inserted)
                        {
                            if (columns != null)
                            {
                                row = it.Value.Rows[0];
                                string post = null;
                                updatedProperty = GetMembers(null, columns.Body, '\0', false, ref post);
                                //Check if column is updated earlier and remove old update.
                                if (!u.Columns.Any(x => updatedProperty.Any(y => y == x)))
                                {
                                    u.Columns.AddRange(updatedProperty);
                                }
                                else
                                {
                                    List<string> updated = new List<string>();
                                    foreach (string p in updatedProperty)
                                    {
                                        int pos = u.Columns.IndexOf(p);
                                        if (pos != -1)
                                        {
                                            KeyValuePair<object, GXSerializedItem> old = row[pos];
                                            row.RemoveAt(pos);
                                            row.Insert(pos, new KeyValuePair<object, GXSerializedItem>(value, old.Value));
                                        }
                                        else
                                        {
                                            u.Columns.Add(p);
                                            updated.Add(p);
                                        }
                                    }
                                    updatedProperty = updated.ToArray();
                                }
                            }
                            else
                            {
                                u.Rows.Add(row);
                            }
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
                    else
                    {
                        if (!insert && handledObjects != null)
                        {
                            handledObjects.Add(value);
                        }
                    }
                }
                if (!insert && !inserted)
                {
                    if (where != null && where.List.Count > 1)
                    {
                        GXWhereCollection tmp2 = new GXWhereCollection(where.Parent);
                        tmp2.List.AddRange(where.List);
                        //Remove get by ID if where is added.
                        tmp2.List.RemoveAt(0);
                        u.Where.Add(tmp2.ToString());
                        where.sql = tmp2.sql;
                        where.Updated = tmp2.Updated;
                    }
                    else
                    {
                        string str = ConvertToString(si.Get(value), settings, false);
                        u.Where.Add("WHERE " + GetColumnName(si.Target as PropertyInfo, columnQuotation) + " = " + str);
                    }
                }
                //Get inserted column names.
                if (columns != null && !update)
                {
                    string post = null;
                    List<string> colums = new List<string>();
                    colums.AddRange(GXDbHelpers.GetMembers(null, columns.Body, '\0', false, ref post));
                    foreach (string item in colums)
                    {
                        KeyValuePair<string, GXSerializedItem> it = new KeyValuePair<string, GXSerializedItem>(item, properties[item]);
                        GetColumn(settings, value, itemsList, excluded, insert, mapTable, columnQuotation, updating, where, type, u, update, it);
                    }
                }
                else
                {
                    Dictionary<Type, List<string>> cols = null;
                    if (value is GXSelectArgs s)
                    {
                        //ToString is called to update the ColumnList.
                        //Do not remove!
                        try
                        {
                            s.Columns.Insert = true;
                            s.Columns.ToString();
                        }
                        finally
                        {
                            s.Columns.Insert = false;
                        }
                        cols = new Dictionary<Type, List<string>>(s.Columns.ColumnList);
                        //Unknown columns are not added to select.
                        foreach (var c in cols)
                        {
                            if (c.Key == type)
                            {
                                foreach (string r in c.Value)
                                {
                                    if (!properties.ContainsKey(r))
                                    {
                                        Expression<Func<object, object>> expression = (_ => r);
                                        s.Columns.Excluded.Add(new KeyValuePair<Type, LambdaExpression>(c.Key, expression));
                                    }
                                }
                            }
                        }
                    }
                    //if column is excluded.
                    string[] removed = null;
                    if (excluded != null)
                    {
                        foreach (KeyValuePair<Type, LambdaExpression> e in excluded)
                        {
                            if (e.Key == type)
                            {
                                string post = null;
                                removed = GetMembers(null, e.Value, '\0', false, ref post);
                            }
                        }
                    }
                    foreach (var it in properties)
                    {
                        bool skip = false;
                        if (removed != null)
                        {
                            foreach (string col in removed)
                            {
                                if (col == it.Key)
                                {
                                    skip = true;
                                    break;
                                }
                            }
                            if (skip)
                            {
                                continue;
                            }
                        }
                        if (value is GXSelectArgs args)
                        {
                            //Unknown columns are not added.
                            bool found = false;
                            foreach (var c in cols)
                            {
                                if (c.Key == type)
                                {
                                    foreach (var r in c.Value)
                                    {
                                        if (it.Key == r)
                                        {
                                            //Don't add auto increment value.
                                            if ((it.Value.Attributes & Attributes.AutoIncrement) != 0)
                                            {
                                                Expression<Func<object, object>> expression = q => r;
                                                args.Columns.Excluded.Add(new KeyValuePair<Type, LambdaExpression>(c.Key, expression));
                                            }
                                            found = true;
                                            break;
                                        }
                                    }
                                    if (found)
                                    {
                                        break;
                                    }
                                }
                            }
                            if (!found)
                            {
                                continue;
                            }
                        }
                        GetColumn(settings, value, itemsList, excluded, insert, mapTable, columnQuotation, updating, where, type, u, update, it);
                    }
                }
                //Remove excluded columns.
                if (excluded != null)
                {
                    foreach (KeyValuePair<Type, LambdaExpression> it in excluded)
                    {
                        if (it.Key == type)
                        {
                            string post = null;
                            string[] removed = GetMembers(null, it.Value, '\0', false, ref post);
                            foreach (string col in removed)
                            {
                                u.Columns.Remove(col);
                            }
                        }
                    }
                }

                //Get values.
                foreach (string it in u.Columns)
                {
                    if (updatedProperty != null &&
                        updatedProperty.Length != 0 &&
                        !updatedProperty.Contains(it))
                    {
                        continue;
                    }
                    GXSerializedItem item = properties[it];
                    if (item.Relation != null && item.Relation.ForeignTable != type &&
                        item.Relation.RelationMapTable == null &&
                        //If relation is to the class not Id.
                        !GXInternal.IsGenericDataType(item.Type) &&
                        !(value is GXSelectArgs))
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
                                if (IsZeroOrEmpty(tmp))
                                {
                                    Dictionary<Type, GXUpdateItem> tmpList = new Dictionary<Type, GXUpdateItem>();
                                    tmpList = itemsList.Concat(tmpList).ToDictionary(x => x.Key, x => x.Value);
                                    itemsList.Clear();
                                    GetValues(settings, target, parent, null, itemsList, excluded,
                                        insert, mapTable, columnQuotation, updating, where, handledObjects, insertedObjects);
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
                                GetValues(settings, v, parent, null, itemsList, excluded, insert, mapTable,
                                    columnQuotation, updating, where, handledObjects, null);
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
        /// <summary>
        /// Get selected column.
        /// </summary>
        private static void GetColumn(GXDBSettings settings, object value, List<KeyValuePair<Type, GXUpdateItem>> itemsList, List<KeyValuePair<Type, LambdaExpression>> excluded, bool insert, bool mapTable, char columnQuotation, bool updating, GXWhereCollection where, Type type, GXUpdateItem u, bool update, KeyValuePair<string, GXSerializedItem> it)
        {
            if (it.Value.Relation != null && it.Value.Relation.ForeignTable != type)
            {
                if (it.Value.Relation.RelationType != RelationType.OneToOne)
                {
                    object target;
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
                        GetValues(settings, target, value, null, itemsList,
                            excluded, insert, mapTable, columnQuotation, updating, where, null, null);
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
            else if (!update && (it.Value.Attributes & Attributes.AutoIncrement) == 0
                //Primary key is not set in update.
                && !(!insert && (it.Value.Attributes & Attributes.PrimaryKey) != 0))
            {
                u.Columns.Add(it.Key);
            }
        }

        internal static string[] GetMembers(GXDBSettings settings, Expression expression, char quoteSeparator, bool where, ref string post)
        {
            return GetMembers(settings, expression, quoteSeparator, where, false, ref post, false);
        }

        internal static string[] HandleMethod(GXDBSettings settings, UnaryExpression unaryExpression, MethodCallExpression m, char quoteSeparator, bool where, bool getValue, ref string post)
        {
            if (m.Method.DeclaringType == typeof(GXSql))
            {
                if (m.Method.Name == "Count")
                {
                    string value = null;
                    if (m.Arguments[0].NodeType != ExpressionType.Parameter)
                    {
                        value = GetMembers(settings, m.Arguments[0], quoteSeparator, where, ref post)[0];
                    }
                    if (value == null || value == AddQuotes("*", quoteSeparator))
                    {
                        return new string[] { "COUNT(1)" };
                    }
                    return new string[] { "COUNT(" + value + ")" };
                }
                if (m.Method.Name == "DistinctCount")
                {
                    string value = null;
                    if (m.Arguments[0].NodeType != ExpressionType.Parameter)
                    {
                        value = GetMembers(settings, m.Arguments[0], quoteSeparator, where, ref post)[0];
                    }
                    if (value == null || value == AddQuotes("*", quoteSeparator))
                    {
                        return new string[] { "COUNT(DISTINCT 1)" };
                    }
                    return new string[] { "COUNT(DISTINCT " + value + ")" };
                }
                if (m.Method.Name == "In")
                {
                    string[] list = GetMembers(settings, m.Arguments[1], quoteSeparator, where, true, ref post, false);
                    string tmp = "(" + GetMembers(settings, m.Arguments[0], quoteSeparator, where, false, ref post, false)[0];
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
                    string tmp;
                    if (m.Arguments.Count == 3)
                    {
                        if (unaryExpression != null && unaryExpression.NodeType == ExpressionType.Not)
                        {
                            tmp = "(NOT EXISTS (";
                        }
                        else
                        {
                            tmp = "(EXISTS (";
                        }
                        tmp += GetMembers(settings, m.Arguments[2], quoteSeparator, where, false, ref post, false)[0] + " AND " +
                                  GetMembers(settings, m.Arguments[1], quoteSeparator, where, false, ref post, false)[0] + " = " +
                                  GetMembers(settings, m.Arguments[0], quoteSeparator, where, false, ref post, false)[0] + "))";
                        return new string[] { tmp };
                    }
                    if (m.Arguments.Count == 1)
                    {
                        if (unaryExpression != null && unaryExpression.NodeType == ExpressionType.Not)
                        {
                            tmp = "(NOT EXISTS (";
                        }
                        else
                        {
                            tmp = "(EXISTS (";
                        }
                        tmp += GetMembers(settings, m.Arguments[0], quoteSeparator, where, false, ref post, false)[0] + "))";
                        return new string[] { tmp };
                    }
                    else
                    {
                        throw new ArgumentOutOfRangeException("Exist failed.");
                    }
                }
                if (m.Method.Name == "Contains")
                {
                    return new string[] {"(" + GetMembers(settings, m.Arguments[0], quoteSeparator, where, ref post)[0] + " LIKE('%" +
                            GetMembers(settings, m.Arguments[1], '\0', where, ref post)[0] + "%'))"};
                }
                if (m.Method.Name == "IsEmpty")
                {
                    post = ")";
                    return new string[] { "COUNT(1) WHERE NOT EXISTS (SELECT 1" };
                }
            }
            if (m.Method.DeclaringType == typeof(System.Linq.Enumerable) && m.Method.Name == "Contains")
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("(" + GetMembers(settings, m.Arguments[1], quoteSeparator, where, ref post)[0]);
                if (unaryExpression != null && unaryExpression.NodeType == ExpressionType.Not)
                {
                    sb.Append(" NOT IN (");
                }
                else
                {
                    sb.Append(" IN (");
                }
                foreach (string it in GetMembers(settings, m.Arguments[0], quoteSeparator, where, ref post))
                {
                    sb.Append(it);
                    sb.Append(", ");
                }
                sb.Length -= 2;
                sb.Append("))");
                return new string[] { sb.ToString() };
            }
            if (typeof(IEnumerable).IsAssignableFrom(m.Method.DeclaringType) &&
                m.Method.DeclaringType != typeof(string) &&
                m.Method.Name == "Contains")
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("(" + GetMembers(settings, m.Arguments[0], quoteSeparator, where, ref post)[0]);
                if (unaryExpression != null && unaryExpression.NodeType == ExpressionType.Not)
                {
                    sb.Append(" NOT IN (");
                }
                else
                {
                    sb.Append(" IN (");
                }
                foreach (string it in GetMembers(settings, m.Object, quoteSeparator, where, ref post))
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
                return new string[] {"(" + GetMembers(settings, m.Object, quoteSeparator, where, ref post)[0] + " LIKE('" +
                            GetMembers(settings, m.Arguments[0], '\0', where, ref post)[0] + "%'))"};
            }
            if (m.Method.DeclaringType == typeof(string) && m.Method.Name == "EndsWith")
            {
                return new string[] {"(" + GetMembers(settings, m.Object, quoteSeparator, where, ref post)[0] + " LIKE('%" +
                            GetMembers(settings, m.Arguments[0], '\0', where, ref post)[0] + "'))"};
            }
            if (m.Method.DeclaringType == typeof(string) && m.Method.Name == "Contains")
            {
                return new string[] {"(" + GetMembers(settings, m.Object, quoteSeparator, where, ref post)[0] + " LIKE('%" +
                            GetMembers(settings, m.Arguments[0], '\0', where, ref post)[0] + "%'))"};
            }
            if (m.Method.Name == "Equals")
            {
                if (m.Method.DeclaringType == typeof(string))
                {
#if !NETCOREAPP2_0 && !NETCOREAPP2_1
                    if (settings.Type == DatabaseType.Access)
                    {
                        return new string[] {"(" + GetMembers(settings, m.Object, quoteSeparator, where, ref post)[0] + " LIKE('" +
                                GetMembers(settings, m.Arguments[0], '\0', where, true, ref post, false)[0] + "'))"};
                    }
#endif //!NETCOREAPP2_0 && !NETCOREAPP2_1
                    string tmp = GetMembers(settings, m.Arguments[0], '\0', where, true, ref post, false)[0];
                    if (tmp == null)
                    {
                        tmp = GetMembers(settings, m.Object, quoteSeparator, where, ref post)[0];
                        return new string[] { "(" + tmp + " IS NULL)" };
                    }
                    tmp = tmp.ToUpper();
                    if (tmp[0] != '\'')
                    {
                        tmp = "'" + tmp + "'";
                    }
                    return new string[] {"(UPPER(" + GetMembers(settings, m.Object, quoteSeparator, where, ref post)[0] + ") LIKE(" +
                                 tmp + "))"};
                }
                else
                {
                    string value = GetMembers(settings, m.Arguments[0], quoteSeparator, where, true, ref post, false)[0];
                    if (value == null)
                    {
                        return new string[] { "(" + GetMembers(settings, m.Object, quoteSeparator, where, ref post)[0] + " IS NULL)" };
                    }
                    return new string[] {"(" + GetMembers(settings, m.Object, quoteSeparator, where, ref post)[0] + "=" +
                            value.ToUpper() + ")"};
                }
            }
            if (m.Method.DeclaringType == typeof(string) && m.Method.Name == "IsNullOrEmpty")
            {
                string tmp = GetMembers(settings, m.Arguments[0], quoteSeparator, where, ref post)[0];
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
            object value22 = Expression.Lambda(m).Compile().DynamicInvoke();
            return new string[] { settings.ConvertToString(value22, where) };
        }

        internal static string[] GetMembers(
            GXDBSettings settings,
            Expression expression,
            char quoteSeparator,
            bool where,
            bool getValue,
            ref string post,
            bool isParameter)
        {
            if (expression == null)
            {
                throw new ArgumentException("The expression cannot be null.");
            }

            if (expression is LambdaExpression)
            {
                LambdaExpression lambdaEx = expression as LambdaExpression;
                return GetMembers(settings, lambdaEx.Body, quoteSeparator, where, ref post);
            }

            if (expression is MemberExpression)
            {
                // Reference type property or field
                var memberExpression = (MemberExpression)expression;
                Expression e = memberExpression.Expression;
                if (memberExpression.Member.DeclaringType == typeof(GXSql) &&
                    memberExpression.Member.Name == "One")
                {
                    return new string[] { "1()" };
                }
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
                        foreach (object it in value as IEnumerable)
                        {
                            if (first)
                            {
                                first = false;
                            }
                            else
                            {
                                sb.Append(", ");
                            }
                            if (it is string || it is DateTime)
                            {
                                sb.Append("'");
                                sb.Append(it);
                                sb.Append("'");
                            }
                            else if (it is Guid)
                            {
                                sb.Append("'");
                                sb.Append(it.ToString());
                                sb.Append("'");
                            }
                            else
                            {
                                sb.Append(Convert.ToString(it));
                            }
                        }
                        return new string[] { sb.ToString() };
                    }
                    else if (value is PropertyInfo p)
                    {
                        //In where get table type and column name.
                        if (where && p.DeclaringType.IsClass)
                        {
                            if (GXDbHelpers.IsAliasName(p.DeclaringType))
                            {
                                return new string[] { GXDbHelpers.OriginalTableName(p.DeclaringType) +
                            "." + GetColumnName(value as PropertyInfo, settings.ColumnQuotation) };
                            }
                            return new string[] { GXDbHelpers.GetTableName(p.DeclaringType, true, settings) +
                            "." + GetColumnName(value as PropertyInfo, settings.ColumnQuotation) };
                        }
                        return new string[] { GetColumnName(value as PropertyInfo, quoteSeparator) };
                    }
                    if (quoteSeparator == '\0')
                    {
                        return new string[] { Convert.ToString(value) };
                    }
                    if (value != null && value.GetType().IsClass)
                    {
                        GXSerializedItem si = GXSqlBuilder.FindUnique(value.GetType());
                        if (si != null && si.Target is PropertyInfo p)
                        {
                            value = si.Target;
                            //In where get table type and column name.
                            if (where && p.DeclaringType.IsClass)
                            {
                                if (GXDbHelpers.IsAliasName(p.DeclaringType))
                                {
                                    return new string[] { GXDbHelpers.OriginalTableName(p.DeclaringType) +
                            "." + GetColumnName(value as PropertyInfo, settings.ColumnQuotation) };
                                }
                                return new string[] { GXDbHelpers.GetTableName(p.DeclaringType, true, settings) +
                            "." + GetColumnName(value as PropertyInfo, settings.ColumnQuotation) };
                            }
                            return new string[] { GetColumnName(value as PropertyInfo, quoteSeparator) };
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
                            foreach (object it in (target as IEnumerable))
                            {
                                if (first)
                                {
                                    first = false;
                                }
                                else
                                {
                                    sb.Append(", ");
                                }
                                if (it is string || it is DateTime)
                                {
                                    sb.Append("'");
                                    sb.Append(it);
                                    sb.Append("'");
                                }
                                else if (it is Guid)
                                {
                                    sb.Append("'");
                                    sb.Append(it.ToString());
                                    sb.Append("'");
                                }
                                else
                                {
                                    sb.Append(it);
                                }
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
                        string str;
                        if (target is DateTime)
                        {
                            str = settings.ConvertToString(target, where);
                        }
                        else
                        {
                            str = Convert.ToString(target);
                        }
                        if (where && getValue && target is string)
                        {
                            return new string[] { "'" + str + "'" };
                        }
                        if (where && getValue && target is Guid)
                        {
                            return new string[] { "'" + str + "'" };
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
                        if ((it.Value.Attributes & Attributes.Id) != 0)
                        {
                            //If collection
                            if (target is IEnumerable)
                            {
                                sb.Append('(');
                                sb.Append(GetColumnName(it.Value.Target as PropertyInfo, settings.ColumnQuotation));
                                sb.Append(" IN(");
                                IEnumerator e2 = (target as IEnumerable).GetEnumerator();
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
                                    string str;
                                    if (value is Guid g)
                                    {
                                        str = GetQuetedValue(g.ToString().ToUpper());
                                    }
                                    else if (value is string s)
                                    {
                                        str = GetQuetedValue(s);
                                    }
                                    else
                                    {
                                        str = value.ToString();
                                    }
                                    sb.Append(str);
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
                            if (where)
                            {
                                if (value == null)
                                {
                                    return new string[] { "(" + GetColumnName(it.Value.Target as PropertyInfo, settings.ColumnQuotation) + " IS NULL)" };
                                }
                                if (isParameter)
                                {
                                    //If where => where.User == user
                                    return new string[] { ConvertToString(value, settings, where) };
                                }
                                return new string[] { "(" + GetColumnName(it.Value.Target as PropertyInfo, settings.ColumnQuotation) + " = " + ConvertToString(value, settings, where) + ")" };
                            }
                            else
                            {
                                return new string[] { "(" + GetColumnName(it.Value.Target as PropertyInfo, settings.ColumnQuotation) + " = " + ConvertToString(value, settings, where) + ")" };
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
                if (methodCallExpression.Arguments.Count != 0 &&
                    (methodCallExpression.Arguments[0].NodeType == ExpressionType.MemberAccess ||
                    methodCallExpression.Arguments[0].NodeType == ExpressionType.Constant ||
                    methodCallExpression.Arguments[0].NodeType == ExpressionType.Convert))
                {
                    return HandleMethod(settings, null, methodCallExpression, quoteSeparator, where, getValue, ref post);
                }
                object value = Expression.Lambda(methodCallExpression).Compile().DynamicInvoke();
                if (where && getValue && value is string)
                {
                    return new string[] { AddQuotes((string)value, '\'') };
                }
                else if (where && getValue && value is Guid)
                {
                    return new string[] { AddQuotes(value.ToString(), '\'') };
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
                    return HandleMethod(settings, unaryExpression, m, quoteSeparator, where, getValue, ref post);
                }
                return GetMembers(settings, unaryExpression.Operand, quoteSeparator, where, getValue, ref post, true);
            }

            if (expression is NewExpression)
            {
                // Property, field of method returning value type
                var newExpression = (NewExpression)expression;
                List<string> list = new List<string>();
                foreach (var it in newExpression.Arguments)
                {
                    list.AddRange(GetMembers(settings, it, quoteSeparator, where, ref post));
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
                    foreach (string var in GetMembers(settings, it, quoteSeparator, where, ref post))
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
                        list.Add("(" + GetMembers(settings, newExpression.Left, quoteSeparator, where, ref post)[0] + " AND " +
                            GetMembers(settings, newExpression.Right, quoteSeparator, where, ref post)[0] + ")");
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
                        list.Add("POWER(" + GetMembers(settings, newExpression.Left, quoteSeparator, where, ref post)[0] + ", " +
                            GetMembers(settings, newExpression.Right, quoteSeparator, where, ref post)[0] + ")");
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
                    tmp = GetMembers(settings, newExpression.Right, quoteSeparator, where, ref post)[0];
                    if (settings.UseEnumStringValue)
                    {
                        tmp = Enum.Parse(u.Operand.Type, tmp, true).ToString();
                        tmp = AddQuotes(tmp, '\'');
                    }
                }
                else
                {
                    bool isClass2 = newExpression.Left.Type.IsClass && newExpression.Left.Type != typeof(string);
                    tmp = GetMembers(settings, newExpression.Right, quoteSeparator, where, true, ref post, isClass2)[0];
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
                list.Add("(" + GetMembers(settings, newExpression.Left, quoteSeparator, where, ref post)[0] + op + tmp + ")");
                return list.ToArray();
            }

            if (expression is ConstantExpression ce)
            {
                if (ce.Value is string)
                {
                    if ("*".Equals(ce.Value))
                    {
                        return new string[] { (string)ce.Value };
                    }
                    return new string[] { AddQuotes((string)ce.Value, quoteSeparator) };
                }
                else if (ce.Value == null)
                {
                    return new string[] { null };
                }
                else if (ce.Value is bool)
                {
                    return new string[] { ((bool)ce.Value) ? "1" : "0" };
                }
                else
                {
                    return new string[] { ce.Value.ToString() };
                }
            }

            throw new ArgumentException("Invalid expression");
        }
    }
}
