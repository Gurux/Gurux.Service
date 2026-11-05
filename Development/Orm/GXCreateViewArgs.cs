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
        private GXCreateViewArgs(GXSelectArgs select, Type viewType)
        {
            Select = select;
            type = viewType;
            Parent = new GXSettingsArgs();
            Maps = new GXMapCollection(Parent);
        }

        private void UpdateMaps(GXDBSettings settings, GXMapCollection list)
        {
            Select.Columns.Maps.Clear();
            foreach (BinaryExpression it in list.List)
            {
                var e = (MemberExpression)it.Right;
                GXGetMembersArgs args = new GXGetMembersArgs(settings, TargetType.Column)
                {
                    Expression = it.Left,
                };
                string[] target = GXDbHelpers.GetMembers(args) ?? throw new InvalidOperationException("View target columns are missing.");
                args.Expression = it.Right;
                string[] source = GXDbHelpers.GetMembers(args) ?? throw new InvalidOperationException("View source columns are missing.");
                Select.Columns.Maps.Add(source[0], target[0]);
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            UpdateMaps(Settings, Maps);
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
            GXCreateViewArgs view = new GXCreateViewArgs(arg, typeof(T));
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
        public GXMapCollection Maps { get; private set; }
    }
}
