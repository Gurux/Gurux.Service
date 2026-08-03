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
using Gurux.Service.Orm.Enums;
using Gurux.Service.Orm.Internal;
using Gurux.Service.Orm.Model;
using Gurux.Service.Orm.Settings;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Runtime.Serialization;

namespace Gurux.Service.Orm
{
    /// <summary>
    /// This class is used to make SQL query.
    /// </summary>
    internal class GXSqlBuilder
    {
        /// <summary>
        /// Mapping between C# and DB types.
        /// </summary>
        internal Dictionary<Type, string> DbTypeMap = new Dictionary<Type, string>();
        static Dictionary<Type, GXRelationTable> relationTable = new Dictionary<Type, GXRelationTable>();

        private string GetType(string value)
        {
            int pos = value.IndexOf('(');
            if (pos != -1)
            {
                int end = value.IndexOf(')', pos);
                if (end != -1)
                {
                    return value.Remove(pos, end - pos + 1);
                }
            }
            return value;
        }

        /// <summary>
        /// Get C# data type from SB data type.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="len">Column length.</param>
        /// <returns></returns>
        internal Type GetDataType(string type, int len)
        {
            if (len == -1 || len == 65535)
            {
                if (string.Compare(Settings.StringColumnDefinition(0), type, true) == 0)
                {
                    return typeof(string);
                }
                if (string.Compare(Settings.StringColumnDefinition(len), type + "(" + len.ToString() + ")", true) == 0)
                {
                    return typeof(string);
                }
                if (string.Compare(Settings.GuidColumnDefinition, type + "(" + len.ToString() + ")", true) == 0)
                {
                    return typeof(Guid);
                }
            }
            string type2 = null;
            if (len != 0)
            {
                type2 = type + "(" + len.ToString() + ")";
            }
            if (string.Equals(type, "character varying",
                StringComparison.OrdinalIgnoreCase))
            {
                return typeof(string);
            }
            if (string.Compare(Settings.StringColumnDefinition(len),
                len == 0 ? type : type2, true) == 0)
            {
                return typeof(string);
            }
            if (string.Compare(Settings.ByteArrayColumnDefinition(len),
                len == 0 ? type : type2, true) == 0)
            {
                if ((Settings.Type == DatabaseType.Oracle || Settings.Type == DatabaseType.SapHana) &&
                    len == 16)
                {
                    //Oracle and SAP HANA use RAW/VARBINARY for Guid data.
                    return typeof(Guid);
                }
                return typeof(byte[]);
            }
            if (string.Equals(type, "real", StringComparison.OrdinalIgnoreCase))
            {
                return typeof(float);
            }
            if (string.Equals(type, "numeric", StringComparison.OrdinalIgnoreCase))
            {
                if (Settings.Type == DatabaseType.PostgreSQL && len == 20)
                {
                    return typeof(UInt64);
                }
                return typeof(decimal);
            }
            if (Settings.Type == DatabaseType.PostgreSQL &&
                string.Equals(type, "smallint", StringComparison.OrdinalIgnoreCase))
            {
                //PostgreSQL uses smallint for sbyte, byte and short.
                return typeof(short);
            }

            if (Settings.Type == DatabaseType.PostgreSQL)
            {
                if (string.Equals(type, "smallint", StringComparison.OrdinalIgnoreCase))
                {
                    //PostgreSQL uses smallint for sbyte, byte and short.
                    return typeof(Int16);
                }
                if (string.Equals(type, "integer", StringComparison.OrdinalIgnoreCase))
                {
                    //PostgreSQL uses smallint for sbyte, byte and short.
                    return typeof(Int32);
                }
                if (string.Equals(type, "bigint", StringComparison.OrdinalIgnoreCase))
                {
                    //PostgreSQL uses smallint for sbyte, byte and short.
                    return typeof(Int64);
                }
            }
            else if (Settings.Type == DatabaseType.Oracle)
            {
                if (string.Equals(type, "RAW", StringComparison.OrdinalIgnoreCase) && len != 16)
                {
                    //Oracle uses RAW for binary data.
                    return typeof(byte[]);
                }
                if (type.StartsWith("INTERVAL DAY", StringComparison.OrdinalIgnoreCase) &&
                    type.Contains("TO SECOND", StringComparison.OrdinalIgnoreCase))
                {
                    return typeof(TimeSpan);
                }

                if (string.Equals(type, "NUMBER", StringComparison.OrdinalIgnoreCase))
                {
                    if (len == 1)
                    {
                        return typeof(bool);
                    }
                    //PostgreSQL uses smallint for sbyte, byte and short.
                    if (len == 3)
                    {
                        return typeof(byte);
                    }
                    if (len == 5)
                    {
                        return typeof(short);
                    }
                    if (len == 19)
                    {
                        return typeof(long);
                    }
                    if (len == 20)
                    {
                        return typeof(UInt64);
                    }
                    return typeof(Int32);
                }
            }
            else if (Settings.Type == DatabaseType.SqLite)
            {
                if (string.Equals(type, "integer", StringComparison.OrdinalIgnoreCase))
                {
                    //SQLite uses smallint for sbyte, byte and short.
                    return typeof(Int32);
                }
                if (string.Equals(type, "BIGINT", StringComparison.OrdinalIgnoreCase))
                {
                    //SQLite uses bigint for UInt32, Int64 and UInt64.
                    return typeof(UInt64);
                }
            }
            else if (Settings.Type == DatabaseType.SapHana)
            {
                if (string.Equals(type, "SMALLINT", StringComparison.OrdinalIgnoreCase))
                {
                    //SAP HANA uses SMALLINT for sbyte and Int16.
                    return typeof(Int16);
                }
                if (string.Equals(type, "INTEGER", StringComparison.OrdinalIgnoreCase))
                {
                    //SAP HANA uses INTEGER for Int32.
                    return typeof(Int32);
                }
                if (string.Equals(type, "BIGINT", StringComparison.OrdinalIgnoreCase))
                {
                    if (len == 19)
                    {
                        return typeof(long);
                    }
                    //SAP HANA uses BIGINT for Int64.
                    return typeof(UInt64);
                }
                if (type.StartsWith("DECIMAL", StringComparison.OrdinalIgnoreCase))
                {
                    if (len == 20)
                    {
                        return typeof(UInt64);
                    }
                    return typeof(decimal);
                }
            }
            foreach (var it in DbTypeMap)
            {
                if (string.Compare(it.Value, type, true) == 0 ||
                    string.Compare(it.Value, type2, true) == 0 ||
                    (len == 0 &&
                    string.Compare(GetType(it.Value), type, true) == 0))
                {
                    return it.Key;
                }
            }
            if (string.Compare(type, GetType(Settings.BoolColumnDefinition), true) == 0)
            {
                return typeof(bool);
            }
            return null;
        }

        static private readonly Dictionary<Type, Dictionary<string, GXSerializedItem>> SerializedObjects = new Dictionary<Type, Dictionary<string, GXSerializedItem>>();

        /// <summary>
        /// DB settings.
        /// </summary>
        public GXDBSettings Settings
        {
            get;
            private set;
        }

        internal string GetDataBaseType(Type type, object target)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                type = Nullable.GetUnderlyingType(type);
            }
            //100 character is allocated for type.
            if (type == typeof(Type))
            {
                return Settings.StringColumnDefinition(100);
            }
            if (type.IsEnum)
            {
                if (Settings.UseEnumStringValue)
                {
                    StringLengthAttribute[] attr = (StringLengthAttribute[])type.GetCustomAttributes(typeof(StringLengthAttribute), true);
                    if (attr.Length == 0)
                    {
                        return Settings.StringColumnDefinition(100);
                    }
                    return Settings.StringColumnDefinition(attr[0].MaximumLength);
                }
                return GetDataBaseType(Enum.GetUnderlyingType(type), null);
            }
            if (type == typeof(string) || type == typeof(System.String) ||
                type == typeof(char[]) || type == typeof(object))
            {
                if (target is PropertyInfo)
                {
                    {
                        MaxLengthAttribute[] attr = (MaxLengthAttribute[])(target as PropertyInfo).GetCustomAttributes(typeof(MaxLengthAttribute), true);
                        if (attr.Length != 0)
                        {
                            return Settings.StringColumnDefinition(attr[0].Length);
                        }
                    }
                    {
                        StringLengthAttribute[] attr = (StringLengthAttribute[])(target as PropertyInfo).GetCustomAttributes(typeof(StringLengthAttribute), true);
                        if (attr.Length == 0)
                        {
                            return Settings.StringColumnDefinition(0);
                        }
                        return Settings.StringColumnDefinition(attr[0].MaximumLength);
                    }
                }
                return Settings.StringColumnDefinition(0);
            }
            if (type == typeof(byte[]))
            {
                if (target is PropertyInfo)
                {
                    MaxLengthAttribute[] attr = (MaxLengthAttribute[])(target as PropertyInfo).GetCustomAttributes(typeof(MaxLengthAttribute), true);
                    if (attr.Length != 0)
                    {
                        return Settings.ByteArrayColumnDefinition(attr[0].Length);
                    }
                    return Settings.ByteArrayColumnDefinition(0);
                }
            }
            if (type.IsArray && type != typeof(byte[]) && type != typeof(char[]))
            {
                return GetDataBaseType(GXInternal.GetPropertyType(type), null);
            }
            if (!DbTypeMap.ContainsKey(type))
            {
                if (typeof(IEnumerable).IsAssignableFrom(type) ||
                    type.IsClass)
                {
                    type = GXInternal.GetPropertyType(type);
                    foreach (var i in type.GetInterfaces())
                    {
                        if (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IUnique<>))
                        {
                            type = i.GenericTypeArguments[0];
                            return GetDataBaseType(type, null);
                        }
                    }
                }
                else if (GXInternal.IsGenericDataType(type))
                {
                    throw new Exception("Invalid data type: " + type.Name);
                }
                else
                {
                    throw new Exception("Invalid data type: " + type.Name + ". Make sure that you have added ForeignKey attribute to the property.");
                }
            }
            else if (type == typeof(DateTime) || type == typeof(DateOnly) ||
                type == typeof(DateTimeOffset) || type == typeof(TimeOnly))
            {
                if (target is PropertyInfo)
                {
                    TimeStorageUnitAttribute[] attr = (TimeStorageUnitAttribute[])(target as PropertyInfo).GetCustomAttributes(typeof(TimeStorageUnitAttribute), true);
                    if (attr.Length == 1)
                    {
                        if (type == typeof(DateTimeOffset))
                        {
                            return Settings.DateTimeOffsetColumnDefinition(attr[0].Unit);
                        }
                        return Settings.DateTimeColumnDefinition(attr[0].Unit);
                    }
                }
            }
            return DbTypeMap[type];
        }

        /// <summary>
        /// Returns table names in the current database.
        /// </summary>
        /// <param name="connection">Database connection.</param>
        /// <param name="transaction">Transaction.</param>
        /// <param name="databaseName">Database name.</param>
        /// <returns>Database table names.</returns>
        internal string[] GetTables(DbConnection connection, IDbTransaction transaction, string databaseName)
        {
            string query = Settings.GetTables(databaseName);
            return ((List<string>)SelectInternal<string>(connection, transaction, query)).ToArray();
        }

        /// <summary>
        /// Get tables to create.
        /// </summary>
        /// <param name="type">Parent table type</param>
        /// <param name="tables">Collection of tables to create.</param>
        internal static void GetTables(Type type, Dictionary<Type, GXSerializedItem> tables)
        {
            if (!tables.ContainsKey(type))
            {
                foreach (var it in GXSqlBuilder.GetProperties(type))
                {
                    if (it.Value.Relation != null)
                    {
                        //Add primary table.
                        if (!tables.ContainsKey(type))
                        {
                            tables.Add(type, it.Value);
                        }
                        //Add 1:n and n:n relation tables.
                        if (typeof(System.Collections.IEnumerable).IsAssignableFrom(it.Value.Type))
                        {
                            GetTables(GXInternal.GetPropertyType(it.Value.Type), tables);
                        }
                        else if (!GXInternal.IsGenericDataType(it.Value.Type))
                        {
                            GetTables(it.Value.Type, tables);
                        }
                        //Add 1:1 relation table.
                        if (!tables.ContainsKey(it.Value.Relation.ForeignTable))
                        {
                            GetTables(it.Value.Relation.ForeignTable, tables);
                            tables.TryAdd(it.Value.Relation.ForeignTable, it.Value.Relation.ForeignId);
                        }
                        //Add relation map table.
                        if (it.Value.Relation.RelationMapTable != null)
                        {
                            tables.TryAdd(it.Value.Relation.RelationMapTable.Relation.PrimaryTable, it.Value.Relation.RelationMapTable.Relation.PrimaryId);
                        }
                    }
                }
            }
        }

        internal static GXDBSettings CreateSettings(DatabaseType type)
        {
            GXDBSettings settings;
            switch (type)
            {
                case DatabaseType.MySQL:
                    settings = new GXMySqlSettings();
                    break;
                case DatabaseType.MSSQL:
                    settings = new GXMSSqlSettings();
                    break;
                case DatabaseType.SqLite:
                    settings = new GXSqLiteSettings();
                    break;
                case DatabaseType.Oracle:
                    settings = new GXOracleSqlSettings();
                    break;
                case DatabaseType.PostgreSQL:
                    settings = new GXPostgreSqlSettings();
                    break;
                case DatabaseType.MariaDB:
                    settings = new GXMariaDBSettings();
                    break;
                case DatabaseType.DB2:
                    settings = new GXDB2Settings();
                    break;
                case DatabaseType.SapHana:
                    settings = new GXSapHanaSqlSettings();
                    break;
                default:
                    throw new ArgumentOutOfRangeException("Invalid Database type.");
            }
            return settings;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="tablePrefix">Used table prefix (optional).</param>
        public GXSqlBuilder(DbConnection connection, string tablePrefix)
        {
            string name = connection.GetType().Name;
            string version = connection.ServerVersion;
            DatabaseType type;
            if (string.Compare(name, "SQLiteConnection", true) == 0)
            {
                type = DatabaseType.SqLite;
            }
            else if (name == "MySqlConnection")
            {
                if (version.Contains("MariaDB"))
                {
                    type = DatabaseType.MariaDB;
                }
                else
                {
                    type = DatabaseType.MySQL;
                }
            }
            else if (name == "SqlConnection")
            {
                type = DatabaseType.MSSQL;
            }
            else if (name == "OracleConnection")
            {
                type = DatabaseType.Oracle;
            }
            else if (name == "NpgsqlConnection")
            {
                type = DatabaseType.PostgreSQL;
            }
            else if (name == "DB2Connection")
            {
                type = DatabaseType.DB2;
            }
            else if (name == "HanaConnection")
            {
                type = DatabaseType.SapHana;
            }
            else
            {
                throw new ArgumentOutOfRangeException("Unknown connection.");
            }
            Settings = CreateSettings(type);
            Settings.ServerVersion = version;
            Settings.TablePrefix = tablePrefix;
            DbTypeMap[typeof(char)] = Settings.CharColumnDefinition;
            DbTypeMap[typeof(bool)] = Settings.BoolColumnDefinition;
            DbTypeMap[typeof(Guid)] = Settings.GuidColumnDefinition;
            DbTypeMap[typeof(DateTime)] = Settings.DateTimeColumnDefinition(TimeStorageUnit.Seconds);
            DbTypeMap[typeof(TimeSpan)] = Settings.TimeSpanColumnDefinition;
            DbTypeMap[typeof(DateTimeOffset)] = Settings.DateTimeOffsetColumnDefinition(TimeStorageUnit.Seconds);
            DbTypeMap[typeof(DateOnly)] = Settings.DateOnlyColumnDefinition;
            DbTypeMap[typeof(TimeOnly)] = Settings.TimeOnlyColumnDefinition;
            DbTypeMap[typeof(byte)] = Settings.ByteColumnDefinition;
            DbTypeMap[typeof(sbyte)] = Settings.SByteColumnDefinition;
            DbTypeMap[typeof(Int16)] = Settings.ShortColumnDefinition;
            DbTypeMap[typeof(UInt16)] = Settings.UShortColumnDefinition;
            DbTypeMap[typeof(Int32)] = Settings.IntColumnDefinition;
            DbTypeMap[typeof(UInt32)] = Settings.UIntColumnDefinition;
            DbTypeMap[typeof(Int64)] = Settings.LongColumnDefinition;
            DbTypeMap[typeof(UInt64)] = Settings.ULongColumnDefinition;
            DbTypeMap[typeof(float)] = Settings.FloatColumnDefinition;
            DbTypeMap[typeof(double)] = Settings.DoubleColumnDefinition;
            DbTypeMap[typeof(decimal)] = Settings.DesimalColumnDefinition;
            DbTypeMap[typeof(object)] = Settings.ObjectColumnDefinition;
        }

        /// <summary>
        /// Get table name.
        /// </summary>
        /// <returns>Table name.</returns>
        public string GetTableName<T>()
        {
            return GetTableName(typeof(T), false);
        }

        /// <summary>
        /// Update DB relations.
        /// </summary>
        /// <param name="mainType"></param>
        /// <param name="s"></param>
        /// <param name="primaryData">True, if primary relation data is updated.</param>
        /// <param name="relationTable">Relation tables.</param>
        private static void UpdateRelations(Type mainType, GXSerializedItem s, bool primaryData, Dictionary<Type, GXRelationTable> relationTable)
        {
            Type type;
            if (primaryData)
            {
                s.Relation = new GXRelationTable();
                s.Relation.Column = s;
                s.Relation.PrimaryTable = mainType;
                s.Relation.PrimaryId = s;
                if ((s.Attributes & Attributes.ForeignKey) != 0)
                {
                    ForeignKeyAttribute[] fks = ((ForeignKeyAttribute[])(s.Target as PropertyInfo).GetCustomAttributes(typeof(ForeignKeyAttribute), false));
                    ForeignKeyAttribute fk;
                    if (fks.Length == 0)
                    {
                        fk = null;
                        type = null;
                    }
                    else
                    {
                        fk = fks[0];
                        type = fk.Type;
                    }
                    //If type is not give in ForeignKeyAttribute.
                    if (type == null)
                    {
                        type = s.Type;
                        if (typeof(IEnumerable).IsAssignableFrom(type))
                        {
                            type = GXInternal.GetPropertyType(type);
                        }
                    }
                    s.Relation.ForeignTable = type;
                    if (fk != null && fk.MapTable != null)
                    {
                        s.Relation.RelationType = RelationType.ManyToMany;
                    }
                    else if (s.Type != typeof(string) && typeof(System.Collections.IEnumerable).IsAssignableFrom(s.Type))
                    {
                        s.Relation.RelationType = RelationType.OneToMany;
                        s.Relation.PrimaryId = GXSqlBuilder.FindRelation(type, mainType);
                        if (s.Relation.PrimaryId == null)
                        {
                            throw new Exception(string.Format("Relation create failed. Foreign table '{0}' do not have relation to table '{1}'.",
                                GXDbHelpers.ConvertToString(null, TargetType.Table, null, type, null),
                                GXDbHelpers.OriginalTableName(mainType)));
                        }
                    }
                    else
                    {
                        s.Relation.RelationType = RelationType.OneToOne;
                    }
                }
                else if ((s.Attributes & Attributes.Relation) != 0)
                {
                    RelationAttribute ra = ((RelationAttribute[])(s.Target as PropertyInfo).GetCustomAttributes(typeof(RelationAttribute), false))[0];
                    type = ra.Target;
                    if (type == null)
                    {
                        type = s.Type;
                    }
                    if (typeof(IEnumerable).IsAssignableFrom(type))
                    {
                        type = GXInternal.GetPropertyType(type);
                    }
                    s.Relation.ForeignTable = type;
                }
            }
            else
            {
                if ((s.Attributes & Attributes.ForeignKey) != 0)
                {
                    ForeignKeyAttribute[] fks = ((ForeignKeyAttribute[])(s.Target as PropertyInfo).GetCustomAttributes(typeof(ForeignKeyAttribute), false));
                    ForeignKeyAttribute fk;
                    if (fks.Length == 0)
                    {
                        fk = null;
                        type = null;
                    }
                    else
                    {
                        fk = fks[0];
                        type = fk.Type;
                    }
                    //If type is not give in ForeignKeyAttribute.
                    if (type == null)
                    {
                        type = s.Type;
                        if (typeof(IEnumerable).IsAssignableFrom(type))
                        {
                            type = GXInternal.GetPropertyType(type);
                        }
                    }
                    GXSerializedItem secondary = FindUnique(type);
                    if (secondary == null)
                    {
                        throw new Exception(string.Format("Table {0} Relation create failed. Class must be derived from IUnique or target type must set in ForeignKey or Relation attribute.",
                            GXDbHelpers.ConvertToString(null, TargetType.Table, null, mainType, null)));
                    }
                    s.Relation.ForeignId = secondary;
                    //Update relation map fields.
                    if (fk != null && fk.MapTable != null)
                    {
                        foreach (var it in GetProperties(fk.MapTable))
                        {
                            if ((it.Value.Attributes & Attributes.ForeignKey) != 0 &&
                                s.Relation.ForeignTable == it.Value.Relation.ForeignTable)
                            {
                                s.Relation.RelationMapTable = it.Value;
                                break;
                            }
                        }
                    }
                }
                else if ((s.Attributes & Attributes.Relation) != 0)
                {
                    RelationAttribute ra = ((RelationAttribute[])(s.Target as PropertyInfo).GetCustomAttributes(typeof(RelationAttribute), false))[0];
                    type = ra.Target;
                    if (type == null)
                    {
                        type = s.Type;
                    }
                    if (typeof(IEnumerable).IsAssignableFrom(type))
                    {
                        type = GXInternal.GetPropertyType(type);
                    }
                    GXSerializedItem secondary = FindUnique(type);
                    if (secondary == null)
                    {
                        throw new Exception(string.Format("Table {0} Relation create failed. Class must be derived from IUnique or target type must set in ForeignKey or Relation attribute.",
                            GXDbHelpers.ConvertToString(null, TargetType.Table, null, mainType, null)));
                    }
                    s.Relation.ForeignId = secondary;
                }
            }
        }

        /// <summary>
        /// Update DB attribute values.
        /// </summary>
        /// <param name="type">Target type.</param>
        /// <param name="attributes"></param>
        /// <param name="s"></param>
        private static void UpdateAttributes(Type type, object[] attributes, GXSerializedItem s)
        {
            int value = 0;
            PropertyInfo pi = s.Target as PropertyInfo;
            if (pi != null && pi.Name == "Id")
            {
                foreach (var i in type.GetInterfaces())
                {
                    if (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IUnique<>))
                    {
                        value |= (int)(Attributes.Id | Attributes.PrimaryKey);
                        break;
                    }
                }
            }
            if (s.Type.IsGenericType && s.Type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                //If value is nullable.
                value |= (int)Attributes.AllowNull;
            }
            else if ((value & (int)(Attributes.Id | Attributes.PrimaryKey)) == 0 &&
                s.Type.IsClass)
            {
                //If value is nullable.
                value |= (int)Attributes.AllowNull;
            }
            foreach (object att in attributes)
            {
                //If field is ignored.
                if (att is IgnoreAttribute && (((IgnoreAttribute)att).IgnoreType & IgnoreType.Db) != 0)
                {
                    value |= (int)Attributes.Ignored;
                }
                else
                {
                    if (att is DefaultValueAttribute)
                    {
                        DefaultValueAttribute def = att as DefaultValueAttribute;
                        s.DefaultValue = def.Value;
                        value |= (int)Attributes.DefaultValue;
                    }
                    //Is property indexed.
                    else if (att is IndexAttribute || att is IndexCollectionAttribute)
                    {
                        value |= (int)Attributes.Index;
                    }
                    //Is property auto indexed value.
                    else if (att is AutoIncrementAttribute)
                    {
                        value |= (int)Attributes.AutoIncrement;
                    }
                    //Primary key value.
                    else if (att is PrimaryKeyAttribute)
                    {
                        value |= (int)Attributes.PrimaryKey;
                    }
                    //Foreign key value.
                    else if (att is ForeignKeyAttribute fk)
                    {
                        value |= (int)Attributes.ForeignKey;
                        value |= (int)Attributes.AllowNull;
                    }
                    //Relation field.
                    else if (att is RelationAttribute)
                    {
                        value |= (int)Attributes.Relation;
                    }
                    else if (att is StringLengthAttribute)
                    {
                        value |= (int)Attributes.StringLength;
                    }
                    else if (att is DataMemberAttribute)
                    {
                        DataMemberAttribute n = att as DataMemberAttribute;
                        if (n.IsRequired)
                        {
                            value |= (int)Attributes.IsRequired;
                        }
                    }
                    if (att is FilterAttribute fa)
                    {
                        value |= (int)Attributes.Filter;
                        s.FilterType = fa.FilterType;
                        s.FilterValue = fa.DefaultValue;
                    }
                    else if (att is IsRequiredAttribute ra)
                    {
                        if (ra.IsRequired)
                        {
                            value &= ~(int)Attributes.AllowNull;
                        }
                    }
                    else if (att is TimeStorageUnitAttribute tsu && tsu.Unit == TimeStorageUnit.Seconds)
                    {
                        //If MS is ignored.
                        value |= (int)Attributes.MsIgnored;
                    }
                }
            }
            s.Attributes = (Attributes)value;
        }

        /// <summary>
        /// Get field names.
        /// </summary>
        /// <typeparam name="T">Target type.</typeparam>
        /// <returns>Field names.</returns>
        public static string[] GetFields<T>()
        {
            Type type = typeof(T);
            Dictionary<string, GXSerializedItem> properties = GetProperties(type);
            List<string> list = new List<string>();
            foreach (var it in properties)
            {
                list.Add(it.Key);
            }
            return list.ToArray();
        }

        static internal GXSerializedItem FindUnique(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                type = Nullable.GetUnderlyingType(type);
            }
            foreach (var it in GetProperties(type))
            {
                if ((it.Value.Attributes & Attributes.Id) != 0)
                {
                    return it.Value;
                }
            }
            return null;
        }

        static internal GXSerializedItem FindRelation(Type target, Type table)
        {
            foreach (var it in GetProperties(target))
            {
                if (it.Value.Relation != null && it.Value.Relation.ForeignTable.IsAssignableFrom(table))
                {
                    return it.Value;
                }
            }
            return null;
        }

        static internal GXSerializedItem FindAutoIncrement(Type type)
        {
            foreach (var it in GetProperties(type))
            {
                if ((it.Value.Attributes & Attributes.AutoIncrement) != 0)
                {
                    return it.Value;
                }
            }
            return null;
        }

        internal static Dictionary<string, GXSerializedItem> GetProperties<T>()
        {
            return GetProperties(typeof(T));
        }

        /// <summary>
        /// Change database.
        /// </summary>
        /// <param name="databaseName">Name of the database to switch to.</param>
        internal DbConnection ChangeDatabase(DbConnection connection, string databaseName)
        {
            databaseName = GXDbHelpers.GetDatabaseName(Settings.Type, databaseName);
            if (Settings.Type == DatabaseType.SqLite)
            {
                if (connection.ConnectionString == "Data Source=:memory:")
                {
                    return connection;
                }
                var type = connection.GetType();
                connection = (DbConnection)Activator.CreateInstance(
                    type,
                    connection.ConnectionString)!;
                connection.Open();
                return connection;
            }
            if (Settings.Type == DatabaseType.DB2 ||
                Settings.Type == DatabaseType.SapHana)
            {
                GXSchemaManager.ExecuteNonQuery(connection, null, null, "SET SCHEMA " + databaseName);
                return connection;
            }
            if (Settings.Type == DatabaseType.Oracle ||
                Settings.Type == DatabaseType.SapHana)
            {
                GXSchemaManager.ExecuteNonQuery(connection, null, null, "ALTER SESSION SET CURRENT_SCHEMA = " + databaseName);
                return connection;
            }
            connection.ChangeDatabase(databaseName);
            return connection;
        }


        /// <summary>
        /// Initialize select. Save table indexes and column setters to make data handling faster.
        /// </summary>
        internal void InitializeSelect<T>(
            IDataReader reader,
            GXDBSettings settings,
            Dictionary<Type, GXSerializedItem> tables,
            Dictionary<Type, int> TableIndexes,
            Dictionary<int, GXColumnHelper> columns,
            Dictionary<Type, List<object>> mapTable,
            Dictionary<Type, Dictionary<Type, GXSerializedItem>> relationDataSetters)
        {
            GXSerializedItem si;
            string name;
            int tmp;
            Type tp;
            Dictionary<string, GXSerializedItem> properties = null;
            Type tableType = null;
            string lastTable = null;
            DataTable schema = null;
            int pos = 0, tableIndex = -1;
            if (tables.Count == 1)
            {
                tableType = typeof(T);
            }
            else if (!settings.SelectUsingAs)
            {
                schema = reader.GetSchemaTable();
                if (schema.Columns[10].ColumnName == "BaseTableName")
                {
                    tableIndex = 10;
                }
                else
                {
                    foreach (DataColumn index in schema.Columns)
                    {
                        if (index.ColumnName == "BaseTableName")
                        {
                            tableIndex = pos;
                            break;
                        }
                        ++pos;
                    }
                    if (tableIndex == -1)
                    {
                        throw new ArgumentOutOfRangeException("Table name not found.");
                    }
                }
            }

            for (pos = 0; pos != reader.FieldCount; ++pos)
            {
                GXColumnHelper c = new GXColumnHelper();
                //Get column and table name.
                name = reader.GetName(pos);
                //If table name is returned in schema.
                if (schema != null)
                {
                    tmp = name.LastIndexOf('.');
                    if (tmp != -1)
                    {
                        c.Name = name.Substring(tmp + 1);
                        c.Table = name.Substring(0, tmp);
                    }
                    else
                    {
                        c.Name = name;
                        c.Table = schema.Rows[pos].ItemArray[tableIndex].ToString();
                    }
                    if (string.IsNullOrEmpty(c.Table))
                    {
                        throw new Exception("Table name not found in the table schema.");
                    }
                }
                else
                {
                    tmp = name.LastIndexOf('.');
                    if (tmp == -1)
                    {
                        c.Table = GXDbHelpers.ConvertToString(settings, TargetType.Table, null, tableType, null);
                        c.Name = name;
                        if (name[0] == settings.ColumnNameQuoteCharacter)
                        {
                            c.Name = name.Substring(1, name.Length - 2);
                        }
                    }
                    else
                    {
                        c.Table = name.Substring(0, tmp);
                        c.Name = name.Substring(tmp + 1);
                    }
                }
                //If table has change.
                if (string.Compare(lastTable, c.Table, true) != 0)
                {
                    si = null;
                    foreach (var it in tables)
                    {
                        if (string.Compare(GXDbHelpers.ConvertToString(settings, TargetType.Table, null, it.Key, null), c.Table, true) == 0 ||
                            string.Compare(GXDbHelpers.OriginalTableName(it.Key), c.Table, true) == 0)
                        {
                            si = it.Value;
                            break;
                        }
                    }
                    //If there is only one table.
                    if (si == null)
                    {
                        tableType = typeof(T);
                    }
                    else if (si.Relation != null)
                    {
                        tableType = si.Relation.PrimaryTable;
                    }
                    else
                    {
                        tableType = (si.Target as PropertyInfo).ReflectedType;
                    }
                    properties = GXSqlBuilder.GetProperties(tableType);
                    lastTable = c.Table;
                    if (tables.Count != 1)
                    {
                        //Find Relation table setter.
                        foreach (var it in properties)
                        {
                            if (it.Value.Relation != null && it.Value.Relation.RelationType != RelationType.OneToOne &&
                                it.Value.Relation.RelationType != RelationType.Relation &&
                                GXInternal.GetPropertyType(it.Value.Type) == it.Value.Relation.ForeignTable)
                            {
                                Dictionary<Type, GXSerializedItem> list;
                                if (it.Value.Relation.RelationType == RelationType.ManyToMany)
                                {
                                    tp = ((ForeignKeyAttribute[])(it.Value.Target as PropertyInfo).GetCustomAttributes(typeof(ForeignKeyAttribute), true))[0].MapTable;
                                    if (!mapTable.ContainsKey(tp))
                                    {
                                        if (!tables.ContainsKey(it.Value.Relation.ForeignTable))
                                        {
                                            continue;
                                        }
                                        List<object> list2 = new List<object>();
                                        mapTable.Add(tp, list2);
                                        list2.Add(it.Value.Relation.PrimaryTable);
                                        list2.Add(it.Value.Relation.ForeignTable);
                                        GXSerializedItem t = new GXSerializedItem();
                                        list = new Dictionary<Type, GXSerializedItem>();
                                        relationDataSetters.Add(tp, list);
                                        list.Add(it.Value.Relation.ForeignTable, GXSqlBuilder.FindRelation(tp, it.Value.Relation.ForeignTable));
                                        list.Add(it.Value.Relation.PrimaryTable, GXSqlBuilder.FindRelation(tp, it.Value.Relation.PrimaryTable));
                                    }
                                }
                                tp = GXInternal.GetPropertyType(it.Value.Type);
                                if (!tables.ContainsKey(it.Value.Relation.ForeignTable))
                                {
                                    continue;
                                }
                                if (relationDataSetters.ContainsKey(tp))
                                {
                                    if (relationDataSetters[tp].ContainsKey(it.Value.Type))
                                    {
                                        list = relationDataSetters[tp];
                                        if (list.ContainsKey(tableType))
                                        {
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        list = new Dictionary<Type, GXSerializedItem>();
                                    }
                                }
                                else
                                {
                                    list = new Dictionary<Type, GXSerializedItem>();
                                    relationDataSetters.Add(tp, list);
                                }
                                list.Add(tableType, it.Value);
                            }
                        }
                    }
                }
                if (properties.Count != 0 && properties.ContainsKey(c.Name))
                {
                    columns.Add(pos, c);
                    c.Setter = properties[c.Name];
                    //Add table index position.
                    if (TableIndexes != null && (c.Setter.Attributes & Attributes.PrimaryKey) != 0)
                    {
                        if (!TableIndexes.ContainsKey(tableType))
                        {
                            TableIndexes.Add(tableType, pos);
                        }
                    }
                }
                c.TableType = tableType;
            }
        }

        /// <summary>
        /// Get list of databases.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <returns>Array of database names.</returns>
        public string[] GetDatabases(IDbConnection connection, IDbTransaction transaction = null)
        {
            string query = Settings.GetDatabasesQuery();
            return ((List<string>)SelectInternal<string>(connection, transaction, query)).ToArray();
        }


        internal object SelectInternal<T>(IDbConnection connection, IDbTransaction transaction,
         string query)
        {
            object value = null, item, id = null;
            List<object[]> objectList = null;
            List<T> baseList = null;
            List<T> list = null;
            Dictionary<string, GXSerializedItem> properties = null;
            object[] values = null;
            Dictionary<Type, GXSerializedItem> tables = null;
            Type type = typeof(T);
            //Dictionary of read tables by name.
            string maintable = GetTableName(type, false);
            Dictionary<int, GXColumnHelper> columns = null;
            Dictionary<Type, int> TableIndexes = null;
            //This is done because every object is created only once in relation data.
            Dictionary<Type, SortedDictionary<object, object>> objects = null;
            string targetTable;
            Dictionary<Type, Dictionary<Type, GXSerializedItem>> relationDataSetters = null;
            //If n:n relation is used make lists where relation tables are added by relation type.
            Dictionary<Type, List<object>> mapTables = null;
            //Columns that are updated when row is read. This is needed when relation data is try tu update and it's not read yet.
            List<KeyValuePair<int, object>> UpdatedColumns = new List<KeyValuePair<int, object>>();
            if (typeof(T) == typeof(object[]))
            {
                objectList = new List<object[]>();
            }
            else if (GXInternal.IsGenericDataType(typeof(T)))
            {
                baseList = new List<T>();
            }
            else
            {
                tables = new Dictionary<Type, GXSerializedItem>();
                GXSqlBuilder.GetTables(typeof(T), tables);
                //If there are no relations to other tables.
                if (!tables.ContainsKey(type))
                {
                    tables.Add(type, null);
                }
                list = new List<T>();
                columns = new Dictionary<int, GXColumnHelper>();
                //If we are using 1:n or n:n references.
                if (tables.Count != 1)
                {
                    relationDataSetters = new Dictionary<Type, Dictionary<Type, GXSerializedItem>>();
                    TableIndexes = new Dictionary<Type, int>();
                    objects = new Dictionary<Type, SortedDictionary<object, object>>();
                    mapTables = new Dictionary<Type, List<object>>();
                }
            }
            try
            {
                using (IDbCommand com = connection.CreateCommand())
                {
                    com.CommandType = CommandType.Text;
                    com.CommandText = query;
                    com.Transaction = transaction;
                    using (IDataReader reader = com.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            UpdatedColumns.Clear();
                            if (values == null)
                            {
                                values = new object[reader.FieldCount];
                            }
                            reader.GetValues(values);
                            if (columns != null && columns.Count == 0)
                            {
                                InitializeSelect<T>(reader, Settings, tables, TableIndexes, columns, mapTables, relationDataSetters);
                            }
                            targetTable = null;
                            if (list != null)
                            {
                                //If we want to read only basic data types example count(*)
                                if (GXInternal.IsGenericDataType(type))
                                {
                                    list.Add((T)Settings.ChangeType(reader.GetValue(0), type));
                                    return list;
                                }
                                properties = GXSqlBuilder.GetProperties<T>();
                            }
                            if (objectList != null)
                            {
                                objectList.Add(values);
                            }
                            else if (baseList != null)
                            {
                                baseList.Add((T)Convert.ChangeType(values[0], type));
                            }
                            else
                            {
                                item = null;
                                //If we are reading values from multiple tables each component is created only once.
                                bool isCreated = false;
                                //For Oracle reader.FieldCount is too high. For this reason columns.Count is used.
                                for (int pos = 0; pos != Math.Min(reader.FieldCount, columns.Count); ++pos)
                                {
                                    value = null;
                                    //If we are asking some data from the DB that is not exist on class.
                                    //This is removed from the interface etc...
                                    if (!columns.ContainsKey(pos))
                                    {
                                        continue;
                                    }
                                    GXColumnHelper col = columns[pos];
                                    //If we are reading multiple objects and object has changed.
                                    if (string.Compare(col.Table, targetTable, true) != 0)
                                    {
                                        isCreated = false;
                                        if (TableIndexes != null && TableIndexes.ContainsKey(col.TableType))
                                        {
                                            id = values[TableIndexes[col.TableType]];
                                            if (id == null || id is DBNull)
                                            {
                                                isCreated = true;
                                            }
                                            else
                                            {
                                                if (objects.ContainsKey(col.TableType))
                                                {
                                                    // Check is item already created.
                                                    if (objects[col.TableType].ContainsKey(Settings.ChangeType(id, col.Setter.Type)))
                                                    {
                                                        isCreated = true;
                                                    }
                                                }
                                                else
                                                {
                                                    objects.Add(col.TableType, new SortedDictionary<object, object>());
                                                }
                                            }
                                        }
                                        else //If Map table.
                                        {
                                            id = null;
                                        }
                                        if (!isCreated)
                                        {
                                            if (!GXInternal.IsGenericDataType(col.TableType) && item == null || item.GetType() != col.TableType)
                                            {
                                                item = GXInternal.CreateClass(col.TableType);
                                                if (item != null && item.GetType() == typeof(T))
                                                {
                                                    list.Add((T)item);
                                                }
                                                //If we are adding map table.
                                                if (mapTables != null && item != null && id == null && mapTables.ContainsKey(item.GetType()))
                                                {
                                                    mapTables[item.GetType()].Add(item);
                                                }
                                            }
                                            if (objects != null && id != null)
                                            {
                                                //Id is not save directly because class might change it's type example from uint to int.
                                                if (GXInternal.IsGenericDataType(col.Setter.Type))
                                                {
                                                    objects[col.TableType].Add(Settings.ChangeType(id, col.Setter.Type), item);
                                                }
                                                else //If we are saving table.
                                                {
                                                    objects[col.TableType].Add(id, item);
                                                }
                                            }
                                        }
                                        targetTable = col.Table;
                                    }
                                    if (!isCreated)
                                    {
                                        //If 1:1 relation.
                                        if (objects != null && !GXInternal.IsGenericDataType(col.Setter.Type) &&
                                            !GXInternal.IsGenericDataType(GXInternal.GetPropertyType(col.Setter.Type)) &&
                                            col.Setter.Type.IsClass && col.Setter.Type != typeof(byte[]))
                                        {
                                            Type pt = GXInternal.GetPropertyType(col.Setter.Type);
                                            if (GXInternal.IsGenericDataType(pt))
                                            {
                                                if (!string.IsNullOrEmpty(values[pos].ToString()))
                                                {
                                                    string[] tmp = values[pos].ToString().Split(new char[] { ';' });
                                                    Array items = Array.CreateInstance(pt, tmp.Length);
                                                    int pos2 = -1;
                                                    foreach (string it in tmp)
                                                    {
                                                        items.SetValue(Settings.ChangeType(it, pt), ++pos2);
                                                    }
                                                    value = items;
                                                }
                                                else
                                                {
                                                    value = Array.CreateInstance(pt, 0);
                                                }
                                            }
                                            else
                                            {
                                                //Columns relations are updated when all data from the row is read.
                                                UpdatedColumns.Add(new KeyValuePair<int, object>(pos, item));
                                            }
                                        }
                                        else if (col.Setter != null)
                                        {
                                            value = Settings.ChangeType(values[pos], col.Setter.Type);
                                        }
                                        else
                                        {
                                            value = values[pos];
                                        }
                                        if (value != null)
                                        {
                                            if (col.Setter.Set != null)
                                            {
                                                col.Setter.Set(item, value);
                                            }
                                            else
                                            {
                                                PropertyInfo pi = col.Setter.Target as PropertyInfo;
                                                if (pi != null)
                                                {
                                                    pi.SetValue(item, value, null);
                                                }
                                                else
                                                {
                                                    FieldInfo fi = col.Setter.Target as FieldInfo;
                                                    fi.SetValue(item, value);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            //Update columns that was not read yet.
                            foreach (var it in UpdatedColumns)
                            {
                                GXColumnHelper col = columns[it.Key];
                                object relationId = Settings.ChangeType(values[it.Key], col.Setter.Relation.ForeignId.Type);
                                if (objects.ContainsKey(col.Setter.Type) && objects[col.Setter.Type].ContainsKey(relationId))
                                {
                                    object relationData = objects[col.Setter.Type][relationId];
                                    col.Setter.Set(it.Value, relationData);
                                }
                            }
                            UpdatedColumns.Clear();
                        }
                        reader.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                throw GXDatabaseException.Create(ex, query);
            }
            if (list != null)
            {
                //Update relation data.
                if (relationDataSetters != null)
                {
                    Type mapTable = null;
                    foreach (var it in objects)
                    {
                        if (relationDataSetters.ContainsKey(it.Key))
                        {
                            var parents = relationDataSetters[it.Key];
                            foreach (var p in parents)
                            {
                                if (!objects.ContainsKey(p.Key))
                                {
                                    continue;
                                }
                                if (p.Value.Relation.RelationType == RelationType.ManyToMany)
                                {
                                    mapTable = p.Value.Relation.RelationMapTable.Relation.PrimaryTable;
                                }
                                SortedDictionary<object, object> parentList = objects[p.Key];
                                Dictionary<object, List<object>> parentValues = new Dictionary<object, List<object>>();
                                foreach (var p2 in parentList)
                                {
                                    parentValues.Add(p2.Key, new List<object>());
                                }
                                object pId;
                                if (p.Value.Relation.RelationType == RelationType.ManyToMany)
                                {
                                    foreach (object v in mapTables[mapTable])
                                    {
                                        pId = relationDataSetters[mapTable][p.Key].Get(v);
                                        object cId = relationDataSetters[mapTable][p.Value.Relation.ForeignTable].Get(v);
                                        //Loop values and map them to parent id.
                                        foreach (var c in it.Value)
                                        {
                                            object id2 = p.Value.Relation.ForeignId.Get(c.Value);
                                            if (id2.Equals(cId))
                                            {
                                                //Value is null if item is empty in that row.
                                                if (parentValues.ContainsKey(pId))
                                                {
                                                    parentValues[pId].Add(c.Value);
                                                }
                                                break;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    //Loop values and map them to parent id.
                                    foreach (var c in it.Value)
                                    {
                                        if (p.Value.Relation.RelationType != RelationType.Relation)
                                        {
                                            //If FK is primary data type like int.
                                            if (GXInternal.IsGenericDataType(p.Value.Relation.PrimaryId.Type))
                                            {
                                                pId = p.Value.Relation.PrimaryId.Get(c.Value);
                                            }
                                            else //If FK is class.
                                            {
                                                //Get target class.
                                                pId = p.Value.Relation.PrimaryId.Get(c.Value);
                                                //With SQLite there might be some empty rows after delete.
                                                if (pId != null)
                                                {
                                                    //Get ID from target class.
                                                    pId = p.Value.Relation.PrimaryId.Relation.ForeignId.Get(pId);
                                                }
                                            }
                                            //Value is null if item is empty in that row.
                                            if (pId != null && parentValues.ContainsKey(pId))
                                            {
                                                parentValues[pId].Add(c.Value);
                                            }
                                        }
                                    }
                                }
                                //Add collections of child values to the parent.
                                foreach (var p3 in parentValues)
                                {
                                    p.Value.Set(p3.Key, GXInternal.ConvertListIfNeeded(p3.Value, p.Value.Type));
                                }
                            }
                        }
                    }
                }
                return list;
            }
            if (baseList != null)
            {
                return baseList;
            }
            return objectList;
        }

        /// <summary>
        /// Returns table names in the current database.
        /// </summary>
        /// <param name="connection">Database connection.</param>
        /// <param name="transaction">Transaction.</param>
        /// <param name="databaseName">Database name.</param>
        /// <returns>Database table names.</returns>
        internal string[] GetTables(IDbConnection connection, IDbTransaction transaction, string databaseName)
        {
            string query = Settings.GetTables(databaseName);
            return ((List<string>)SelectInternal<string>(connection, transaction, query)).ToArray();
        }

        internal Type GetColumnType(string tableName, string columnName, IDbConnection connection,
          IDbTransaction transaction, out int len, out string databaseType)
        {
            string str = null;
            len = 0;
            string query = Settings.GetColumnTypeQuery(connection.Database, tableName, columnName);
            try
            {
                using (IDbCommand com = connection.CreateCommand())
                {
                    com.Transaction = transaction;
                    com.CommandType = CommandType.Text;
                    com.CommandText = query;
                    using (IDataReader reader = com.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            str = reader.GetString(0);
                            if (reader.FieldCount > 1)
                            {
                                object tmp = reader.GetValue(1);
                                if (tmp != null && !(tmp is DBNull))
                                {
                                    len = Convert.ToInt32(tmp);
                                }
                                if (len < 1 && reader.FieldCount > 2)
                                {
                                    tmp = reader.GetValue(2);
                                    if (tmp != null && !(tmp is DBNull))
                                    {
                                        len = Convert.ToInt32(tmp);
                                    }
                                }
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw GXDatabaseException.Create(ex, query);
            }
            int lengthStart = str?.IndexOf('(') ?? -1;
            if (lengthStart > 0 && str.EndsWith(')') &&
                int.TryParse(str[(lengthStart + 1)..^1], out int parsedLength))
            {
                len = parsedLength;
                str = str[..lengthStart];
            }
            Type type = GetDataType(str, len);
            if (type == null)
            {
                throw new ArgumentException("Invalid data type: " + str);
            }
            databaseType = str;
            return type;
        }

        internal string[] GetColumns(string tableName, IDbConnection connection,
        IDbTransaction transaction = null)
        {
            int index = 0;
            string query = Settings.GetColumnsQuery(connection.Database, tableName, out index);
            List<string> list = new List<string>();
            try
            {
                using (IDbCommand com = connection.CreateCommand())
                {
                    com.Transaction = transaction;
                    com.CommandType = CommandType.Text;
                    com.CommandText = query;
                    using (IDataReader reader = com.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(reader.GetString(index));
                        }
                        reader.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                throw GXDatabaseException.Create(ex, query);
            }
            return list.ToArray();
        }


        internal static Dictionary<string, GXSerializedItem> GetProperties(Type type)
        {
            //Return empty collection if basic type.
            if (GXInternal.IsGenericDataType(type) ||
                typeof(IEnumerable).IsAssignableFrom(type))
            {
                return new Dictionary<string, GXSerializedItem>();
            }

            Dictionary<string, GXSerializedItem> properties;
            if (SerializedObjects.ContainsKey(type))
            {
                properties = SerializedObjects[type];
            }
            else
            {
                //var sw = Stopwatch.StartNew();
                properties = (Dictionary<string, GXSerializedItem>)GXInternal.GetValues(type, false, UpdateAttributes);
                SerializedObjects[type] = properties;
                foreach (var it in properties)
                {
                    //Check is this ForeignKey if not set.
                    if ((it.Value.Attributes & Attributes.ForeignKey) == 0)
                    {
                        if (it.Value.Type != typeof(string) &&
                            it.Value.Type != typeof(Guid) &&
                            it.Value.Type != typeof(DateTime) &&
                            //If Array or List
                            (it.Value.Type.IsClass ||
                            //If IEnumerable
                            typeof(IEnumerable).IsAssignableFrom(it.Value.Type)))
                        {
                            Type type2;
                            if (typeof(IEnumerable).IsAssignableFrom(it.Value.Type))
                            {
                                type2 = GXInternal.GetPropertyType(it.Value.Type);
                            }
                            else
                            {
                                type2 = type;
                            }
                            IDictionary<string, GXSerializedItem> tmp = GetProperties(type2);
                            foreach (var it2 in tmp)
                            {
                                if ((it2.Value.Attributes & Attributes.ForeignKey) != 0 &&
                                    it2.Value.Relation != null &&
                                    it2.Value.Relation.ForeignTable == type)
                                {
                                    it.Value.Attributes |= Attributes.ForeignKey;
                                }
                            }
                        }
                    }

                    if ((it.Value.Attributes & (Attributes.ForeignKey | Attributes.Relation)) != 0)
                    {
                        UpdateRelations(type, it.Value, true, relationTable);
                    }
                }
                foreach (var it in properties)
                {
                    if ((it.Value.Attributes & (Attributes.ForeignKey | Attributes.Relation)) != 0)
                    {
                        UpdateRelations(type, it.Value, false, relationTable);
                    }
                }
                //                sw.Stop();
                //                Debug.WriteLine("Cache '" + type.Name + "' build time: " + sw.ElapsedMilliseconds + " ms");
            }
            return properties;
        }

        /// <summary>
        /// Get table name.
        /// </summary>
        /// <param name="type">Table type.</param>
        /// <param name="addQuoteSeparator">Is quote separator added.</param>
        /// <returns>Table name.</returns>
        internal string GetTableName(Type type, bool addQuoteSeparator)
        {
            return GXDbHelpers.ConvertToString(Settings, addQuoteSeparator ? TargetType.Table : TargetType.Table | TargetType.Plain, null, type.Name, null);
        }
    }
}
