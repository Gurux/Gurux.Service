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
using System.Reflection;

namespace Gurux.Service
{
    /// <summary>
    /// SQL helper methods.
    /// </summary>
    public static class GXObjectComparer
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
