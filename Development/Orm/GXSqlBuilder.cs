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
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using Gurux.Service.Orm.Common.Model;
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
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Gurux.Service.Orm
{
    /// <summary>
    /// This class is used to make SQL query.
    /// </summary>
    public class GXSqlBuilder
    {
        /// <summary>
        /// Mapping between C# and DB types.
        /// </summary>
        internal Dictionary<Type, string> DbTypeMap = new Dictionary<Type, string>();
        static Dictionary<Type, GXRelationTable> relationTable = new Dictionary<Type, GXRelationTable>();
        /// <summary>
        /// Name of the connected database. This is used to get table names from the database.
        /// </summary>
        internal string Database;
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
        /// Get C# data type from DB data type.
        /// </summary>
        /// <param name="type">The provider-specific database type name.</param>
        /// <param name="len">Column length.</param>
        /// <returns>The CLR type corresponding to the database type and column length.</returns>
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
            if (Settings.Type == DatabaseType.MSSQL &&
                (string.Equals(type, "char", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(type, "nchar", StringComparison.OrdinalIgnoreCase)))
            {
                return len == 1 ? typeof(char) : typeof(string);
            }
            if (Settings.Type == DatabaseType.MSSQL &&
                string.Equals(type, "binary", StringComparison.OrdinalIgnoreCase))
            {
                // SQL Server fixed-length binary columns (for example Data Vault hash keys).
                return typeof(byte[]);
            }
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

            if ((Settings.Type == DatabaseType.MySQL || Settings.Type == DatabaseType.MariaDB) &&
               string.Equals(type, "datetime", StringComparison.OrdinalIgnoreCase))
            {
                return typeof(DateTime);
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
                if (type.StartsWith("TIMESTAMP", StringComparison.OrdinalIgnoreCase))
                {
                    if (type.Contains("WITH TIME ZONE", StringComparison.OrdinalIgnoreCase))
                    {
                        return typeof(DateTimeOffset);
                    }
                    return typeof(DateTime);
                }
                if (string.Equals(type, "DATE", StringComparison.OrdinalIgnoreCase))
                {
                    return typeof(DateOnly);
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
                if (string.Equals(type, "BOOLEAN", StringComparison.OrdinalIgnoreCase))
                {
                    return typeof(bool);
                }
                if (string.Equals(type, "BLOB", StringComparison.OrdinalIgnoreCase) && len != 16)
                {
                    //SAP HANA uses BLOB for binary data.
                    return typeof(byte[]);
                }
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
                if (string.Equals(type, "TIMESTAMP", StringComparison.OrdinalIgnoreCase))
                {
                    return typeof(DateTime);
                }
                if (string.Equals(type, "TIME", StringComparison.OrdinalIgnoreCase))
                {
                    return typeof(TimeOnly);
                }
                if (string.Equals(type, "DATE", StringComparison.OrdinalIgnoreCase))
                {
                    return typeof(DateOnly);
                }
            }
            else if (Settings.Type == DatabaseType.DB2)
            {
                if (string.Equals(type, "INTEGER", StringComparison.OrdinalIgnoreCase))
                {
                    return typeof(int);
                }
                if (string.Equals(type, "DECIMAL", StringComparison.OrdinalIgnoreCase))
                {
                    return typeof(decimal);
                }
                if (string.Equals(type, "TIMESTAMP", StringComparison.OrdinalIgnoreCase))
                {
                    return typeof(DateTime);
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
            throw new Exception("Unsupported type: " + type);
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

        /// <summary>
        /// Change database.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="databaseName">Name of the database to switch to.</param>
        public async Task<DbConnection> ChangeDatabaseAsync(DbConnection connection, string databaseName)
        {
            Database = Settings.EscapeIdentifier(Settings.TablePrefix, databaseName);
            databaseName = Settings.EscapeIdentifier(Settings.TablePrefix, databaseName);
            if (Settings.Type == DatabaseType.SqLite)
            {
                return connection;
            }
            if (Settings.Type == DatabaseType.DB2 ||
                Settings.Type == DatabaseType.SapHana)
            {
                string query = "SET SCHEMA " + databaseName;
                GXSchemaManager.ExecuteNonQuery(this, connection, null, null, query);
                return connection;
            }
            if (Settings.Type == DatabaseType.Oracle ||
                Settings.Type == DatabaseType.SapHana)
            {
                string query = "ALTER SESSION SET CURRENT_SCHEMA = " + databaseName;
                GXSchemaManager.ExecuteNonQuery(this, connection, null, null, query);
                return connection;
            }
            await connection.ChangeDatabaseAsync(databaseName);
            return connection;
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
        /// <param name="sender">The source of the event.</param>
        /// <param name="connection">Database connection.</param>
        /// <param name="transaction">Transaction.</param>
        /// <param name="eventHandler">Event handler for executed SQL.</param>
        /// <param name="databaseName">Database name.</param>
        /// <returns>Database table names.</returns>
        internal string[] GetTables(object sender,
            DbConnection connection,
            IDbTransaction? transaction,
            EventHandler<GXSqlExecutedEventArgs>? eventHandler,
            string databaseName)
        {
            string query = Settings.GetTablesQuery(databaseName);
            return SelectInternal<string>(sender, connection, transaction,
                eventHandler, query, 0, 0).ToArray();
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
        public GXSqlBuilder(DbConnection connection,
            string? tablePrefix = null)
        {
            Database = connection.Database;
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
            Init(type, tablePrefix);
            Settings!.ServerVersion = version;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="type">Database type.</param>
        /// <param name="tablePrefix">Used table prefix (optional).</param>
        public GXSqlBuilder(DatabaseType type,
            string? tablePrefix = null)
        {
            Init(type, tablePrefix);
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="type">Database type.</param>
        /// <param name="tablePrefix">Used table prefix (optional).</param>
        private void Init(DatabaseType type, string? tablePrefix)
        {
            Settings = CreateSettings(type);
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
        /// <param name="mainType">The entity type that owns the relation.</param>
        /// <param name="s">The serialized member metadata to update.</param>
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
                                s.Relation.ForeignTable == it.Value.Relation?.ForeignTable)
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
        /// <param name="attributes">The attributes declared on the mapped member.</param>
        /// <param name="s">The serialized member metadata to update.</param>
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
                        if (s.DefaultValue is DefaultValueKind.NewGuid)
                        {
                            //If database generates new guid when inserting new record.
                            value |= (int)Attributes.NewGuid;
                        }
                        else if (s.DefaultValue is DefaultValueKind.Now ||
                            s.DefaultValue is DefaultValueKind.UtcNow)
                        {
                            //If database generates current timestamp when inserting new record.
                            //This is used to avoid sending timestamp value to database.
                            value |= (int)Attributes.CurrentTimestamp;
                        }
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
                    else if (att is MaxLengthAttribute)
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
                    else if (att is ConcurrencyCheckAttribute)
                    {
                        value |= (int)Attributes.ConcurrencyCheck;
                    }
                    else if (att is System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedAttribute gen && gen.DatabaseGeneratedOption == System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.None)
                    {
                        value |= (int)Attributes.NotGenerated;
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

        static internal GXSerializedItem? FindUnique(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                type = Nullable.GetUnderlyingType(type)!;
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

        static internal KeyValuePair<string, GXSerializedItem>? FindUnique2(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                type = Nullable.GetUnderlyingType(type)!;
            }
            foreach (var it in GetProperties(type))
            {
                if ((it.Value.Attributes & Attributes.Id) != 0)
                {
                    return it;
                }
            }
            return null;
        }

        static internal bool IsSimpleType(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;

            return type.IsPrimitive ||
                   type.IsEnum ||
                   type == typeof(string) ||
                   type == typeof(byte[]);
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
                if ((it.Value.Attributes & (Attributes.Id | Attributes.PrimaryKey)) != 0)
                {
                    if ((it.Value.Attributes & Attributes.AutoIncrement) != 0)
                    {
                        return it.Value;
                    }
                    break;
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
        /// <param name="connection">DB connection.</param>
        /// <param name="databaseName">Name of the database to switch to.</param>
        internal DbConnection ChangeDatabase(DbConnection connection, string databaseName)
        {
            Database = databaseName;
            databaseName = Settings.EscapeIdentifier(Settings.TablePrefix, databaseName);
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
                GXSchemaManager.ExecuteNonQuery(this, connection, null, null, "SET SCHEMA " + databaseName);
                return connection;
            }
            if (Settings.Type == DatabaseType.Oracle ||
                Settings.Type == DatabaseType.SapHana)
            {
                GXSchemaManager.ExecuteNonQuery(this, connection, null, null, "ALTER SESSION SET CURRENT_SCHEMA = " + databaseName);
                return connection;
            }
            connection.ChangeDatabase(databaseName);
            return connection;
        }

        /// <summary>
        /// Get list of databases.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="connection">DB connection.</param>
        /// <param name="transaction">Transaction.</param>
        /// <param name="eventHandler">Event handler for executed SQL.</param>
        /// <returns>Array of database names.</returns>
        internal string[] GetDatabases(object sender,
            IDbConnection connection,
            IDbTransaction? transaction,
            EventHandler<GXSqlExecutedEventArgs>? eventHandler)
        {
            int index = 0;
            string query = Settings.GetDatabasesQuery(out index);
            return SelectInternal<string>(sender, connection, transaction,
                eventHandler, query, index, 0).ToArray();
        }

        private static void NotifyEvent(object sender,
            EventHandler<GXSqlExecutedEventArgs>? eventHandler,
             string query,
             TimeSpan elapsed)
        {
            if (eventHandler != null)
            {
                GXSqlExecutedEventArgs args = new GXSqlExecutedEventArgs
                {
                    Sql = query,
                    Elapsed = elapsed
                };
                eventHandler.Invoke(sender, args);
            }

        }

        internal List<T> SelectInternal<T>(
           object sender,
           IDbConnection connection,
           IDbTransaction? transaction,
           EventHandler<GXSqlExecutedEventArgs>? eventHandler,
           string query,
           int index,
           int commandTimeout,
           CancellationToken cancellationToken = default)
        {
            List<T> list = new List<T>();
            var sw = Stopwatch.StartNew();
            using (IDbCommand com = connection.CreateCommand())
            {
                if (commandTimeout > 0)
                {
                    com.CommandTimeout = commandTimeout;
                }
                com.Transaction = transaction;
                com.CommandType = CommandType.Text;
                com.CommandText = query;
                try
                {
                    using (IDataReader reader = com.ExecuteReader())
                    {
                        bool isClass = typeof(T).IsClass &&
                            typeof(T) != typeof(string) && typeof(T) != typeof(Guid);
                        while (reader.Read())
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            var value = reader.GetValue(index);
                            if (value is DBNull)
                            {
                                value = null;
                            }
                            else if (value != null && !isClass && typeof(T) != value.GetType())
                            {
                                value = Convert.ChangeType(value, typeof(T));
                            }
                            list.Add((T)value);
                        }
                        reader.Close();
                        sw.Stop();
                        NotifyEvent(sender, eventHandler, com.CommandText, sw.Elapsed);
                    }
                }
                catch (OperationCanceledException)
                {
                    sw.Stop();
                    throw;
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    NotifyEvent(sender, eventHandler, com.CommandText, sw.Elapsed);
                    throw GXDatabaseException.Create(ex, com.CommandText);
                }
            }
            return list;
        }

        private static List<T[]> Convert2<T>(List<object[]> list)
        {
            return list
    .SelectMany(x => x)
    .Cast<T[]>()
    .ToList();
        }

        internal object SelectInternal<T>(
            IDbConnection connection,
            IDbTransaction? transaction,
            GXSelectArgs arg,
            int commandTimeout,
            CancellationToken cancellationToken)
        {
            //Generate SQL again.
            arg.Settings = Settings;
            string query = arg.ToString(false);
            cancellationToken.ThrowIfCancellationRequested();
            List<object[]> list = new List<object[]>();
            using (IDbCommand com = connection.CreateCommand())
            {
                if (commandTimeout > 0)
                {
                    com.CommandTimeout = commandTimeout;
                }
                com.Transaction = transaction;
                com.CommandType = CommandType.Text;
                com.CommandText = query;
                try
                {
                    int count = Math.Max(1, arg.Columns.Columns.Count != 0 ?
                        arg.Columns.Columns.Count :
                        arg.Columns.SchemaColumns.Count);
                    using (IDataReader reader = com.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            object[] values = new object[count];
                            reader.GetValues(values);
                            list.Add(values);
                        }
                        reader.Close();
                    }
                    if (count == 1 && (!typeof(T).IsClass ||
                        typeof(T) == typeof(string)))
                    {
                        List<T?> tmp = new List<T?>();
                        foreach (var it in list)
                        {
                            object? value = it[0];
                            if (value is DBNull)
                            {
                                value = null;
                            }
                            else if (value != null && typeof(T) != value.GetType())
                            {
                                value = Convert.ChangeType(value, typeof(T));
                            }
                            tmp.Add((T?)value);
                        }
                        return tmp;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw GXDatabaseException.Create(ex, com.CommandText);
                }
            }
            if (GXInternal.IsGenericDataType(typeof(T)))
            {
                if (typeof(T).IsArray)
                {
                    List<T> objects = new List<T>();
                    foreach (object[] arr in list)
                    {
                        Type elementType = typeof(T).GetElementType()!;
                        Array values = Array.CreateInstance(elementType, arr.Length);
                        for (int pos = 0; pos != arr.Length; ++pos)
                        {
                            object? value = arr[pos];
                            if (value is DBNull)
                            {
                                value = null;
                            }
                            else if (value != null &&
                                pos < arg.Columns.SchemaColumns.Count)
                            {
                                Type type = arg.Columns.SchemaColumns[pos].Type;
                                type = Nullable.GetUnderlyingType(type) ?? type;
                                value = Settings.ChangeType(value, type);
                            }
                            values.SetValue(value, pos);
                        }
                        objects.Add((T)(object)values);
                    }
                    return objects;
                }
                return list;
            }
            //Update values.
            //If there are no relations to other tables.
            //Find classes.
            Dictionary<Type, TreeLevel> tables = new Dictionary<Type, TreeLevel>();
            int index = 0;
            if (arg.Columns.Columns.Count != 0)
            {
                foreach (var it in arg.Columns.Columns)
                {
                    if (!tables.ContainsKey(it.Key))
                    {
                        tables.Add(it.Key, new TreeLevel(it.Key, index, new Dictionary<int, GXSerializedItem>()));
                    }
                    tables[it.Key].indexes.Add(index, it.Value);
                    ++index;
                }
            }
            else if (arg.Columns.SchemaColumns.Count != 0)
            {
                Type type = typeof(T);
                tables.Add(type, new TreeLevel(type, 0, new Dictionary<int, GXSerializedItem>()));
                Dictionary<string, GXSerializedItem> properties = GetProperties(type);
                foreach (GXColumnSchema column in arg.Columns.SchemaColumns.OrderBy(c => c.Ordinal == 0 ? int.MaxValue : c.Ordinal))
                {
                    var property = properties
                        .Where(w => string.Equals(w.Key, column.Name, StringComparison.OrdinalIgnoreCase))
                        .Select(w => w.Value)
                        .SingleOrDefault();
                    if (property != null)
                    {
                        tables[type].indexes.Add(index, property);
                    }
                    ++index;
                }
            }
            return TreeBuilder.BuildTree<T>(Settings, list, tables.Values);
        }

        /// <summary>
        /// Returns table names in the current database.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="connection">Database connection.</param>
        /// <param name="transaction">Transaction.</param>
        /// <param name="eventHandler">Event handler for executed SQL.</param>
        /// <param name="databaseName">Database name.</param>
        /// <returns>Database table names.</returns>
        internal string[] GetTables(object sender,
            IDbConnection connection,
            IDbTransaction transaction,
            EventHandler<GXSqlExecutedEventArgs>? eventHandler,
            string databaseName)
        {
            string query = Settings.GetTablesQuery(databaseName);
            return SelectInternal<string>(sender,
                connection, transaction, eventHandler, query, 0, 0).ToArray();
        }

        internal Type GetColumnType(
            object sender,
            string tableName,
            string columnName,
            IDbConnection connection,
            IDbTransaction? transaction,
            EventHandler<GXSqlExecutedEventArgs>? eventHandler,
            out int len,
            out string databaseType)
        {
            string? str = null;
            len = 0;
            var sw = Stopwatch.StartNew();
            columnName = Settings.EscapeIdentifier(null, columnName);
            columnName = UnescapeIdentifier(columnName);
            //Escape identifier is not added to column name.
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
                            if (Settings.Type == DatabaseType.DB2 &&
                                string.Equals(str, "TIMESTAMP", StringComparison.OrdinalIgnoreCase) &&
                                reader.FieldCount > 2 && !reader.IsDBNull(2))
                            {
                                // DB2 LENGTH is storage size; SCALE is fractional-second precision.
                                str += "(" + Convert.ToInt32(reader.GetValue(2)).ToString(System.Globalization.CultureInfo.InvariantCulture) + ")";
                            }
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
                sw.Stop();
                NotifyEvent(sender, eventHandler, query, sw.Elapsed);
                throw GXDatabaseException.Create(ex, query);
            }
            if (string.IsNullOrEmpty(str))
            {
                throw new ArgumentException($"Invalid data type for {tableName}.{columnName}.");
            }
            // Keep explicit precision (including zero) for schema comparisons.
            databaseType = str;
            int lengthStart = str.IndexOf('(');
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
            return type;
        }

        internal static string UnescapeIdentifier(string value)
        {
            if (value.Length >= 2 &&
                ((value[0] == '`' && value[^1] == '`') ||
                (value[0] == '"' && value[^1] == '"') ||
                (value[0] == '[' && value[^1] == ']')))
            {
                return value[1..^1];
            }
            return value;
        }

        internal string[] GetColumns(object sender,
            string tableName,
            IDbConnection connection,
            IDbTransaction? transaction,
            EventHandler<GXSqlExecutedEventArgs>? eventHandler)
        {
            int index = 0;
            string query = Settings.GetColumnsQuery(connection.Database,
                tableName, out index);
            return SelectInternal<string>(sender, connection,
                transaction, eventHandler, query, index, 0).ToArray();
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
            lock (SerializedObjects)
            {
                if (SerializedObjects.ContainsKey(type))
                {
                    properties = SerializedObjects[type];
                }
                else
                {
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
                }
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
            return GXDbHelpers.ConvertToString(Settings, addQuoteSeparator ? TargetType.Table : TargetType.Table | TargetType.Plain, null, type, null);
        }
    }
}
