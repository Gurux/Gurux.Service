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
namespace Gurux.Service.Orm.Model
{
    /// <summary>Contains the table and update choice for a generation operation.</summary>
    public sealed class GXGeneratingEventArgs : EventArgs
    {
        /// <summary>Initializes generation options for a table.</summary>
        /// <param name="tableName">Name of the table being processed.</param>
        public GXGeneratingEventArgs(string tableName)
        {
            TableName = tableName;
        }

        /// <summary>Gets the name of the table being processed.</summary>
        public string TableName { get; }

        /// <summary>Gets or sets whether to update the table. Defaults to true.</summary>
        public bool Update { get; set; } = true;
    }
}
