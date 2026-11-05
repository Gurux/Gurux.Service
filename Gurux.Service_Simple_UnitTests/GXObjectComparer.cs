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

using Gurux.Service.Orm;
using System.Reflection;

namespace Gurux.Service_Simple_Unit_Test
{
    /// <summary>
    /// SQL helper methods.
    /// </summary>
    internal static class GXObjectComparer
    {
        /// <summary>
        /// Compares two instances of the same type and returns the properties
        /// whose values are different.
        /// </summary>
        /// <typeparam name="T">The type of the objects to compare.</typeparam>
        /// <param name="obj1">The original object.</param>
        /// <param name="obj2">The object to compare with the original object.</param>
        /// <returns>
        /// A list containing the changed properties and their original and new values.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="obj1"/> or <paramref name="obj2"/> is <c>null</c>.
        /// </exception>
        public static List<GXPropertyDifference> Differences<T>(T obj1, T obj2)
        {
            ArgumentNullException.ThrowIfNull(obj1);
            ArgumentNullException.ThrowIfNull(obj2);

            List<GXPropertyDifference> differences = new List<GXPropertyDifference>();

            foreach (PropertyInfo property in typeof(T).GetProperties(
                BindingFlags.Public | BindingFlags.Instance))
            {
                if (!property.CanRead ||
                    property.GetIndexParameters().Length != 0)
                {
                    continue;
                }

                object? oldValue = property.GetValue(obj1);
                object? newValue = property.GetValue(obj2);

                if (!Equals(oldValue, newValue))
                {
                    differences.Add(new GXPropertyDifference
                    {
                        Name = property.Name,
                        OldValue = oldValue,
                        NewValue = newValue
                    });
                }
            }
            return differences;
        }
    }
}
