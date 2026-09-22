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
using Gurux.Service.Orm.Common.Enums;
using Gurux.Service.Orm.Common.Model;
using Gurux.Service.Orm.Enums;
using Gurux.Service.Orm.Settings;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
        internal Expression? Expression;
        internal TargetType TargetType;

        internal string? Post;
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
        /// <summary>
        /// Generated SQL query is stored in this string builder.
        /// </summary>
        internal StringBuilder? StringBuilder;

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
        /// <param name="value">The text to quote and escape.</param>
        /// <param name="dataQuote">Replacement text used to escape embedded quote characters.</param>
        /// <param name="quoteSeparator">The quote character surrounding the value.</param>
        /// <returns>The value enclosed in the requested quote characters, or unchanged when no quote character is configured.</returns>
        internal static string AddQuotes(string value,
            string? dataQuote,
            char quoteSeparator)
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
            return type.GetCustomAttributes(typeof(AliasAttribute), true).Any();
        }

        internal static bool IsSharedTable(Type type)
        {
            return type.BaseType != typeof(object) && type.BaseType.GetCustomAttributes(typeof(DataContractAttribute), true).Length != 0;
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

        internal static Type GetType(Expression expression)
        {
            //Get type.
            if (expression is LambdaExpression lambdaEx)
            {
                return GetType(lambdaEx.Body);
            }
            if (expression is ConstantExpression c)
            {
                return (Type)c.Value!;
            }
            if (expression is MemberExpression m)
            {
                return m.Expression!.Type;
            }
            if (expression is NewExpression ne)
            {
                return GetType(ne.Arguments[0]);
            }
            throw new NotImplementedException();
        }

        internal static string[]? GetMemberList(GXGetMembersArgs args)
        {
            var type = args.TargetType;
            var old = args.StringBuilder;
            args.StringBuilder = null;
            args.TargetType |= TargetType.Plain;
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
                    GetMembers(args);
                    args.StringBuilder.Append(" LIKE('%");
                    args.Expression = args.MethodCallExpression.Arguments[1];
                    args.TargetType |= TargetType.Plain;
                    GetMembers(args);
                    args.StringBuilder.Append("%')");
                }
                if (args.MethodCallExpression.Method.Name == "IsEmpty")
                {
                    args.Post = ") THEN 1 ELSE 0 END AS IsEmpty";
                    if (args.Settings.Type == DatabaseType.Oracle)
                    {
                        args.Post += " FROM DUAL";
                    }
                    else if (args.Settings.Type == DatabaseType.SapHana)
                    {
                        args.Post += " FROM DUMMY";
                    }
                    else if (args.Settings.Type == DatabaseType.DB2)
                    {
                        args.Post += " FROM SYSIBM.SYSDUMMY1";
                    }
                    return ["CASE WHEN NOT EXISTS (SELECT 1"];
                }
                if (args.MethodCallExpression.Method.Name == "Greater")
                {
                    args.Expression = args.MethodCallExpression.Arguments[0];
                    GetMembers(args);
                    args.StringBuilder.Append(" > ");
                    args.Expression = args.MethodCallExpression.Arguments[1];
                    GetMembers(args);
                }
                if (args.MethodCallExpression.Method.Name == "Less")
                {
                    args.Expression = args.MethodCallExpression.Arguments[0];
                    GetMembers(args);
                    args.StringBuilder.Append(" < ");
                    args.Expression = args.MethodCallExpression.Arguments[1];
                    GetMembers(args);
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
                    GetMembers(args);
                    args.StringBuilder.Append(" IS NULL");
                }
                if (args.MethodCallExpression.Method.Name == "NotNull")
                {
                    args.Expression = args.MethodCallExpression.Arguments[1];
                    GetMembers(args);
                    args.StringBuilder.Append(" IS NOT NULL");
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
                    args.StringBuilder.Append(value);
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
            if (value22 is GXColumnSchema cs)
            {
                string? name = args.Settings.ConvertToString(cs.Name, ConvertOption.None);
                if (args.StringBuilder != null)
                {
                    args.StringBuilder.Append(name);
                }
                return [name];
            }
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

        internal static string GetTableName(GXDBSettings settings, Type type, bool plain)
        {
            if (type.BaseType != typeof(object) && type.BaseType.GetCustomAttributes(typeof(DataContractAttribute), true).Any())
            {
                return GetTableName(settings, type.BaseType, plain);
            }
            DataContractAttribute[] attr = (DataContractAttribute[])type.GetCustomAttributes(typeof(DataContractAttribute), true);
            if (!attr.Any() || attr[0].Name == null)
            {
                if (plain)
                {
                    return settings.TablePrefix + type.Name;
                }
                return settings.EscapeIdentifier(settings.TablePrefix, type.Name);
            }
            if (plain)
            {
                return settings?.TablePrefix + attr[0].Name;
            }
            return settings.EscapeIdentifier(settings.TablePrefix, attr[0].Name);
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
                name = attr[0].Name!;
            }
            return settings.EscapeIdentifier(null, name);
        }

        internal static string ConvertToString(GXGetMembersArgs args, object value)
        {
            return ConvertToString(args.Settings, args.TargetType, null, value, null);
        }

        internal static string ConvertToString(GXDBSettings settings,
            TargetType targetType,
            string? tableName,
            object? value,
            Dictionary<string, string>? maps)
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
                        if (!plain)
                        {
                            return settings.EscapeIdentifier(null, cn);
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
                                name = GetTableName(settings, type, true) + ".";
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
                    return settings.EscapeIdentifier(settings.TablePrefix, tn);
                }
                if (value is Type tableType)
                {
                    return GetTableName(settings, tableType, plain);
                }
            }
            throw new ArgumentOutOfRangeException();
        }

        /// <summary>
        /// Get excluded properties for a given type based on the provided list of excluded expressions.
        /// </summary>
        /// <param name="Excluded">Excluded member expressions grouped by entity type.</param>
        /// <param name="args">Options used to resolve members from the expressions.</param>
        /// <param name="type">The mapped CLR type.</param>
        /// <returns>The names of excluded members for the requested entity type.</returns>
        static internal List<string> ExcludedProperties(List<KeyValuePair<Type, LambdaExpression>> Excluded,
            GXGetMembersArgs args, Type type)
        {
            var expression = args.Expression;
            List<string> list = new List<string>();
            foreach (KeyValuePair<Type, LambdaExpression> it in Excluded)
            {
                if (it.Key == type)
                {
                    args.Expression = it.Value;
                    string[]? removed = GetMemberList(args);
                    if (removed != null)
                    {
                        list.AddRange(removed);
                    }
                }
            }
            args.Expression = expression;
            return list;
        }

        /// <summary>
        /// Returns the names of properties whose values are null for all items
        /// in the collection.
        /// </summary>
        /// <param name="type">The type of items in the collection.</param>
        /// <param name="items">The collection to inspect.</param>
        /// <returns>
        /// Property names whose values are null for every item in the collection.
        /// </returns>
        public static List<string> GetAlwaysNullProperties(Type type, IEnumerable<object> items)
        {
            if (type == typeof(GXSelectArgs))
            {
                return [];
            }
            ArgumentNullException.ThrowIfNull(items);

            if (!items.Any())
            {
                return [];
            }
            var list = items.Where(item => !(item is GXSelectArgs)).ToArray();
            return type
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead)
                .Where(p => list.All(item => p.GetValue(item) == null))
                .Select(p => p.Name)
                .ToList();
        }

        internal static string[]? GetMembers(GXGetMembersArgs args)
        {
            if (args.Expression == null)
            {
                throw new ArgumentException("The expression cannot be null.");
            }

            if (args.Expression is LambdaExpression lambdaEx)
            {
                args.Expression = lambdaEx.Body;
                return GetMembers(args);
            }

            if (args.Expression is MemberExpression memberExpression)
            {
                // Reference type property or field
                Expression e = memberExpression.Expression!;
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
                        string name = ConvertToString(args.Settings, TargetType.Column, tableName, value, null);
                        if (args.StringBuilder != null)
                        {
                            args.StringBuilder.Append(name);
                            return null;
                        }
                        return [name];
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
                    if (target is GXColumnSchema cs)
                    {
                        string? name = args.Settings.ConvertToString(cs.Name, ConvertOption.None);
                        args.StringBuilder?.Append(name);
                        return [name!];
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
                        if (!properties.Any())
                        {
                            foreach (object it in t)
                            {
                                if (args.StringBuilder == null)
                                {
                                    first = false;
                                    list.Add(ConvertToString(args.Settings, args.TargetType | TargetType.Value, null, it, null));
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
                            if (first && args.StringBuilder != null)
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
                                    value = it.Value.Get(e2);
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
                            value = it.Value.Get(target);
                            if (!((it.Value.Attributes & Attributes.Id) != 0 &&
                                IsZero(value)))
                            {
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
                            }
                            else if (args.StringBuilder?.Length == 1)
                            {
                                //Remove ) from the string.
                                --args.StringBuilder.Length;
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
                                value = it.Value.Get(e2.Current);
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
                        value = it.Value.Get(target);
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
                var old = args.TargetType;
                args.TargetType = TargetType.Column;
                var list = GetMembers(args);
                args.TargetType = old;
                if (args.StringBuilder == null && list != null)
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
                string tmp = ConvertToString(args.Settings, args.TargetType | TargetType.Value, null, ce.Value, null);
                if (args.StringBuilder == null)
                {
                    return [tmp];
                }
                args.StringBuilder.Append(tmp);
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

        /// <summary>
        /// Check is value is zero. 
        /// This method is used to check if a numeric value is zero, regardless of its type (e.g., int, float, double, etc.). It returns true if the value is zero, and false otherwise.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <returns>true if the value is zero; otherwise, false.</returns>
        public static bool IsZero(object? value)
        {
            return value switch
            {
                byte v => v == 0,
                sbyte v => v == 0,
                short v => v == 0,
                ushort v => v == 0,
                int v => v == 0,
                uint v => v == 0,
                long v => v == 0,
                ulong v => v == 0,
                float v => v == 0,
                double v => v == 0,
                decimal v => v == 0,
                Guid v => v == Guid.Empty,
                string v => v == string.Empty,
                null => true,
                _ => false
            };
        }

        public static Type GetType(string typeName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(typeName);

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type? type = FindType(assembly, typeName);
                if (type != null)
                {
                    return type;
                }
                foreach (AssemblyName satelliteName in assembly.GetReferencedAssemblies())
                {
                    try
                    {
                        Assembly satellite = Assembly.Load(satelliteName);
                        type = FindType(satellite, typeName);
                        if (type != null)
                        {
                            return type;
                        }
                    }
                    catch (FileNotFoundException)
                    {
                        // Referenced assembly is not available.
                    }
                    catch (FileLoadException)
                    {
                        // Assembly could not be loaded.
                    }
                }
            }

            throw new TypeLoadException($"Type '{typeName}' was not found.");
        }

        public static object CreateInstance(string typeName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(typeName);

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type? type = FindType(assembly, typeName);
                if (type != null)
                {
                    return Activator.CreateInstance(type)
                        ?? throw new InvalidOperationException(
                            $"Failed to create an instance of type '{type.FullName}'.");
                }

                foreach (AssemblyName satelliteName in assembly.GetReferencedAssemblies())
                {
                    try
                    {
                        Assembly satellite = Assembly.Load(satelliteName);

                        type = FindType(satellite, typeName);
                        if (type != null)
                        {
                            return Activator.CreateInstance(type)
                                ?? throw new InvalidOperationException(
                                    $"Failed to create an instance of type '{type.FullName}'.");
                        }
                    }
                    catch (FileNotFoundException)
                    {
                        // Referenced assembly is not available.
                    }
                    catch (FileLoadException)
                    {
                        // Assembly could not be loaded.
                    }
                }
            }

            throw new TypeLoadException($"Type '{typeName}' was not found.");
        }

        private static Type? FindType(Assembly assembly, string typeName)
        {
            // Fast path if full type name was supplied.
            Type? type = assembly.GetType(typeName, throwOnError: false, ignoreCase: false);
            if (type != null)
            {
                return type;
            }

            try
            {
                return assembly.GetTypes().FirstOrDefault(
                    t => string.Equals(t.Name, typeName, StringComparison.Ordinal) ||
                         string.Equals(t.FullName, typeName, StringComparison.Ordinal));
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types
                    .Where(t => t != null)
                    .FirstOrDefault(
                        t => string.Equals(t!.Name, typeName, StringComparison.Ordinal) ||
                             string.Equals(t.FullName, typeName, StringComparison.Ordinal));
            }
        }

        /// <summary>Adds an integer offset to a supported numeric value while preserving its numeric type.</summary>
        /// <param name="value">The numeric value to increment.</param>
        /// <param name="add">The integer offset to add.</param>
        /// <returns>The incremented value boxed as its numeric type.</returns>
        /// <exception cref="ArgumentException">The value is null or is not a supported numeric type.</exception>
        public static object Add(object? value, int add)
        {
            return value switch
            {
                byte v => (byte)(v + add),
                sbyte v => (sbyte)(v + add),
                short v => (short)(v + add),
                ushort v => (ushort)(v + add),
                int v => v + add,
                uint v => v + (uint)add,
                long v => v + add,
                ulong v => v + (ulong)add,
                float v => v + add,
                double v => v + add,
                decimal v => v + add,
                _ => throw new ArgumentException("Value must be a numeric type.", nameof(value))
            };
        }
    }
}
