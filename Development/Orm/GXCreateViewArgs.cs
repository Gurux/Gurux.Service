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

using Gurux.Service.DB;
using Gurux.Service.Orm.Internal;
using Gurux.Service.Orm.Settings;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Text;
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
                GXGetMembersArgs args = new GXGetMembersArgs(settings, TargetType.Column)
                {
                    Expression = it.Left,
                };
                string[] target = GXDbHelpers.GetMembers(args);
                args.Expression = it.Right;
                string[] source = GXDbHelpers.GetMembers(args);
                Select.Columns.Maps.Add(source[0], target[0]);
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            UpdateMaps(Settings, Maps, null);
            GXGetMembersArgs args = new GXGetMembersArgs(Settings, TargetType.Table)
            {
                StringBuilder = new StringBuilder()
            };
            args.StringBuilder.Append("Create View ");
            args.StringBuilder.Append(GXDbHelpers.ConvertToString(args, type));
            args.StringBuilder.Append(" AS ");
            args.StringBuilder.Append(Select.ToString(false));
            return args.StringBuilder.ToString();
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

        /// <summary>
        /// Create view from select arguments.
        /// </summary>
        /// <typeparam name="T">Type of the view.</typeparam>
        /// <param name="arg">Select arguments.</param>
        /// <returns>GXCreateViewArgs instance.</returns>
        public static GXCreateViewArgs Create<T>(GXSelectArgs arg)
        {
            GXCreateViewArgs view = new GXCreateViewArgs();
            view.Select = arg;
            view.type = typeof(T);
            if (arg.Parent.QueryCache != null)
            {
                view.UseQueryCache(arg.Parent.QueryCache);
            }
            return view;
        }

        /// <summary>
        /// Set query cache.
        /// </summary>
        /// <param name="queryCache">Query cache to use.</param>
        /// <returns>Update arguments.</returns>
        public GXCreateViewArgs UseQueryCache(GXQueryCache queryCache)
        {
            Parent.Settings = GXSqlBuilder.CreateSettings(queryCache.DatabaseType);
            Parent.QueryCache = queryCache ?? Parent.QueryCache ?? new GXQueryCache();
            return this;
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
