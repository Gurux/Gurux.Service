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
#if !NETCOREAPP2_0 && !NETCOREAPP2_1 && !NETCOREAPP3_1
using System;
using System.Text;
using System.Web;
using Gurux.Common.JSon;
using System.Collections;
using System.Net;
using System.Security.Principal;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using Gurux.Common;
using System.Data.Common;
using Gurux.Service.Orm;
using Gurux.Common.Internal;

namespace Gurux.Service.Rest
{
    public class GXWebService : IHttpHandler
    {
        /// <summary>
        /// Used DB connection.
        /// </summary>
        internal GXDbConnection Connection;

        /// <summary>
        /// Application host.
        /// </summary>
        internal object Host;

        /// <summary>
        /// Information from method.
        /// </summary>
        internal GXRestMethodInfo RestMethodInfo;

        private GXJsonParser Parser;

        private Hashtable RestMap;

        /// <summary>
        /// Constructor.
        /// </summary>
        public GXWebService()
        {
            Parser = new GXJsonParser();
            RestMap = new Hashtable();
        }

        public bool IsReusable
        {
            get
            {
                return true;
            }
        }

        public void ProcessRequest(HttpContext context)
        {
            InvokeHandler handler;
            if (context.Request.ContentType.Contains("json"))
            {
                switch (context.Request.HttpMethod)
                {
                    case "GET":
                        handler = RestMethodInfo.Get;
                        break;
                    case "POST":
                        handler = RestMethodInfo.Post;
                        break;
                    case "PUT":
                        handler = RestMethodInfo.Put;
                        break;
                    case "DELETE":
                        handler = RestMethodInfo.Delete;
                        break;
                    default:
                        handler = null;
                        break;
                }
                if (handler == null)
                {
                    throw new HttpException(405, string.Format("Method '{0}' not allowed for {1}", context.Request.HttpMethod, RestMethodInfo.RequestType.Name));
                }
                object req;
                if (context.Request.HttpMethod == "POST")
                {
                    req = Parser.Deserialize(context.Request.InputStream, RestMethodInfo.RequestType);
                }
                else
                {
                    string data = "{" + context.Request.QueryString.ToString() + "}";
                    req = Parser.Deserialize(data, RestMethodInfo.RequestType);
                }
                //Get Rest class from cache.
                GXRestService target = RestMap[RestMethodInfo.RestClassType] as GXRestService;
                if (target == null)
                {
                    target = GXJsonParser.CreateInstance(RestMethodInfo.RestClassType) as GXRestService;
                    RestMap[RestMethodInfo.RestClassType] = target;
                }
                //Update user and DB info.

                //If proxy is used.
                string add = HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"].ToString();
                if (add == null)
                {
                    add = context.Request.UserHostAddress;
                }
                target.Host = Host;
                target.User = context.User;
                target.Db = Connection;
                object tmp = handler(target, req);
                string reply = Parser.Serialize(tmp);
                context.Response.Write(reply);
                context.Response.ContentType = "json";
            }
        }
    }
}
#endif //!NETCOREAPP2_0 && !NETCOREAPP2_1 && !NETCOREAPP3_1