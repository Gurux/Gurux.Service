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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Gurux.Service.Orm.Internal;

/// <summary>Maps scalar collections to one JSON text column. Byte arrays retain their binary mapping.</summary>
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
        var element = GetElementType(type);
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

    internal static object? Encode(object? value, Type type) => value == null ? null
        : new Value(JsonSerializer.Serialize(value, typeof(IEnumerable<>).MakeGenericType(GetElementType(type)!)));

    internal static object? Decode(string json, Type type)
    {
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
