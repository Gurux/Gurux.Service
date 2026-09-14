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
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace Gurux.Service.Orm.Common.Model
{
    /// <summary>
    /// Describes the type, constraints, and database metadata of a table column.
    /// </summary>
    public sealed class GXColumnSchema
    {
        /// <summary>
        /// Gets or sets the database column name.
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// Gets or sets the one-based ordinal position of the column.
        /// </summary>
        public int Ordinal { get; set; }

        /// <summary>
        /// Gets or sets the native database type name, such as varchar, NUMBER, or uuid.
        /// </summary>
        public string DbType { get; set; } = "";

        /// <summary>
        /// Gets or sets the corresponding .NET type, or null when the type is unknown.
        /// </summary>
        [JsonIgnore]
        [XmlIgnore]
        public Type Type { get; set; } = default!;

        /// <summary>
        /// Gets or sets the full .NET type name used to serialize <see cref="Type"/>.
        /// </summary>
        /// <remarks>Assigning null, an empty string, or whitespace clears <see cref="Type"/>. Other values are resolved with <see cref="System.Type.GetType(string, bool)"/> with errors enabled.</remarks>
        /// <exception cref="TypeLoadException">The specified type cannot be found.</exception>
        public string? TypeName
        {
            get => Type?.FullName;
            set => Type = string.IsNullOrWhiteSpace(value)
                ? null!
                : System.Type.GetType(value, throwOnError: true)!;
        }

        /// <summary>
        /// Gets or sets the maximum character or binary length, or null if unknown or inapplicable.
        /// </summary>
        public long? MaxLength { get; set; }

        /// <summary>
        /// Gets or sets the numeric precision, or null if unknown or inapplicable. For decimal(18, 2), this is 18.
        /// </summary>
        public int? Precision { get; set; }

        /// <summary>
        /// Gets or sets the numeric scale, or null if unknown or inapplicable. For decimal(18, 2), this is 2.
        /// </summary>
        public int? Scale { get; set; }

        /// <summary>
        /// Gets or sets the fractional-second precision, or null if unknown or inapplicable.
        /// </summary>
        public int? DateTimePrecision { get; set; }

        /// <summary>
        /// Gets or sets whether the column accepts NULL values.
        /// </summary>
        public bool IsNullable { get; set; }

        /// <summary>
        /// Gets or sets whether the column belongs to the primary key.
        /// </summary>
        public bool IsPrimaryKey { get; set; }

        /// <summary>
        /// Gets or sets whether the column is marked as unique.
        /// </summary>
        public bool IsUnique { get; set; }

        /// <summary>
        /// Gets or sets whether the database generates the column value.
        /// </summary>
        public bool IsGenerated { get; set; }

        /// <summary>
        /// Gets or sets whether the column is an identity column.
        /// </summary>
        public bool IsIdentity { get; set; }

        /// <summary>
        /// Gets or sets whether the column auto-increments.
        /// </summary>
        public bool IsAutoIncrement { get; set; }

        /// <summary>
        /// Gets or sets whether the column value is computed from an expression.
        /// </summary>
        public bool IsComputed { get; set; }

        /// <summary>
        /// Gets or sets the column default value or expression, or null if no default is available.
        /// </summary>
        public object? DefaultValue { get; set; }

        /// <summary>
        /// Gets or sets the computed column expression, or null if unavailable.
        /// </summary>
        public string? ComputedExpression { get; set; }

        /// <summary>
        /// Gets or sets the column collation, or null if unavailable.
        /// </summary>
        public string? Collation { get; set; }

        /// <summary>
        /// Gets or sets the column description, or null if unavailable.
        /// </summary>
        public string? Comment { get; set; }

        /// <summary>
        /// Gets or sets the containing table schema, or null if unavailable.
        /// </summary>
        [JsonIgnore]
        [XmlIgnore]
        [SoapIgnore]
        public GXTableSchema? Parent { get; set; }

        /// <summary>
        /// Determines whether a column and a value refer to the same object.
        /// </summary>
        /// <param name="column">The column schema to compare, or null.</param>
        /// <param name="value">The value to compare, or null.</param>
        /// <returns><see langword="true"/> if the operands are the same reference or are both null; otherwise, <see langword="false"/>.</returns>
        public static bool operator ==(GXColumnSchema? column, object? value)
        {
            return Equals(column, value);
        }

        /// <summary>
        /// Determines whether a column and a value refer to different objects.
        /// </summary>
        /// <param name="column">The column schema to compare, or null.</param>
        /// <param name="value">The value to compare, or null.</param>
        /// <returns><see langword="true"/> if the operands are different references; otherwise, <see langword="false"/>.</returns>
        public static bool operator !=(GXColumnSchema? column, object? value)
        {
            return !Equals(column, value);
        }

        /// <summary>
        /// Determines whether the specified object is this column schema instance.
        /// </summary>
        /// <param name="obj">The object to compare with this instance.</param>
        /// <returns><see langword="true"/> if the objects are the same instance; otherwise, <see langword="false"/>.</returns>
        public override bool Equals(object? obj)
        {
            return ReferenceEquals(this, obj);
        }

        /// <summary>
        /// Returns an identity-based hash code for this instance.
        /// </summary>
        /// <returns>The hash code for this column schema instance.</returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        /// Returns the column name and native database type.
        /// </summary>
        /// <returns>The column name and database type separated by a space.</returns>
        public override string? ToString()
        {
            return $"{Name} {DbType}";
        }
    }
}
