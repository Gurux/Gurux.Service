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

using System.Security.Principal;
using Gurux.Service.Db;
using System.Linq.Expressions;
using System;
using System.Collections.Generic;

namespace Gurux.Service.Rest
{
    /// <summary>
    /// Base class for Gurux REST service.
    /// </summary>
    public abstract class GXRestService
    {
        /// <summary>
        /// User host address.
        /// </summary>
        public string UserHostAddress
        {
            get;
            internal set;
        }

        /// <summary>
        /// Application host.
        /// </summary>
        public GXAppHost Host
        {
            get;
            internal set;
        }

        /// <summary>
        /// User information.
        /// </summary>
        public IPrincipal User
        {
            get;
            internal set;
        }

        /// <summary>
        /// Database connection.
        /// </summary>
        public GXDbConnection Db
        {
            get;
            internal set;
        }

        public T SelectById<T>(ulong id)
        {
            GXSelectArgs arg = GXSelectArgs.SelectById<T>(id);
            arg.Relations = false;
            return Db.SingleOrDefault<T>(arg);
        }

        public T SelectById<T>(long id)
        {
            GXSelectArgs arg = GXSelectArgs.SelectById<T>(id);
            arg.Relations = false;
            return Db.SingleOrDefault<T>(arg);
        }

        public List<T> Select<T>(Expression<Func<T, object>> columns, Expression<Func<T, object>> where)
        {
            GXSelectArgs arg = GXSelectArgs.Select<T>(columns, where);
            arg.Relations = false;
            return Db.Select<T>(arg);
        }

        public T SingleOrDefault<T>(Expression<Func<T, object>> columns, Expression<Func<T, object>> where)
        {
            GXSelectArgs arg = GXSelectArgs.Select<T>(columns, where);
            arg.Relations = false;
            return Db.SingleOrDefault<T>(arg);
        }
    }
}
