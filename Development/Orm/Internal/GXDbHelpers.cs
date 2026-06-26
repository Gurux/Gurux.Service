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
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Enums;
using Gurux.Service.Orm.Settings;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace Gurux.Service.Orm.Internal
{
    internal sealed class GXGetMembersArgs
    {
        internal GXDBSettings Settings;
        internal Expression Expression;
        internal TargetType TargetType;

        internal string Post;
        /// <summary>
        /// List separator is used when multiple columns are generated with one expression. 
        /// For example in insert when columns are defined with new operator. 
        /// In that case list separator is used to separate column names and values.
        /// </summary>
        internal string ListSeparator = ", ";
        /// <summary>
        /// Keep list of amount of the operations.
        /// </summary>
        internal Dictionary<string, int> OperationCount = null;

        /// <summary>
        /// Table name is not needed if data is retreaved from single table because of inheritance. 
        /// In that case all data is in one table and table name is not needed to get correct column name.
        /// </summary>
        internal bool SingleTable;

        internal UnaryExpression UnaryExpression;
        internal MethodCallExpression MethodCallExpression;

        internal StringBuilder StringBuilder;

        public GXGetMembersArgs(GXDBSettings settings, TargetType targetType)
        {
            Settings = settings;
            TargetType = targetType;
        }

        public override string ToString()
        {
            if (StringBuilder != null)
            {
                return StringBuilder.ToString();
            }
            return base.ToString();
        }
    }

    static class GXDbHelpers
    {
        /// <summary>
        /// Add quotes around the value.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="dataQuote"></param>
        /// <param name="quoteSeparator"></param>
        /// <returns></returns>
        internal static string AddQuotes(string value, string dataQuote, char quoteSeparator)
        {
            if (!string.IsNullOrEmpty(dataQuote) &&
                string.IsNullOrEmpty(value))
            {
                value = value.Replace("'", dataQuote);
            }
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args">GXGetMembersArgs caller.</param>
        /// <param name="upd">GXUpdateArgs caller.</param>
        /// <param name="values">List of values to be updated or added.</param>
        /// <param name="excluded">List of excluded columns.</param>
        /// <param name="queries">List of generated queries.</param>
        /// <param name="where">Where clause collection.</param>
        /// <param name="insertedObjects">List of inserted objects.</param>
        internal static void GetQueries(
            GXGetMembersArgs args,
            GXUpdateArgs upd,
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
                GetValues(args, it.Key, null, it.Value, list, excluded, upd == null, false,
                    args.Settings.ColumnNameQuoteCharacter, false, where, handledObjects, insertedObjects);
            }
            foreach (KeyValuePair<Type, GXUpdateItem> table in list)
            {
                if (upd != null)
                {
                    GetUpdateQuery(upd, table, args.Settings, queries);
                }
                else
                {
                    GetInsertQuery(args, table, queries);
                }
            }
        }

        internal static void GetUpdateQuery(
            GXUpdateArgs args,
            KeyValuePair<Type, GXUpdateItem> table,
            GXDBSettings settings,
            List<string> queries)
        {
            if (!table.Value.Inserted)
            {
                StringBuilder sb = new StringBuilder();
                int index = 0, colIndex = 0;
                object value;
                sb.Length = 0;
                TargetType type = TargetType.Table;
                if (!AddQuetationAlways(settings))
                {
                    type |= TargetType.Plain;
                }
                string tableName = ConvertToString(args.Settings,
                     type, null, table.Key, null);
                bool first;
                foreach (var col in table.Value.Rows)
                {
                    ++index;
                    if (args.Count != 0 && index == args.Count)
                    {
                        break;
                    }
                    sb.Append("UPDATE ");
                    sb.Append(tableName);
                    sb.Append(" SET ");
                    colIndex = 0;
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
                        sb.Append(ConvertToString(args.Settings, TargetType.Column, null, table.Value.Columns[colIndex], null));
                        sb.Append(" = ");
                        GXSerializedItem row = it.Value;
                        if (row != null)
                        {
                            value = row.Get(it.Key);
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
                        if (value == null && (row.Attributes & Attributes.AllowNull) == 0)
                        {
                            //Get the default value if nullable value is null and it's not allowed.
                            value = row.DefaultValue;
                        }
                        sb.Append(ConvertToString(settings, TargetType.Value, null, value, null));
                        ++colIndex;
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

        private static bool AddQuetationAlways(GXDBSettings settings)
        {
            return settings.Type == DatabaseType.PostgreSQL;
        }

        private static void GetInsertColumns(
            GXDBSettings settings,
            bool first,
            KeyValuePair<Type, GXUpdateItem> table,
            StringBuilder sb,
            bool select)
        {
            TargetType type = TargetType.Table;
            if (!AddQuetationAlways(settings))
            {
                type |= TargetType.Plain;
            }
            string tableName = ConvertToString(settings,
                type,
                null, table.Key, null);
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
                sb.Append(ConvertToString(settings, TargetType.Column, null, col, null));
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
                    ret = (o1 as ValueType).Equals(Convert.ChangeType(ZeroValue, o1.GetType()));
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
            GXGetMembersArgs args,
            KeyValuePair<Type, GXUpdateItem> table,
            List<string> queries,
            List<int> queryRowCounts = null)
        {
            StringBuilder sb = new StringBuilder();
            object value;
            sb.Length = 0;
            bool firstRow = true, first, select = false;
            string post = null;
            int maxRows = GXSqlBuilder.FindAutoIncrement(table.Key) != null ?
                1 : Math.Max(1, args.Settings.MaximumRowUpdate);
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
            GetInsertColumns(args.Settings, true, table, sb, select);
            args.TargetType = TargetType.Value;
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
                    if (args.Settings.Type == DatabaseType.Oracle)
                    {
                        GetInsertColumns(args.Settings, false, table, sb, select);
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
                        if (it.Key is GXSelectArgs sa)
                        {
                            //Remove VALUES( or SELECT
                            sb.Length -= 7;
                            sb.Append(sa.ToString(false));
                            break;
                        }
                        first = false;
                    }
                    else
                    {
                        sb.Append(args.ListSeparator);
                    }
                    GXSerializedItem row = it.Value;
                    if (row != null)
                    {
                        if (it.Key is GXSelectArgs s)
                        {
                            s.Columns.Insert = true;
                            try
                            {
                                string sql = s.ToString(false);
                                if (select)
                                {
                                    //Remove duplicate select
                                    sql = sql.Substring(7);
                                    int index = sql.IndexOf(" FROM ", StringComparison.OrdinalIgnoreCase);
                                    post = sql.Substring(index);
                                    sql = sql.Substring(0, index);
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
                                if ((row.Attributes & Attributes.AllowNull) == 0)
                                {
                                    if ((row.Attributes & Attributes.ForeignKey) != 0 &&
                                        IsZeroOrEmpty(value))
                                    {
                                        if (row.Target is PropertyInfo pi)
                                        {
                                            throw new ArgumentException("Foreign key can't be null. " + table.Key.Name + "." + pi.Name);
                                        }
                                        throw new ArgumentException("Foreign key can't be null. + " + table.Key.Name);
                                    }
                                    if (value == null)
                                    {
                                        //Get the dafault value.
                                        value = row.DefaultValue;
                                    }
                                }
                            }
                            else
                            {
                                value = GXInternal.GetValue(it.Key, row.Target);
                            }
                            if ((row.Attributes & Attributes.MsIgnored) != 0)
                            {
                                //If ms part is ignored.
                                args.TargetType |= TargetType.IgnoreMs;
                            }
                            else
                            {
                                args.TargetType &= ~TargetType.IgnoreMs;
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
                    sb.Append(ConvertToString(args, value));
                }
                if (!select)
                {
                    sb.Append(")");
                }
                //If all rows can't insert with one query.
                if (rowCnt >= maxRows)
                {
                    if (args.Settings.Type == DatabaseType.Oracle)
                    {
                        sb.Append(" SELECT 1 FROM DUAL");
                    }
                    queries.Add(sb.ToString());
                    queryRowCounts?.Add(rowCnt);
                    sb.Length = 0;
                    GetInsertColumns(args.Settings, true, table, sb, select);
                    firstRow = true;
                }
            }
            if (!string.IsNullOrEmpty(post))
            {
                sb.Append(post);
            }
            if (!firstRow)
            {
                if (args.Settings.Type == DatabaseType.Oracle)
                {
                    sb.Append(" SELECT 1 FROM DUAL");
                }
                queries.Add(sb.ToString());
                queryRowCounts?.Add(rowCnt);
            }
            return table.Value.Rows.Count;
        }

        /// <summary>
        /// Get added or updated values.
        /// </summary>
        /// <param name="args">Database settings.</param>
        /// <param name="value">Value to insert or update.</param>
        /// <param name="parent">Parent object.</param>
        /// <param name="columns">Columns to update or insert.</param>
        /// <param name="itemsList">List of items to update.</param>
        /// <param name="excluded">Excluded columns.</param>
        /// <param name="insert">Indicates if the operation is an insert.</param>
        /// <param name="mapTable">Indicates if the table should be mapped.</param>
        /// <param name="columnQuotation">Character used for column quotation.</param>
        /// <param name="updating">Indicates if the operation is an update.</param>
        /// <param name="where">Indicates if the operation is a where clause.</param>
        /// <param name="handledObjects">List of handled objects.</param>
        /// <param name="insertedObjects">List of inserted objects.</param>
        internal static void GetValues(
            GXGetMembersArgs args,
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
                    arg.Settings = args.Settings;
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
                            if (v is GXTableBase b)
                            {
                                if (insert)
                                {
                                    b.BeforeAdd();
                                }
                                else
                                {
                                    b.BeforeUpdate();
                                }
                            }
                            GetValues(args, v, parent, columns, itemsList, excluded, insert, false,
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
                            PropertyInfo pi = si.Target as PropertyInfo;
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
                            if ((si.Attributes & Attributes.Id) != 0)
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
                string[] updatedProperties = null;
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
                        GXWhereCollection tmp2 = new GXWhereCollection(where.Parent, where.Joins);
                        tmp2.List.AddRange(where.List);
                        //Remove get by ID if where is added.
                        tmp2.List.RemoveAt(0);
                        u.Where.Add(tmp2.ToString());
                    }
                    else
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.Append("WHERE ");
                        sb.Append(ConvertToString(args.Settings, TargetType.Column, null, si.Target, null));
                        sb.Append(" = ");
                        sb.Append(ConvertToString(args.Settings, TargetType.Value, null, si.Get(value), null));
                        u.Where.Add(sb.ToString());
                    }
                }
                //Get inserted column names.
                if (columns != null && !update)
                {
                    args.Expression = columns.Body;
                    var colums = GetMembers(args, TargetType.Column | TargetType.Plain);
                    var colums2 = GetMemberList(args);
                    foreach (string item in colums)
                    {
                        KeyValuePair<string, GXSerializedItem> it = new KeyValuePair<string, GXSerializedItem>(item, properties[item]);
                        GetColumn(args, value, itemsList, excluded, insert, mapTable, columnQuotation, updating, where, type, u, update, it);
                    }
                }
                else
                {
                    Dictionary<Type, List<(string, Type)>> cols = null;
                    if (value is GXSelectArgs s)
                    {
                        if (columns != null)
                        {
                            //Remove last row.
                            u.Rows.RemoveAt(u.Rows.Count - 1);
                            row = u.Rows.Last();
                            args.Expression = columns.Body;
                            updatedProperties = GetMemberList(args);
                            //Check if column is updated earlier and remove old update.
                            if (!u.Columns.Any(s => updatedProperties.Contains(s)))
                            {
                                u.Columns.AddRange(updatedProperties);
                            }
                            else
                            {
                                List<string> updated = new List<string>();
                                foreach (string p in updatedProperties)
                                {
                                    int pos = u.Columns.IndexOf(p);
                                    if (pos != -1)
                                    {
                                        KeyValuePair<object, GXSerializedItem> old = row[pos];
                                        row.RemoveAt(pos);
                                        row.Insert(pos, new KeyValuePair<object, GXSerializedItem>(value, old.Value));
                                        inserted = true;
                                    }
                                    else
                                    {
                                        u.Columns.Add(p);
                                        updated.Add(p);
                                    }
                                }
                                updatedProperties = updated.ToArray();
                            }
                        }
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
                        //Make copy.
                        cols = s.Columns.ColumnList.ToDictionary();
                        //Unknown columns are not added to select.
                        foreach (var c in cols)
                        {
                            if (c.Key == type)
                            {
                                foreach (string r in c.Value.Select(s => s.Item1))
                                {
                                    if (!properties.ContainsKey(r))
                                    {
                                        Expression<Func<object, object>> expression = _ => r;
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
                                List<string> list = new List<string>();
                                if (removed != null)
                                {
                                    list.AddRange(removed);
                                }
                                args.Expression = e.Value.Body;
                                list.AddRange(GetMemberList(args));
                                removed = list.ToArray();
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
                        if (value is GXSelectArgs args2)
                        {
                            //Unknown columns are not added.
                            bool found = false;
                            foreach (var c in cols)
                            {
                                if (c.Key == type)
                                {
                                    foreach (var r in c.Value.Select(s => s.Item1))
                                    {
                                        if (it.Key == r)
                                        {
                                            //Don't add auto increment value.
                                            if ((it.Value.Attributes & Attributes.AutoIncrement) != 0)
                                            {
                                                Expression<Func<object, object>> expression = q => r;
                                                args2.Columns.Excluded.Add(new KeyValuePair<Type, LambdaExpression>(c.Key, expression));
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
                        }
                        GetColumn(args, value, itemsList, excluded, insert, mapTable, columnQuotation, updating, where, type, u, update, it);
                    }
                }
                if (excluded != null)
                {
                    foreach (KeyValuePair<Type, LambdaExpression> it in excluded)
                    {
                        if (it.Key == type)
                        {
                            args.Expression = it.Value;
                            string[] removed = GetMemberList(args);
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
                    if (updatedProperties != null &&
                        updatedProperties.Length != 0 &&
                        !updatedProperties.Contains(it))
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
                            target = GXInternal.GetValue(parent, item.Target);
                            row.Add(new KeyValuePair<object, GXSerializedItem>(parent, item));
                        }
                        else if (item.Relation.RelationType == RelationType.OneToOne)
                        {
                            target = GXInternal.GetValue(value, item.Target);
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
                                    GetValues(args, target, parent, null, itemsList, excluded,
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
                                GetValues(args, v, parent, null, itemsList, excluded, insert, mapTable,
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
        private static void GetColumn(GXGetMembersArgs args, object value,
            List<KeyValuePair<Type, GXUpdateItem>> itemsList,
            List<KeyValuePair<Type, LambdaExpression>> excluded,
            bool insert, bool mapTable, char columnQuotation, bool updating, GXWhereCollection where, Type type, GXUpdateItem u, bool update, KeyValuePair<string, GXSerializedItem> it)
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
                    else if (target != null && it.Value.Relation.RelationType != RelationType.Relation)
                    {
                        GetValues(args, target, value, null, itemsList,
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
                            m.Columns.Add(ConvertToString(args.Settings, TargetType.Column | TargetType.Plain, null, it.Value.Relation.RelationMapTable.Relation.PrimaryId.Target as PropertyInfo, null));
                            m.Columns.Add(ConvertToString(args.Settings, TargetType.Column | TargetType.Plain, null, GXSqlBuilder.FindRelation(it.Value.Relation.RelationMapTable.Relation.PrimaryTable, type).Target as PropertyInfo, null));
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

        internal static string GetMemberStringValue(GXGetMembersArgs args)
        {
            var old = args.StringBuilder;
            args.StringBuilder = new StringBuilder();
            GetMembers(args, TargetType.Value);
            string tmp = null;
            if (args.StringBuilder.Length != 0)
            {
                tmp = args.StringBuilder.ToString();
            }
            args.StringBuilder = old;
            return tmp;
        }

        internal static string[] GetMemberList(GXGetMembersArgs args)
        {
            var type = args.TargetType;
            var old = args.StringBuilder;
            args.StringBuilder = null;
            var list = GetMembers(args);
            args.StringBuilder = old;
            args.TargetType = type;
            return list;
        }

        internal static string[] HandleMethod(GXGetMembersArgs args, TargetType targetType)
        {
            var old = args.TargetType;
            args.TargetType = targetType;
            try
            {
                return HandleMethod(args);
            }
            finally
            {
                args.TargetType = old;
            }
        }

        internal static string[] HandleMethod(GXGetMembersArgs args)
        {
            if (args.MethodCallExpression.Method.DeclaringType.IsGenericType &&
                args.MethodCallExpression.Method.DeclaringType.GetGenericTypeDefinition() == typeof(ReadOnlySpan<>))
            {
                args.Expression = args.MethodCallExpression.Arguments[0];
                string[] ret = GetMembers(args);
                return ret;
            }
            if (args.MethodCallExpression.Method.DeclaringType == typeof(GXSql))
            {
                if (args.MethodCallExpression.Method.Name == "Count")
                {
                    string value = null;
                    if (args.MethodCallExpression.Arguments[0].NodeType != ExpressionType.Parameter)
                    {
                        args.Expression = args.MethodCallExpression.Arguments[0];
                        value = GetMemberStringValue(args);
                    }
                    if (value == null || value == AddQuotes("*", null, args.Settings.TableNameQuoteCharacter))
                    {
                        if (args.StringBuilder == null)
                        {
                            return ["COUNT(1)"];
                        }
                        args.StringBuilder.Append("COUNT(1)");
                    }
                    else
                    {
                        if (args.StringBuilder == null)
                        {
                            return ["COUNT(" + value + ")"];
                        }
                        args.StringBuilder.Append("COUNT(" + value + ")");
                    }
                    return null;
                }
                if (args.MethodCallExpression.Method.Name == "DistinctCount")
                {
                    string value = null;
                    if (args.MethodCallExpression.Arguments[0].NodeType != ExpressionType.Parameter)
                    {
                        args.Expression = args.MethodCallExpression.Arguments[0];
                        value = GetMemberStringValue(args);
                    }
                    if (value == null || value == AddQuotes("*", null, args.Settings.TableNameQuoteCharacter))
                    {
                        return ["COUNT(DISTINCT 1)"];
                    }
                    return ["COUNT(DISTINCT " + value + ")"];
                }
                if (args.MethodCallExpression.Method.Name == "In")
                {
                    args.Expression = args.MethodCallExpression.Arguments[0];
                    GetMembers(args);
                    if (args.UnaryExpression != null && args.UnaryExpression.NodeType == ExpressionType.Not)
                    {
                        args.StringBuilder.Append(" NOT IN (");
                    }
                    else
                    {
                        args.StringBuilder.Append(" IN (");
                    }
                    args.Expression = args.MethodCallExpression.Arguments[1];
                    GetMembers(args);
                    args.StringBuilder.Append(")");
                    return null;
                }
                if (args.MethodCallExpression.Method.Name == "Exists")
                {
                    if (args.MethodCallExpression.Arguments.Count == 3 || args.MethodCallExpression.Arguments.Count == 4)
                    {
                        var mc = args.MethodCallExpression;
                        if (args.UnaryExpression != null && args.UnaryExpression.NodeType == ExpressionType.Not)
                        {
                            args.StringBuilder.Append("NOT EXISTS (");
                        }
                        else
                        {
                            args.StringBuilder.Append("EXISTS (");
                        }
                        args.Expression = mc.Arguments[2];
                        bool old = args.SingleTable;
                        args.SingleTable = false;
                        string query = GetMemberStringValue(args);
                        args.StringBuilder.Append(query);
                        if (query.IndexOf("WHERE") == -1)
                        {
                            args.StringBuilder.Append(" WHERE ");
                        }
                        else
                        {
                            args.StringBuilder.Append(" AND ");
                        }
                        args.Expression = mc.Arguments[1];
                        GetMembers(args);
                        args.StringBuilder.Append(" = ");
                        args.Expression = mc.Arguments[0];
                        GetMembers(args);
                        args.StringBuilder.Append(")");
                        args.SingleTable = old;
                        return null;
                    }
                    if (args.MethodCallExpression.Arguments.Count == 1)
                    {
                        if (args.UnaryExpression != null && args.UnaryExpression.NodeType == ExpressionType.Not)
                        {
                            args.StringBuilder.Append("NOT EXISTS (");
                        }
                        else
                        {
                            args.StringBuilder.Append("EXISTS (");
                        }
                        args.Expression = args.MethodCallExpression.Arguments[0];
                        GetMembers(args);
                        args.StringBuilder.Append(")");
                        return null;
                    }
                    else
                    {
                        throw new ArgumentOutOfRangeException("Exist failed.");
                    }
                }
                if (args.MethodCallExpression.Method.Name == "Contains")
                {
                    args.Expression = args.MethodCallExpression.Arguments[0];
                    var tmp = "(" + GetMembers(args)[0];
                    tmp += " LIKE('%";
                    args.Expression = args.MethodCallExpression.Arguments[1];
                    tmp += GetMembers(args)[0] + "%'))";
                }
                if (args.MethodCallExpression.Method.Name == "IsEmpty")
                {
                    args.Post = ") THEN 1 ELSE 0 END AS IsEmpty";
                    if (args.Settings.Type == DatabaseType.Oracle)
                    {
                        args.Post += " FROM DUAL";
                    }
                    return ["CASE WHEN NOT EXISTS (SELECT 1"];
                }
                if (args.MethodCallExpression.Method.Name == "Greater")
                {
                    args.Expression = args.MethodCallExpression.Arguments[0];
                    var tmp = "(" + GetMembers(args)[0] + " > ";
                    args.Expression = args.MethodCallExpression.Arguments[1];
                    tmp += GetMembers(args)[0] + ")";
                }
                if (args.MethodCallExpression.Method.Name == "Less")
                {
                    args.Expression = args.MethodCallExpression.Arguments[0];
                    var tmp = "(" + GetMembers(args)[0] + " < ";
                    args.Expression = args.MethodCallExpression.Arguments[1];
                    tmp += GetMembers(args)[0] + ")";
                }
                if (args.MethodCallExpression.Method.Name == "GreaterOrEqual")
                {
                    args.Expression = args.MethodCallExpression.Arguments[0];
                    GetMembers(args);
                    args.StringBuilder.Append(" >= ");
                    args.Expression = args.MethodCallExpression.Arguments[1];
                    GetMembers(args);
                }
                if (args.MethodCallExpression.Method.Name == "LessOrEqual")
                {
                    args.Expression = args.MethodCallExpression.Arguments[0];
                    GetMembers(args);
                    args.StringBuilder.Append(" <= ");
                    args.Expression = args.MethodCallExpression.Arguments[1];
                    GetMembers(args);
                }
                if (args.MethodCallExpression.Method.Name == "Null")
                {
                    args.Expression = args.MethodCallExpression.Arguments[0];
                    var tmp = "(" + GetMembers(args)[0] + " IS NULL)";
                }
                if (args.MethodCallExpression.Method.Name == "NotNull")
                {
                    args.Expression = args.MethodCallExpression.Arguments[1];
                    var tmp = "(" + GetMembers(args)[0] + " IS NOT NULL)";
                }
            }
            if (args.MethodCallExpression.Method.Name == "Contains" &&
                (args.MethodCallExpression.Method.DeclaringType == typeof(Enumerable) ||
                args.MethodCallExpression.Method.DeclaringType == typeof(System.MemoryExtensions)))
            {
                var tmp = args.MethodCallExpression;
                args.Expression = args.MethodCallExpression.Arguments[1];
                GetMembers(args);
                if (args.UnaryExpression != null && args.UnaryExpression.NodeType == ExpressionType.Not)
                {
                    args.StringBuilder.Append(" NOT IN (");
                }
                else
                {
                    args.StringBuilder.Append(" IN (");
                }
                args.Expression = tmp.Arguments[0];
                GetMembers(args);
                args.StringBuilder.Append(")");
                return null;
            }
            if (typeof(IEnumerable).IsAssignableFrom(args.MethodCallExpression.Method.DeclaringType) &&
                args.MethodCallExpression.Method.DeclaringType != typeof(string) &&
                args.MethodCallExpression.Method.Name == "Contains")
            {
                args.Expression = args.MethodCallExpression.Arguments[0];
                GetMembers(args);
                if (args.UnaryExpression != null && args.UnaryExpression.NodeType == ExpressionType.Not)
                {
                    args.StringBuilder.Append(" NOT IN (");
                }
                else
                {
                    args.StringBuilder.Append(" IN (");
                }
                args.Expression = args.MethodCallExpression.Object;
                GetMembers(args, args.TargetType | TargetType.Plain);
                args.StringBuilder.Append(")");
                return null;
            }
            if (args.MethodCallExpression.Method.DeclaringType == typeof(string))
            {
                var mce = args.MethodCallExpression;
                if (mce.Method.Name == "StartsWith")
                {
                    return HandleMethod(args, mce, " LIKE('", "%')");
                }
                if (mce.Method.Name == "EndsWith")
                {
                    return HandleMethod(args, mce, " LIKE('%", "')");
                }
                if (mce.Method.Name == "Contains")
                {
                    return HandleMethod(args, mce, " LIKE('%", "%')");
                }
            }
            if (args.MethodCallExpression.Method.Name == "Equals")
            {
                var mce = args.MethodCallExpression;
                if (mce.Method.DeclaringType == typeof(string))
                {
                    args.Expression = args.MethodCallExpression.Arguments[0];
                    string tmp = GetMemberStringValue(args);
                    if (tmp == null)
                    {
                        args.Expression = args.MethodCallExpression.Object;
                        GetMembers(args);
                        args.StringBuilder.Append(" IS NULL");
                        return null;
                    }
                    tmp = tmp.ToUpper();
                    if (tmp[0] != '\'')
                    {
                        tmp = "'" + tmp + "'";
                    }
                    args.StringBuilder.Append("UPPER(");
                    args.Expression = args.MethodCallExpression.Object;
                    GetMembers(args);
                    args.StringBuilder.Append(") LIKE(" + tmp + ")");
                    return null;
                }
                else
                {
                    args.Expression = mce.Object;
                    GetMembers(args, TargetType.Column);
                    args.Expression = mce.Arguments[0];
                    string value = GetMemberStringValue(args);
                    bool not = args.UnaryExpression != null && args.UnaryExpression.NodeType == ExpressionType.Not;
                    if (value == "NULL")
                    {
                        if (not)
                        {
                            args.StringBuilder.Append(" IS NOT NULL");
                        }
                        else
                        {
                            args.StringBuilder.Append(" IS NULL");
                        }
                        return null;
                    }
                    args.StringBuilder.Append(" = ");
                    args.StringBuilder.Append(value.ToUpper());
                    return null;
                }
            }
            if (args.MethodCallExpression.Method.DeclaringType == typeof(string) && args.MethodCallExpression.Method.Name == "IsNullOrEmpty")
            {
                var mce = args.MethodCallExpression;
                args.Expression = mce.Arguments[0];
                GetMembers(args);
                string sql = args.StringBuilder.ToString();
                args.StringBuilder.Length = 0;
                if (args.Settings.Type == DatabaseType.Oracle)
                {
                    if (args.UnaryExpression.NodeType == ExpressionType.Not)
                    {
                        args.StringBuilder.Append(sql + " IS NOT NULL");
                    }
                    else
                    {
                        args.StringBuilder.Append(sql + " IS NULL");
                    }
                }
                else
                {
                    if (args.UnaryExpression.NodeType == ExpressionType.Not)
                    {
                        args.StringBuilder.Append("(" + sql + " IS NOT NULL AND " + sql + " <> '')");
                    }
                    else
                    {
                        args.StringBuilder.Append("(" + sql + " IS NULL OR " + sql + " = '')");
                    }
                }
                return null;
            }
            if (args.MethodCallExpression.Method.Name == "Sum" ||
                args.MethodCallExpression.Method.Name == "Max" ||
                args.MethodCallExpression.Method.Name == "Min" ||
                args.MethodCallExpression.Method.Name == "Avg")
            {
                return HandleOperation(args, args.MethodCallExpression.Method.Name.ToUpper());
            }
            object value22 = Expression.Lambda(args.MethodCallExpression).Compile().DynamicInvoke();
            return [args.Settings.ConvertToString(value22, ConvertOption.None)];
        }

        private static string[] HandleMethod(GXGetMembersArgs args, MethodCallExpression mce,
            string pre,
            string post)
        {
            args.Expression = mce.Object;
            GetMembers(args);
            args.StringBuilder.Append(pre);
            args.Expression = mce.Arguments[0];
            GetMembers(args, TargetType.Value | TargetType.Plain);
            args.StringBuilder.Append(post);
            return null;
        }

        private static string[] HandleOperation(GXGetMembersArgs args, string operation)
        {
            args.Expression = args.MethodCallExpression.Arguments[0];
            StringBuilder sb = new StringBuilder();
            args.StringBuilder = sb;
            sb.Append(operation);
            sb.Append('(');
            var listSeparator = args.ListSeparator;
            args.ListSeparator = " + ";
            GetMembers(args);
            args.ListSeparator = listSeparator;
            args.StringBuilder = null;
            sb.Append(") AS ");
            sb.Append(operation);
            if (args.OperationCount == null)
            {
                args.OperationCount = new Dictionary<string, int>();
            }
            if (!args.OperationCount.ContainsKey(operation))
            {
                args.OperationCount[operation] = 0;
            }
            ++args.OperationCount[operation];
            sb.Append(args.OperationCount[operation]);
            return [sb.ToString()];
        }

        internal static string[] GetMembers(GXGetMembersArgs args, TargetType targetType)
        {
            TargetType old = args.TargetType;
            args.TargetType = targetType;
            try
            {
                return GetMembers(args);
            }
            finally
            {
                args.TargetType = old;
            }
        }

        internal static string GetTableName(GXDBSettings settings, Type type, bool addQuotation = false)
        {
            if (type.BaseType != typeof(object) && type.BaseType.GetCustomAttributes(typeof(DataContractAttribute), true).Any())
            {
                return GetTableName(settings, type.BaseType);
            }
            if (!addQuotation)
            {
                addQuotation = settings != null &&
                    (settings.IsReservedWord(type.Name));
            }
            DataContractAttribute[] attr = (DataContractAttribute[])type.GetCustomAttributes(typeof(DataContractAttribute), true);
            if (!attr.Any() || attr[0].Name == null)
            {
                if (settings == null)
                {
                    return type.Name;
                }
                if (!addQuotation)
                {
                    return settings.TablePrefix + type.Name;
                }
                return AddQuotes(settings.TablePrefix + type.Name, null, settings.TableNameQuoteCharacter);
            }
            if (!addQuotation)
            {
                return settings.TablePrefix + attr[0].Name;
            }
            return AddQuotes(settings.TablePrefix + attr[0].Name, null, settings.TableNameQuoteCharacter);
        }

        internal static string GetColumnName(GXDBSettings settings, MemberInfo info)
        {
            DataMemberAttribute[] attr = (DataMemberAttribute[])info.GetCustomAttributes(typeof(DataMemberAttribute), true);
            string name;
            if (attr.Length == 0 || attr[0].Name == null)
            {
                name = info.Name;
            }
            else
            {
                name = attr[0].Name;
            }
            if (settings.IsReservedWord(name) ||
                AddQuetationAlways(settings))
            {
                return AddQuotes(name, null, settings.ColumnNameQuoteCharacter);
            }
            return name;
        }

        internal static string ConvertToString(GXGetMembersArgs args, object value)
        {
            string tableName = null;
            return ConvertToString(args.Settings, args.TargetType, tableName, value, null);
        }

        internal static string ConvertToString(GXDBSettings settings,
            TargetType targetType,
            string tableName,
            object value,
            Dictionary<string, string> maps)
        {
            bool plain = (targetType & TargetType.Plain) != 0;
            if ((targetType & TargetType.Value) != 0)
            {
                if (settings.UseEpochTimeFormat)
                {
                    if (value is DateTime dt)
                    {
                        value = (dt.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
                    }
                    else if (value is DateTimeOffset dto)
                    {
                        value = (dto.UtcDateTime - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
                    }
                }
                ConvertOption options = ConvertOption.Quete;
                if (plain)
                {
                    options = ConvertOption.None;
                }
                if ((targetType & TargetType.IgnoreMs) != 0)
                {
                    options |= ConvertOption.Seconds;
                }
                return settings.ConvertToString(value, options);
            }
            if ((targetType & TargetType.Column) != 0)
            {
                if (value is string cn)
                {
                    if (tableName == null)
                    {
                        if (settings == null)
                        {
                            return cn;
                        }
                        if (!plain &&
                            (settings.IsReservedWord(cn) ||
                            AddQuetationAlways(settings)))
                        {
                            return AddQuotes(cn, null, settings.ColumnNameQuoteCharacter);
                        }
                        return cn;
                    }
                    string name;
                    if (plain)
                    {
                        name = tableName + "." + cn;
                    }
                    else
                    {
                        if (maps != null)
                        {
                            if (settings != null && settings.TableNameQuoteCharacter != '\0' &&
                                tableName.StartsWith(settings.TableNameQuoteCharacter))
                            {
                                name = tableName.Substring(1, tableName.Length - 2) + "." + cn;
                            }
                            else
                            {
                                name = tableName + "." + cn;
                            }
                            if (maps.TryGetValue(name, out var mappedName))
                            {
                                name = tableName + "." + ConvertToString(settings, TargetType.Column, null, cn, maps);
                                return name + " AS " + AddQuotes(ConvertToString(settings, targetType | TargetType.Plain, null, mappedName, null), null, settings.ColumnNameQuoteCharacter); ;
                            }
                            name = tableName + "." + cn;
                        }
                        name = tableName + "." +
                            ConvertToString(settings, TargetType.Column, null, cn, maps);
                    }
                    return name;
                }
                else if (value is Type type)
                {
                    string name = null;
                    if (type.IsClass)
                    {
                        if (IsAliasName(type))
                        {
                            if (tableName != null)
                            {
                                name = OriginalTableName(type) + ".";
                            }
                            name += GetColumnName(settings, type);
                        }
                        else
                        {
                            if (tableName != null)
                            {
                                name = GetTableName(settings, type) + ".";
                            }
                            name += GetColumnName(settings, type);
                        }
                    }
                    else
                    {
                        name = GetColumnName(settings, type);
                    }
                    return name;
                }
                else if (value is MemberExpression memberExpression)
                {
                    if (memberExpression.Member.DeclaringType.IsClass)
                    {
                        return ConvertToString(settings, targetType, tableName, memberExpression.Member, maps);
                    }
                    return ConvertToString(settings, targetType, tableName, memberExpression.Member, maps);
                }
                else if (value is MemberInfo memberInfo)
                {
                    string name;
                    DataMemberAttribute[] attr = (DataMemberAttribute[])memberInfo.GetCustomAttributes(typeof(DataMemberAttribute), true);
                    if (!attr.Any() || attr[0].Name == null)
                    {
                        name = memberInfo.Name;
                    }
                    else
                    {
                        name = attr[0].Name;
                    }
                    return ConvertToString(settings, targetType, tableName, name, maps);
                }
                else if (value is GXSelectArgs args)
                {
                    return args.ToString(false);
                }
            }
            else if ((targetType & TargetType.Table) != 0)
            {
                if (value is string tn)
                {
                    if (settings == null)
                    {
                        return tn;
                    }
                    if (plain)
                    {
                        return settings.TablePrefix + tn;
                    }
                    if (settings.IsReservedWord(tn) || AddQuetationAlways(settings))
                    {
                        return AddQuotes(settings.TablePrefix + tn, null, settings.TableNameQuoteCharacter);
                    }
                    return settings.TablePrefix + tn;
                }
                if (value is Type tableType)
                {
                    if (settings != null && AddQuetationAlways(settings))
                    {
                        return GetTableName(settings, tableType, !plain);
                    }
                    return GetTableName(settings, tableType);
                }
            }
            throw new ArgumentOutOfRangeException();
        }


        internal static string[] GetMembers(GXGetMembersArgs args)
        {
            if (args.Expression == null)
            {
                throw new ArgumentException("The expression cannot be null.");
            }

            if (args.Expression is LambdaExpression)
            {
                LambdaExpression lambdaEx = args.Expression as LambdaExpression;
                args.Expression = lambdaEx.Body;
                return GetMembers(args);
            }

            if (args.Expression is MemberExpression memberExpression)
            {
                // Reference type property or field
                Expression e = memberExpression.Expression;
                if (memberExpression.Member.DeclaringType == typeof(GXSql) &&
                    memberExpression.Member.Name == nameof(GXSql.One))
                {
                    return ["1()"];
                }
                if (e == null)
                {
                    var member = Expression.Convert(memberExpression, typeof(object));
                    var lambda = Expression.Lambda<Func<object>>(member);
                    var getter = lambda.Compile();
                    object value = getter();
                    args.StringBuilder.Append(args.Settings.ConvertToString(value, ConvertOption.Quete));
                    return null;
                }
                //Get member name.
                if (e.NodeType == ExpressionType.Parameter)
                {
                    //If column name.
                    string tableName = null;
                    TargetType type = TargetType.Column;
                    if (!args.SingleTable && (args.TargetType & TargetType.Plain) == 0)
                    {
                        type = TargetType.Table;
                        if (args.StringBuilder == null)
                        {
                            type |= TargetType.Plain;
                        }
                        tableName = ConvertToString(args.Settings, type, null, e.Type, null);
                        type = TargetType.Column;
                    }
                    if (args.StringBuilder == null)
                    {
                        type |= TargetType.Plain;
                    }
                    string name = ConvertToString(args.Settings, type, tableName, memberExpression, null);
                    if (args.StringBuilder == null)
                    {
                        return [name];
                    }
                    args.StringBuilder.Append(name);
                    return null;
                }
                //Get property value.
                if (e.NodeType == ExpressionType.MemberAccess)
                {
                    var member = Expression.Convert(memberExpression, typeof(object));
                    var lambda = Expression.Lambda<Func<object>>(member);
                    var getter = lambda.Compile();
                    object value = getter();
                    if (value != null && value.GetType().IsEnum)
                    {
                        //Convert enum value to integer value.
                        if (!args.Settings.UseEnumStringValue)
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
                                sb.Append(args.ListSeparator);
                            }
                            sb.Append(ConvertToString(args, it));
                        }
                        return [sb.ToString()];
                    }
                    else if (value is PropertyInfo p)
                    {
                        string tableName = null;
                        if (!args.SingleTable)
                        {
                            tableName = ConvertToString(args.Settings, TargetType.Table, null, p.DeclaringType, null);
                        }
                        //In where get table type and column name.
                        if (p.DeclaringType.IsClass)
                        {
                            if (IsAliasName(p.DeclaringType))
                            {
                                return [ OriginalTableName(p.DeclaringType) +
                            "." + ConvertToString(args.Settings, TargetType.Column, tableName, value, null) ];
                            }
                        }
                        args.StringBuilder.Append(ConvertToString(args.Settings, TargetType.Column, tableName, value, null));
                        return null;
                    }
                    if (value != null && value.GetType().IsClass)
                    {
                        GXSerializedItem si = GXSqlBuilder.FindUnique(value.GetType());
                        if (si != null && si.Target is PropertyInfo p)
                        {
                            value = si.Target;
                            //In where get table type and column name.
                            if (args.TargetType != TargetType.Column && p.DeclaringType.IsClass)
                            {
                                if (IsAliasName(p.DeclaringType))
                                {
                                    return [OriginalTableName(p.DeclaringType) + "." + ConvertToString(args.Settings, TargetType.Column, null, value, null)];
                                }
                                return [ConvertToString(args.Settings, TargetType.Table, null, p.DeclaringType, null) + "." +
                                    ConvertToString(args.Settings, TargetType.Column, null, value, null)];
                            }
                            return [ConvertToString(args.Settings, TargetType.Column, null, value, null)];
                        }
                    }
                    args.StringBuilder.Append(ConvertToString(args.Settings, args.TargetType | TargetType.Value, null, value, null));
                    return null;
                }
                if (e.NodeType == ExpressionType.Call)
                {
                    return [ConvertToString(args, Expression.Lambda(memberExpression).Compile().DynamicInvoke())];
                }
                if (memberExpression.NodeType == ExpressionType.MemberAccess && e.NodeType == ExpressionType.Constant)
                {
                    bool first = true;
                    object value;
                    var target = Expression.Lambda(args.Expression).Compile().DynamicInvoke();
                    if (target == null)
                    {
                        return [null];
                    }
                    Dictionary<string, GXSerializedItem> properties;
                    if (target is string || target is GXSelectArgs || !target.GetType().IsClass)//String is class.
                    {
                        if (target is GXSelectArgs)
                        {
                            ((GXSelectArgs)target).Settings = args.Settings;
                        }
                        string tableName = null;
                        string tmp = ConvertToString(args.Settings, args.TargetType, tableName, target, null);
                        if (args.StringBuilder != null)
                        {
                            args.StringBuilder.Append(tmp);
                        }
                        return [tmp];
                    }
                    else if (target is IEnumerable t)
                    {
                        List<string> list = new List<string>();
                        properties = GXSqlBuilder.GetProperties(GXInternal.GetPropertyType(target.GetType()));
                        //If this is a basic type list. example int[].
                        if (properties.Count == 0)
                        {
                            foreach (object it in t)
                            {
                                if (args.StringBuilder == null)
                                {
                                    first = false;
                                    string tableName = null;
                                    list.Add(ConvertToString(args.Settings, args.TargetType | TargetType.Value, tableName, it, null));
                                }
                                else
                                {
                                    if (first)
                                    {
                                        first = false;
                                    }
                                    else
                                    {
                                        args.StringBuilder.Append(args.ListSeparator);
                                    }
                                    args.StringBuilder.Append(ConvertToString(args.Settings, (args.TargetType | TargetType.Value) & ~TargetType.Plain, null, it, null));
                                }
                            }
                            if (first)
                            {
                                throw new ArgumentOutOfRangeException("List is empty.");
                            }
                            return list.ToArray();
                        }
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
                            if (target is IEnumerable list)
                            {
                                if ((args.TargetType & TargetType.Plain) == 0)
                                {
                                    string tableName = null;
                                    args.StringBuilder.Append(ConvertToString(args.Settings, TargetType.Column, tableName, it.Value.Target, null));
                                    args.StringBuilder.Append(" IN(");
                                }
                                first = true;
                                foreach (var e2 in list)
                                {
                                    if (it.Value.Get != null)
                                    {
                                        value = it.Value.Get(e2);
                                    }
                                    else
                                    {
                                        value = GXInternal.GetValue(e2, it.Value);
                                    }
                                    if (first)
                                    {
                                        first = false;
                                    }
                                    else
                                    {
                                        args.StringBuilder.Append(args.ListSeparator);
                                    }
                                    args.StringBuilder.Append(ConvertToString(args.Settings, TargetType.Value, null, value, null));
                                }
                                if ((args.TargetType & TargetType.Plain) == 0)
                                {
                                    args.StringBuilder.Append(")");
                                }
                                return null;
                            }
                            if (it.Value.Get != null)
                            {
                                value = it.Value.Get(target);
                            }
                            else
                            {
                                value = GXInternal.GetValue(target, it.Value);
                            }
                            if (it.Value.Target is PropertyInfo pi)
                            {
                                string tableName = null;
                                if (args.TargetType != TargetType.Value)
                                {
                                    args.StringBuilder.Append(ConvertToString(args.Settings, TargetType.Column, tableName, pi, null));
                                    args.StringBuilder.Append(" = ");
                                }
                                args.StringBuilder.Append(ConvertToString(args.Settings, TargetType.Value, null, value, null));
                            }
                            else
                            {
                                throw new Exception("Primary key must be property.");
                            }
                            return null;
                        }
                    }
                    //If primary key is not used.
                    //If collection
                    if (target is IEnumerable)
                    {
                        Type itemType = GXInternal.GetPropertyType(target.GetType());
                        args.StringBuilder.Append('(');
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
                                args.StringBuilder.Append(" OR ");
                            }
                            args.StringBuilder.Append('(');
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
                                    args.StringBuilder.Append(" AND ");
                                }
                                if (value == null && args.TargetType == TargetType.Where)
                                {
                                    args.StringBuilder.Append(it.Key);
                                    args.StringBuilder.Append(" IS NULL ");
                                }
                                else
                                {
                                    args.StringBuilder.Append(it.Key);
                                    args.StringBuilder.Append(" = ");
                                    args.StringBuilder.Append(args.Settings.ConvertToString(value));
                                }
                            }
                            args.StringBuilder.Append(")");
                            first = true;
                        }
                        args.StringBuilder.Append(")");
                        return null;
                    }
                    args.StringBuilder.Append('(');
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
                            args.StringBuilder.Append(" AND ");
                        }
                        if (value == null && args.TargetType == TargetType.Where)
                        {
                            args.StringBuilder.Append(it.Key);
                            args.StringBuilder.Append(" IS NULL ");
                        }
                        else
                        {
                            if (args.Settings.UseQuotationWhereColumns)
                            {
                                args.StringBuilder.Append(AddQuotes(it.Key,
                                    null,
                                    args.Settings.ColumnNameQuoteCharacter));
                            }
                            else
                            {
                                args.StringBuilder.Append(it.Key);
                            }
                            args.StringBuilder.Append(" = ");
                            args.StringBuilder.Append(args.Settings.ConvertToString(value));
                        }
                    }
                    args.StringBuilder.Append(')');
                    return null;
                }
                throw new Exception("Invalid expression.");
            }
            if (args.Expression is MethodCallExpression)
            {
                // Reference type method
                var methodCallExpression = (MethodCallExpression)args.Expression;
                if (methodCallExpression.Arguments.Count != 0 &&
                    (methodCallExpression.Arguments[0].NodeType == ExpressionType.MemberAccess ||
                    methodCallExpression.Arguments[0].NodeType == ExpressionType.Constant ||
                    methodCallExpression.Arguments[0].NodeType == ExpressionType.NewArrayInit ||
                    methodCallExpression.Arguments[0].NodeType == ExpressionType.Convert))
                {
                    args.MethodCallExpression = methodCallExpression;
                    return HandleMethod(args);
                }
                object value = Expression.Lambda(methodCallExpression).Compile().DynamicInvoke();
                args.StringBuilder.Append(ConvertToString(args, value));
                return null;
            }

            if (args.Expression is UnaryExpression unaryExpression)
            {
                // Property, field of method returning value type
                args.MethodCallExpression = unaryExpression.Operand as MethodCallExpression;
                if (args.MethodCallExpression != null)
                {
                    args.UnaryExpression = unaryExpression;
                    return HandleMethod(args, args.TargetType | TargetType.Column);
                }
                args.Expression = unaryExpression.Operand;
                return GetMembers(args, args.TargetType);
            }

            if (args.Expression is NewExpression)
            {
                // Property, field of method returning value type
                var newExpression = (NewExpression)args.Expression;
                List<string> list = new List<string>();
                bool first = true;
                foreach (var it in newExpression.Arguments)
                {
                    args.Expression = it;
                    if (args.StringBuilder == null)
                    {
                        list.AddRange(GetMembers(args));
                    }
                    else
                    {
                        if (first)
                        {
                            first = false;
                        }
                        else
                        {
                            args.StringBuilder.Append(args.ListSeparator);
                        }
                        GetMembers(args);
                    }
                }
                return list.ToArray();
            }

            if (args.Expression is NewArrayExpression ne)
            {
                List<string> list = new List<string>();
                bool empty = args.StringBuilder == null;
                // Property, field of method returning value type
                bool first = true;
                foreach (var it in ne.Expressions)
                {
                    args.Expression = it;
                    if (args.StringBuilder != null)
                    {
                        if (first)
                        {
                            first = false;
                        }
                        else
                        {
                            args.StringBuilder.Append(args.ListSeparator);
                        }
                        GetMembers(args);
                    }
                    else
                    {
                        list.AddRange(GetMembers(args));
                    }

                }
                if (empty)
                {
                    return list.ToArray();
                }
                return null;
            }

            if (args.Expression is BinaryExpression bi)
            {
                // Property, field of method returning value type
                string op;
                switch (args.Expression.NodeType)
                {
                    case ExpressionType.Add:
                    case ExpressionType.AddChecked:
                        op = " + ";
                        break;
                    case ExpressionType.And:
                        op = " & ";
                        break;
                    case ExpressionType.AndAlso:
                        if (!args.SingleTable)
                        {
                            args.StringBuilder.Append("(");
                        }
                        args.Expression = bi.Left;
                        GetMembers(args);
                        args.StringBuilder.Append(" AND ");
                        args.Expression = bi.Right;
                        GetMembers(args);
                        if (!args.SingleTable)
                        {
                            args.StringBuilder.Append(")");
                        }
                        return null;
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
                        args.StringBuilder.Append("POWER(");
                        args.Expression = bi.Left;
                        GetMembers(args);
                        args.StringBuilder.Append(args.ListSeparator);
                        args.Expression = bi.Right;
                        GetMembers(args);
                        args.StringBuilder.Append(")");
                        return null;
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
                string tmp = null;
                UnaryExpression u = bi.Left as UnaryExpression;
                bool isenum = u != null && u.Operand.Type.IsEnum;
                var nodeType = args.Expression.NodeType;
                if (isenum)
                {
                    args.Expression = bi.Right;
                    tmp = GetMemberStringValue(args);
                    if (args.Settings.UseEnumStringValue)
                    {
                        tmp = Enum.Parse(u.Operand.Type, tmp, true).ToString();
                        tmp = AddQuotes(tmp, null, '\'');
                    }
                }
                else
                {
                    args.Expression = bi.Right;
                    tmp = GetMemberStringValue(args);
                }
                //Where string is empty is not working with oracle DB. We must use where string is null expression.
                if (args.TargetType == TargetType.Where && (tmp == null || tmp == "NULL"))
                {
                    if (nodeType == ExpressionType.NotEqual)
                    {
                        op = " IS NOT NULL";
                    }
                    else if (nodeType == ExpressionType.Equal)
                    {
                        op = " IS NULL";
                    }
                    else
                    {
                        throw new ArgumentException("Argument is null.");
                    }
                    tmp = null;
                }
                args.Expression = bi.Left;
                args.TargetType = TargetType.Column;
                var list = GetMembers(args);
                if (args.StringBuilder == null)
                {
                    return ["(" + list[0] + op + tmp + ")"];
                }
                args.StringBuilder.Append(op);
                args.StringBuilder.Append(tmp);
                return null;
            }

            if (args.Expression is ConstantExpression ce)
            {
                if (ce.Value is string str && str == "*")
                {
                    if (args.StringBuilder == null)
                    {
                        return [str];
                    }
                    args.StringBuilder.Append("*");
                    return null;
                }
                string tableName = null;
                args.StringBuilder.Append(ConvertToString(args.Settings, args.TargetType | TargetType.Value, tableName, ce.Value, null));
                return null;
            }
            if (args.Expression is ParameterExpression pe)
            {
                string tableName = null;
                TargetType type = TargetType.Column;
                if (args.StringBuilder == null)
                {
                    type |= TargetType.Plain;
                }
                string name = ConvertToString(args.Settings, type, tableName, pe.Name, null);
                if (args.StringBuilder == null)
                {
                    return [name];
                }
                args.StringBuilder.Append(name);
                return null;
            }
            throw new ArgumentException("Invalid expression");
        }

        /// <summary>
        /// Determines whether the specified expression is null or represents an empty value.
        /// </summary>
        /// <typeparam name="T">The type of the parameter used in the expression.</typeparam>
        /// <param name="where">The expression to evaluate for null or empty value.</param>
        /// <returns>true if the expression is null or empty; otherwise, false.</returns>
        public static bool IsNullOrEmptyWhereExpression<T>(Expression<Func<T, object>> where)
        {
            if (where == null)
            {
                return true;
            }

            Expression body = where.Body;
            while (body is UnaryExpression unaryExpression &&
                (body.NodeType == ExpressionType.Convert || body.NodeType == ExpressionType.ConvertChecked))
            {
                body = unaryExpression.Operand;
            }

            if (body is ConstantExpression constantExpression)
            {
                if (constantExpression.Value == null)
                {
                    return true;
                }
                if (constantExpression.Value is string value && string.IsNullOrWhiteSpace(value))
                {
                    return true;
                }
            }

            if (body is NewExpression newExpression && newExpression.Arguments.Count == 0)
            {
                return true;
            }

            if (body is NewArrayExpression newArrayExpression && newArrayExpression.Expressions.Count == 0)
            {
                return true;
            }

            return false;
        }

        public static string GetDatabaseName(DatabaseType type, string databaseName)
        {
            if (string.IsNullOrEmpty(databaseName))
            {
                return databaseName;
            }
            if (type == DatabaseType.PostgreSQL)
            {
                if (databaseName.StartsWith("\"") && databaseName.EndsWith("\""))
                {
                    return databaseName;
                }
                return databaseName.ToLower();
            }
            if (type == DatabaseType.Oracle ||
                type == DatabaseType.DB2 ||
                type == DatabaseType.SapHana)
            {
                if (databaseName.StartsWith("\"") && databaseName.EndsWith("\""))
                {
                    return databaseName;
                }
                return databaseName.ToUpper();
            }
            return databaseName;
        }

    }
}
