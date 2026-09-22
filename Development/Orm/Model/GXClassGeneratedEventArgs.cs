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
    /// <summary>Contains the name and source code of a generated class.</summary>
    public sealed class GXClassGeneratedEventArgs : EventArgs
    {
        /// <summary>Initializes the generated class information.</summary>
        /// <param name="className">Name of the generated class.</param>
        /// <param name="source">Generated source code.</param>
        public GXClassGeneratedEventArgs(string className, string source)
        {
            ClassName = className;
            Source = source;
        }

        /// <summary>
        /// Class name of the generated class.
        /// </summary>
        public string ClassName { get; }
        /// <summary>
        /// Generated source code of the class.
        /// </summary>
        public string Source { get; }
    }
}
