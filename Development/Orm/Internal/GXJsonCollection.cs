// --------------------------------------------------------------------------
// Gurux Ltd
//
// Copyright (c) Gurux Ltd.
//
// This file is part of Gurux Device Framework.
//
// Commit: $Format:%H$
// Date:   $Format:%cI$
//
// This software is available under either of the following licenses:
//
// 1. GNU General Public License, version 2 only (GPL-2.0-only).
//    See the LICENSE file included with this distribution.
//
// 2. A commercial license granted by Gurux Ltd under a separate written
//    agreement. Use under this option requires a valid commercial license
//    and is governed by the terms of that agreement.
//
// If you have not obtained a commercial license from Gurux Ltd,
// use of this software is governed by GPL-2.0-only.
//
// Commercial licensing: https://www.gurux.fi/
//
// SPDX-License-Identifier: GPL-2.0-only OR LicenseRef-Gurux-Commercial
// --------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Gurux.Service.Orm.Internal;

/// <summary>Maps scalar collections and string-keyed scalar dictionaries to one JSON text column. Byte arrays retain their binary mapping.</summary>
internal static class GXJsonCollection
{
    internal readonly record struct Value(string Json);

    private static Type? GetElementType(Type type)
    {
        if (type == typeof(string) || type == typeof(byte[]))
        {
            return null;
        }
        var enumerable = type.GetInterfaces().Prepend(type).FirstOrDefault(t =>
            t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        return enumerable?.GetGenericArguments()[0];
    }

    internal static bool IsCollection(Type type)
    {
        var dictionary = GetDictionaryType(type);
        var element = dictionary?.GetGenericArguments()[1] ?? GetElementType(type);
        if (element == null)
        {
            return false;
        }
        element = Nullable.GetUnderlyingType(element) ?? element;
        return element.IsEnum || (element.IsPrimitive && element != typeof(IntPtr) && element != typeof(UIntPtr))
            || element == typeof(string) || element == typeof(decimal) || element == typeof(Guid)
            || element == typeof(DateTime) || element == typeof(DateTimeOffset)
            || element == typeof(TimeSpan) || element == typeof(DateOnly) || element == typeof(TimeOnly);
    }

    private static Type? GetDictionaryType(Type type) => type.GetInterfaces().Prepend(type).FirstOrDefault(t =>
        t.IsGenericType && (t.GetGenericTypeDefinition() == typeof(IDictionary<,>)
            || t.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>))
        && t.GetGenericArguments()[0] == typeof(string));

    internal static object? Encode(object? value, Type type) => value == null ? null
        : new Value(JsonSerializer.Serialize(value, GetDictionaryType(type)
            ?? typeof(IEnumerable<>).MakeGenericType(GetElementType(type)!)));

    internal static object? Decode(string json, Type type)
    {
        if (GetDictionaryType(type) != null)
        {
            return JsonSerializer.Deserialize(json, type);
        }
        var element = GetElementType(type)!;
        var listType = typeof(List<>).MakeGenericType(element);
        var values = (IList?)JsonSerializer.Deserialize(json, listType);
        if (values == null)
        {
            return null;
        }
        var array = Array.CreateInstance(element, values.Count);
        values.CopyTo(array, 0);
        if (type.IsInstanceOfType(array))
        {
            return array;
        }
        if (type.IsInstanceOfType(values))
        {
            return values;
        }
        var enumerable = typeof(IEnumerable<>).MakeGenericType(element);
        var setType = typeof(HashSet<>).MakeGenericType(element);
        if (type.IsAssignableFrom(setType))
        {
            return Activator.CreateInstance(setType, values);
        }
        var constructor = type.GetConstructor([enumerable]);
        if (constructor != null)
        {
            return constructor.Invoke([values]);
        }
        var collectionType = typeof(ICollection<>).MakeGenericType(element);
        if (!type.IsAbstract && type.GetConstructor(Type.EmptyTypes) != null && collectionType.IsAssignableFrom(type))
        {
            var collection = Activator.CreateInstance(type)!;
            var add = collectionType.GetMethod("Add")!;
            foreach (var value in values)
            {
                add.Invoke(collection, [value]);
            }
            return collection;
        }
        throw new NotSupportedException($"Cannot materialize scalar collection '{type.FullName}'.");
    }
}
