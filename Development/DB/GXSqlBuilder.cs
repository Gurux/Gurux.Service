﻿//
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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using Gurux.Service.Orm.Settings;
using Gurux.Common.Internal;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using Gurux.Service.Orm.Enums;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Internal;
using Gurux.Service.Orm.Common.Enums;

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

        private string GetType(string value)
        {
            int pos = value.IndexOf('(');
            if (pos != -1)
            {
                return value.Substring(0, pos);
            }
            return value;
        }

        /// <summary>
        /// Get C# data type from SB data type.
        /// </summary>
        /// <param name="type"></param>
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
            foreach (var it in DbTypeMap)
            {
                if (string.Compare(it.Value, type, true) == 0 ||
                    string.Compare(it.Value, type2, true) == 0)
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
                default:
                    throw new ArgumentOutOfRangeException("Invalid Database type.");
            }
            return settings;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="type">Used BD.</param>
        /// <param name="tablePrefix">Used table prefix (optional).</param>
        public GXSqlBuilder(DatabaseType type, string tablePrefix)
        {
            Settings = CreateSettings(type);
            Settings.TablePrefix = tablePrefix;
            DbTypeMap[typeof(char)] = Settings.CharColumnDefinition;
            DbTypeMap[typeof(bool)] = Settings.BoolColumnDefinition;
            DbTypeMap[typeof(Guid)] = Settings.GuidColumnDefinition;
            DbTypeMap[typeof(DateTime)] = Settings.DateTimeColumnDefinition;
            DbTypeMap[typeof(TimeSpan)] = Settings.TimeSpanColumnDefinition;
            DbTypeMap[typeof(DateTimeOffset)] = Settings.DateTimeOffsetColumnDefinition;
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
            DbTypeMap[typeof(byte[])] = Settings.ByteArrayColumnDefinition;
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
                                GXDbHelpers.GetTableName(type, false, null),
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
                            GXDbHelpers.GetTableName(mainType, true, '\'', null)));
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
                            GXDbHelpers.GetTableName(mainType, true, '\'', null)));
                    }
                    s.Relation.ForeignId = secondary;
                }
            }
        }

        /// <summary>
        /// Update DB attribute values.
        /// </summary>
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
                    if (att is IndexAttribute || att is IndexCollectionAttribute)
                    {
                        value |= (int)Attributes.Index;
                    }
                    //Is property auto indexed value.
                    if (att is AutoIncrementAttribute)
                    {
                        value |= (int)Attributes.AutoIncrement;
                    }
                    //Primary key value.
                    if (att is PrimaryKeyAttribute)
                    {
                        value |= (int)Attributes.PrimaryKey;
                    }
                    //Foreign key value.
                    if (att is ForeignKeyAttribute fk)
                    {
                        value |= (int)Attributes.ForeignKey;
                        value |= (int)Attributes.AllowNull;
                    }
                    //Relation field.
                    else if (att is RelationAttribute)
                    {
                        value |= (int)Attributes.Relation;
                    }
                    if (att is StringLengthAttribute)
                    {
                        value |= (int)Attributes.StringLength;
                    }
                    if (att is DataMemberAttribute)
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
                    if (att is IsRequiredAttribute ra)
                    {
                        if (ra.IsRequired)
                        {
                            value &= ~(int)Attributes.AllowNull;
                        }
                    }
                }
            }
            s.Attributes = (Attributes)value;
        }

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
                            it.Value.Type.IsClass)
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
            return GXDbHelpers.GetTableName(type, addQuoteSeparator, Settings.TableQuotation, Settings.TablePrefix);
        }

        /// <summary>
        ///  Get table name.
        /// </summary>
        /// <param name="type">Table type.</param>
        /// <param name="addQuoteSeparator">Is quote separator added.</param>
        /// <param name="allowSharedTables"></param>
        /// <returns>Table name.</returns>
        internal string GetTableName(Type type, bool addQuoteSeparator, bool allowSharedTables)
        {
            return GXDbHelpers.GetTableName(type, addQuoteSeparator, Settings.TableQuotation, Settings.TablePrefix, allowSharedTables);
        }
    }
}
