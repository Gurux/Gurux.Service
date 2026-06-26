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

using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using Gurux.Service.Orm.Settings;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;

namespace Gurux.Common.Internal
{
    /// <summary>
    /// Property getter delegate.
    /// </summary>
    /// <param name="instance">Target.</param>
    /// <returns>Property value</returns>
    delegate object? GetHandler(object instance);

    /// <summary>
    /// Property setter delegate.
    /// </summary>
    /// <param name="instance">Target.</param>
    /// <param name="value">New value.</param>
    delegate void SetHandler(object instance, object? value);

    [Flags]
    enum Attributes : int
    {
        None = 0,
        /// <summary>
        /// Field is Id.
        /// </summary>
        Id = 1,
        /// <summary>
        /// Is index attribute used.
        /// </summary>
        Index = 2,
        /// <summary>
        /// Is auto increment attribute used.
        /// </summary>
        AutoIncrement = 4,
        /// <summary>
        /// Field is primary key.
        /// </summary>
        PrimaryKey = 8,
        /// <summary>
        /// Is foreign key used.
        /// </summary>
        ForeignKey = 0x10,
        //Value is required.
        Required = 0x20,
        /// <summary>
        /// Property is ignored. DB uses this.
        /// </summary>
        Ignored = 0x40,
        /// <summary>
        /// Value is relation to parent table. This is used with 1:n relation.
        /// </summary>
        Relation = 0x80,
        /// <summary>
        /// String length.
        /// </summary>
        StringLength = 0x100,
        /// <summary>
        /// Value is required.
        /// </summary>
        IsRequired = 0x200,
        /// <summary>
        /// Default value is used.
        /// </summary>
        DefaultValue = 0x400,
        /// <summary>
        /// Null value is allowed.
        /// </summary>
        AllowNull = 0x800,
        /// <summary>
        /// Filter is used.
        /// </summary>
        Filter = 0x1000,
        /// <summary>
        /// Millisecond is ignored.
        /// </summary>
        MsIgnored = 0x2000,
    }

    enum RelationType
    {
        /// <summary>
        /// Relation between two tables.
        /// </summary>
        Relation,
        /// <summary>
        /// Primary key 1:1.
        /// </summary>
        OneToOne,
        /// <summary>
        /// Primary key 1:n
        /// </summary>
        OneToMany,
        /// <summary>
        /// Primary key n:n.
        /// </summary>
        ManyToMany
    }

    [DataContract]
    class GXRelationTable
    {
        public GXRelationTable()
        {
        }

        /// <summary>
        /// Column 1 table.
        /// </summary>
        public Type PrimaryTable;

        /// <summary>
        /// Column 1 id.
        /// </summary>
        public GXSerializedItem PrimaryId;

        /// <summary>
        /// Column 1 info.
        /// </summary>
        public GXSerializedItem Column;

        /// <summary>
        /// Column 2 table.
        /// </summary>
        public Type ForeignTable;

        /// <summary>
        /// Column 2 id.
        /// </summary>
        public GXSerializedItem ForeignId;

        /// <summary>
        /// Relation map table if used.
        /// </summary>
        public GXSerializedItem RelationMapTable;

        public RelationType RelationType;
    }

    class GXSerializedItem
    {
        /// <summary>
        /// Property type.
        /// </summary>
        public Type Type;

        public object Target;
        /// <summary>
        /// Default value if given.
        /// </summary>
        public object DefaultValue;
        /// <summary>
        /// Filter type.
        /// </summary>
        public FilterType FilterType;
        /// <summary>
        /// Filter value if given.
        /// </summary>
        public object FilterValue;

        /// <summary>
        /// Set method.
        /// </summary>
        public SetHandler Set;
        /// <summary>
        /// Get Method.
        /// </summary>
        public GetHandler Get;

        public Attributes Attributes;

        public GXRelationTable Relation;
    }

    /// <summary>
    /// GXJSON and Gurux.Service.Rest internal methods.
    /// </summary>
    class GXInternal
    {
#if !NETSTANDARD2_0 && !NETSTANDARD2_1  
        /// <summary>
        /// Cache of compiled IL get-delegates keyed by MemberInfo to avoid repeated reflection.
        /// </summary>
        private static readonly ConcurrentDictionary<MemberInfo, Tuple<GetHandler, SetHandler>> _handlerCache =
            new ConcurrentDictionary<MemberInfo, Tuple<GetHandler, SetHandler>>();

        private static readonly ConcurrentDictionary<Type, Func<object>> ClassCache = new();

        internal static object CreateClass(Type type)
        {
            return ClassCache.GetOrAdd(type, CreateFactory)();
        }

        private static Func<object> CreateFactory(Type type)
        {
            var ctor = type.GetConstructor(Type.EmptyTypes)
                ?? throw new InvalidOperationException($"No default constructor: {type}");

            var dm = new DynamicMethod(
                "Create",
                typeof(object),
                Type.EmptyTypes,
                type.Module,
                true);

            var il = dm.GetILGenerator();

            il.Emit(OpCodes.Newobj, ctor);

            if (type.IsValueType)
                il.Emit(OpCodes.Box, type);

            il.Emit(OpCodes.Ret);

            return (Func<object>)dm.CreateDelegate(typeof(Func<object>));
        }

        private static DynamicMethod CreateGetDynamicMethod(Type type)
        {
            return new DynamicMethod("Get", typeof(object),
                                     [typeof(object)], type.Module, true);
        }

        private static DynamicMethod CreateSetDynamicMethod(Type type)
        {
            return new DynamicMethod("Set", typeof(void),
                                     [typeof(object), typeof(object)], type.Module, true);
        }

        private static void BoxIfNeeded(Type type, ILGenerator generator)
        {
            if (type.IsValueType)
            {
                generator.Emit(OpCodes.Box, type);
            }
        }

        private static void UnboxIfNeeded(Type type, ILGenerator generator)
        {
            if (type.IsValueType)
            {
                generator.Emit(OpCodes.Unbox_Any, type);
            }
        }

        internal static GetHandler CreateGetHandler(Type type, PropertyInfo propertyInfo)
        {
            MethodInfo getMethodInfo = propertyInfo.GetGetMethod(true);
            DynamicMethod dynamicGet = CreateGetDynamicMethod(type);
            ILGenerator getGenerator = dynamicGet.GetILGenerator();

            getGenerator.Emit(OpCodes.Ldarg_0);
            getGenerator.Emit(OpCodes.Call, getMethodInfo);
            BoxIfNeeded(getMethodInfo.ReturnType, getGenerator);
            getGenerator.Emit(OpCodes.Ret);

            return (GetHandler)dynamicGet.CreateDelegate(typeof(GetHandler));
        }

        internal static GetHandler CreateGetHandler(Type type, FieldInfo fieldInfo)
        {
            DynamicMethod dynamicGet = CreateGetDynamicMethod(type);
            ILGenerator getGenerator = dynamicGet.GetILGenerator();

            getGenerator.Emit(OpCodes.Ldarg_0);
            getGenerator.Emit(OpCodes.Ldfld, fieldInfo);
            BoxIfNeeded(fieldInfo.FieldType, getGenerator);
            getGenerator.Emit(OpCodes.Ret);

            return (GetHandler)dynamicGet.CreateDelegate(typeof(GetHandler));
        }

        internal static SetHandler CreateSetHandler(Type type, PropertyInfo propertyInfo)
        {
            MethodInfo setMethodInfo = propertyInfo.GetSetMethod(true);
            DynamicMethod dynamicSet = CreateSetDynamicMethod(type);
            ILGenerator setGenerator = dynamicSet.GetILGenerator();

            setGenerator.Emit(OpCodes.Ldarg_0);
            setGenerator.Emit(OpCodes.Ldarg_1);
            UnboxIfNeeded(setMethodInfo.GetParameters()[0].ParameterType, setGenerator);
            setGenerator.Emit(OpCodes.Call, setMethodInfo);
            setGenerator.Emit(OpCodes.Ret);
            return (SetHandler)dynamicSet.CreateDelegate(typeof(SetHandler));
        }

        internal static SetHandler CreateSetHandler(Type type, FieldInfo fieldInfo)
        {
            DynamicMethod dynamicSet = CreateSetDynamicMethod(type);
            ILGenerator setGenerator = dynamicSet.GetILGenerator();

            setGenerator.Emit(OpCodes.Ldarg_0);
            setGenerator.Emit(OpCodes.Ldarg_1);
            UnboxIfNeeded(fieldInfo.FieldType, setGenerator);
            setGenerator.Emit(OpCodes.Stfld, fieldInfo);
            setGenerator.Emit(OpCodes.Ret);
            return (SetHandler)dynamicSet.CreateDelegate(typeof(SetHandler));
        }
#endif //!NETSTANDARD2_0 && !NETSTANDARD2_1

        /// <summary>
        /// Get custom attribute.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="target"></param>
        /// <returns></returns>
        static public T GetAttribute<T>(object target)
        {
            T[] atts;
            PropertyInfo pi = target as PropertyInfo;
            if (pi != null)
            {
                atts = pi.GetCustomAttributes(typeof(T), true) as T[];
            }
            else
            {
                FieldInfo fi = target as FieldInfo;
                atts = fi.GetCustomAttributes(typeof(T), true) as T[];
            }
            if (atts.Length == 0)
            {
                return default(T);
            }
            return atts[0];
        }

        /// <summary>
        /// Get custom attribute.
        /// </summary>
        /// <typeparam name="T">Target class type.</typeparam>
        /// <param name="type">Custom attribute type to search for.</param>
        /// <returns>Find custom attribute.</returns>
        static public T GetAttribute<T>(Type type)
        {
            T[] atts = type.GetCustomAttributes(typeof(T), true) as T[];
            if (atts.Length == 0)
            {
                return default(T);
            }
            return atts[0];
        }

        /// <summary>
        /// Get property value.
        /// </summary>
        /// <param name="instance">Class instance where value is get.</param>
        /// <param name="target">Property what is get from the instance.</param>
        public static object GetValue(object instance, object target)
        {
            PropertyInfo pi = target as PropertyInfo;
            if (pi != null)
            {
#if !NETSTANDARD2_0 && !NETSTANDARD2_1
                Tuple<GetHandler, SetHandler> propertyHandlers = GetHandlers(pi.DeclaringType, pi);
                GetHandler handler = propertyHandlers.Item1;
                return handler(instance);
#else
                return pi.GetValue(instance, null);
#endif
            }
            FieldInfo fi = target as FieldInfo;
#if !NETSTANDARD2_0 && !NETSTANDARD2_1
            Tuple<GetHandler, SetHandler> fieldHandlers = GetHandlers(fi.DeclaringType, fi);
            GetHandler fieldHandler = fieldHandlers.Item1;
            return fieldHandler(instance);
#else
            return fi.GetValue(instance);
#endif
        }

        /// <summary>
        /// Get property value.
        /// </summary>
        /// <param name="type">Class instance where value is get.</param>
        /// <param name="target">Property what is get from the instance.</param>
        public static Tuple<GetHandler, SetHandler> GetHandlers(Type type, object target)
        {
            PropertyInfo pi = target as PropertyInfo;
            if (pi != null)
            {
#if !NETSTANDARD2_0 && !NETSTANDARD2_1
                Tuple<GetHandler, SetHandler> handlers = _handlerCache.GetOrAdd(pi, p =>
                    new Tuple<GetHandler, SetHandler>(CreateGetHandler(type, pi),
                        CreateSetHandler(type, pi)));
                return handlers;
#else
                return pi.GetValue(instance, null);
#endif
            }
            FieldInfo fi = target as FieldInfo;
#if !NETSTANDARD2_0 && !NETSTANDARD2_1
            Tuple<GetHandler, SetHandler> handlers2 = _handlerCache.GetOrAdd(fi, f =>
                new Tuple<GetHandler, SetHandler>(CreateGetHandler(type, fi),
                CreateSetHandler(type, fi)));
            return handlers2;
#else
            return fi.GetValue(instance);
#endif
        }

        internal delegate void UpdateAttributes(Type type, object[] attributes, GXSerializedItem s);

        /// <summary>
        /// Get serialized property and field values.
        /// </summary>
        /// <param name="type">Instance type.</param>
        /// <param name="sorted">Are values returned as sorted dictionary.</param>
        /// <param name="attributeUpdater">Updater that is called to get wanted value. Can be null.</param>
        /// <returns>Dictionary of values.</returns>
        internal static IDictionary<string, GXSerializedItem> GetValues(Type type, bool sorted, UpdateAttributes attributeUpdater)
        {
            GXSerializedItem s;
            if (type.IsPrimitive || type == typeof(string) || type == typeof(Guid))
            {
                return null;
            }
            IDictionary<string, GXSerializedItem> list;
            if (sorted)
            {
                list = new SortedDictionary<string, GXSerializedItem>(StringComparer.InvariantCultureIgnoreCase);
            }
            else
            {
                list = new Dictionary<string, GXSerializedItem>(StringComparer.InvariantCultureIgnoreCase);
            }
            bool all = type.GetCustomAttributes(typeof(DataContractAttribute), true).Length == 0;
            BindingFlags flags;
            //If DataContractAttribute is not set get only public property values.
            if (all)
            {
                flags = BindingFlags.Instance | BindingFlags.Public;
            }
            else
            {
                flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            }
            //Save properties.
            foreach (PropertyInfo it in type.GetProperties(flags))
            {
                DataMemberAttribute[] attr = (DataMemberAttribute[])it.GetCustomAttributes(typeof(DataMemberAttribute), true);
                //If value is not marked as ignored.
                if (((all && it.CanWrite) || attr.Length != 0) &&
                        it.GetCustomAttributes(typeof(IgnoreDataMemberAttribute), true).Length == 0)
                {
                    if (!list.ContainsKey(it.Name) && it.Name != "Capacity")
                    {
                        s = new GXSerializedItem();
                        s.Target = it;
                        s.Type = it.PropertyType;
                        string name;
                        if (attr.Length == 0 || string.IsNullOrEmpty(attr[0].Name))
                        {
                            name = it.Name;
                        }
                        else
                        {
                            name = attr[0].Name;
                        }
                        if (attributeUpdater != null)
                        {
                            attributeUpdater(type, it.GetCustomAttributes(true), s);
                        }
                        if ((s.Attributes & Attributes.Ignored) == 0)
                        {
#if !NETSTANDARD2_0 && !NETSTANDARD2_1
                            var tmp = GetHandlers(it.PropertyType, it);
                            s.Get = tmp.Item1;
                            s.Set = tmp.Item2;
#endif
                            list.Add(name, s);
                        }
                    }
                }
            }
            if (!all)
            {
                //Save data members.
                foreach (FieldInfo it in type.GetFields(flags))
                {
                    DataMemberAttribute[] attr = (DataMemberAttribute[])it.GetCustomAttributes(typeof(DataMemberAttribute), true);
                    if (attr.Length != 0)
                    {
                        if (!list.ContainsKey(it.Name))
                        {
                            s = new GXSerializedItem();
                            s.Target = it;
                            s.Type = it.FieldType;
                            string name;
                            if (attr.Length == 0 || string.IsNullOrEmpty(attr[0].Name))
                            {
                                name = it.Name;
                            }
                            else
                            {
                                name = attr[0].Name;
                            }
                            if (attributeUpdater != null)
                            {
                                attributeUpdater(type, it.GetCustomAttributes(true), s);
                            }
                            if ((s.Attributes & Attributes.Ignored) == 0)
                            {
#if !NETSTANDARD2_0
                                var tmp = GetHandlers(it.FieldType, it);
                                s.Get = tmp.Item1;
                                s.Set = tmp.Item2;
#endif
                                list.Add(name, s);
                            }
                        }
                    }
                }
            }
            return list;
        }
       
        internal static bool IsGenericDataType(Type type)
        {
            //If nullable.
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                type = Nullable.GetUnderlyingType(type);
            }
            return type.IsPrimitive || type.IsEnum || type == typeof(Guid) || type == typeof(DateTime) ||
                   type == typeof(string) || type == typeof(Type) || type == typeof(object) ||
                   type == typeof(decimal) || type == typeof(TimeSpan) || type == typeof(DateTimeOffset);
        }

        public static object ConvertListIfNeeded(object value, Type targetType)
        {
            if (value == null)
            {
                return null;
            }

            Type sourceType = value.GetType();

            if (targetType.IsAssignableFrom(sourceType))
            {
                return value;
            }

            if (targetType.IsGenericType &&
                targetType.GetGenericTypeDefinition() == typeof(List<>))
            {
                Type targetItemType = targetType.GetGenericArguments()[0];

                var result = (IList)Activator.CreateInstance(targetType)!;

                foreach (object item in (IEnumerable)value)
                {
                    result.Add(item);
                }
                return result;
            }
            throw new InvalidCastException($"Cannot convert {sourceType.FullName} to {targetType.FullName}");
        }

        internal static Type GetPropertyType(Type target)
        {
            if (target == typeof(object))
            {
                return target;
            }
            if (target.IsArray)
            {
                return target.GetElementType();
            }
            Type[] types = target.GetGenericArguments();
            if (types.Length == 0)
            {
                if (target.BaseType == typeof(object))
                {
                    return target;
                }
                if (target.BaseType == typeof(GXTableBase))
                {
                    return target;
                }
                return GetPropertyType(target.BaseType);
            }
            if (types.Length == 1)
            {
                return types[0];
            }
            if (types.Length == 2)
            {
                return types[1];
            }
            throw new ArgumentException("Unsupported number of generic arguments.");
        }

        /// <summary>
        /// Convert date time to epoch string.
        /// </summary>
        /// <param name="dt">Date time to convert.</param>
        /// <param name="get">Is this http get request.</param>
        /// <returns>Date time as epoch string.</returns>
        public static string ToString(DateTime dt, bool get)
        {
            double offset = 0;
            if (get && dt.Kind == DateTimeKind.Local)
            {
                dt = dt.ToUniversalTime();
            }
            else
            {
                if (dt != DateTime.MinValue && dt != DateTime.MaxValue)
                {
                    offset = TimeZoneInfo.Local.GetUtcOffset(dt).TotalMinutes;
                }
            }
            long value = (long)(dt - new DateTime(1970, 1, 1, 0, 0, 0, dt.Kind)).TotalSeconds;
            if (offset != 0)
            {
                string str;
                if (get)
                {
                    str = "/Date(" + value.ToString();
                }
                else
                {
                    str = "\"\\/Date(" + value.ToString();
                }
                if (offset > 0)
                {
                    str += "+";
                }
                else
                {
                    str += "-";
                }
                str += TimeZoneInfo.Local.GetUtcOffset(dt).Hours.ToString("00") +
                       TimeZoneInfo.Local.GetUtcOffset(dt).Minutes.ToString("00");
                if (get)
                {
                    str += ")/";
                }
                else
                {
                    str += ")\\/\"";
                }
                return str;
            }
            if (get)
            {
                return "/Date(" + value.ToString() + ")/";
            }
            return "\"\\/Date(" + value.ToString() + ")\\/\"";
        }
    }
}