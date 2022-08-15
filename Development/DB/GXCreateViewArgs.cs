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
using System.Linq.Expressions;
using System.Text;
using Gurux.Service.Orm.Settings;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;
using Gurux.Common.Internal;
namespace Gurux.Service.Orm
{
    /// <summary>
    /// Create View arguments.
    /// </summary>
    public class GXCreateViewArgs
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal GXSettingsArgs Parent;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private GXSelectArgs Select;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Type type;

        /// <summary>
        /// Constructor.
        /// </summary>
        private GXCreateViewArgs()
        {
            Parent = new GXSettingsArgs();
            Maps = new GXMapCollection(Parent);
        }
     
        private void UpdateMaps(GXDBSettings settings, GXMapCollection list, List<GXJoin> joins)
        {
            Select.Columns.Maps.Clear();
            foreach (BinaryExpression it in list.List)
            {
                var e = (MemberExpression)it.Right;
                string post = null;
                string[] target = GXDbHelpers.GetMembers(settings, it.Left, settings.TableQuotation, false, false, ref post, false);
                string[] source = GXDbHelpers.GetMembers(settings, it.Right, settings.TableQuotation, false, false, ref post, false);
                Select.Columns.Maps.Add(source[0], target[0]);
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            UpdateMaps(Settings, Maps, null);
            StringBuilder sb = new StringBuilder();
            sb.Append("Create View ");
            sb.Append(GXDbHelpers.GetTableName(type, true, Parent.Settings.TableQuotation, null));
            sb.Append(" AS ");
            sb.Append(Select.ToString(false));
            return sb.ToString();
        }
       
        /// <summary>
        /// Database settings.
        /// </summary>
        public GXDBSettings Settings
        {
            get
            {
                return Parent.Settings;
            }
            internal set
            {
                Parent.Updated = true;
                Parent.Settings = value;
            }
        }

        /// <summary>
        /// Clear all select settings.
        /// </summary>
        public void Clear()
        {
            Parent.Clear();
            Maps.List.Clear();
        }

        public static GXCreateViewArgs Create<T>(GXSelectArgs arg)
        {
            GXCreateViewArgs view = new GXCreateViewArgs();
            view.Select = arg;
            view.type = typeof(T);
            return view;
        }
       
        /// <summary>
        /// Map columns.
        /// </summary>
        public GXMapCollection Maps
        {
            get;
            private set;
        }
    }
}
