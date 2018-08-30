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
using System.Reflection;
using System.Collections;
using Gurux.Common;
using Gurux.Common.Internal;
using System.Collections.Generic;
using System.IO;

namespace Gurux.Service.Rest
{
    internal class GXGeneral
    {
        /// <summary>
        /// Find available Rest messages.
        /// </summary>
        public static void UpdateRestMessageTypes(Hashtable messageMap)
        {
            AuthenticateAttribute[] auths;
            Type tp;
            ParameterInfo[] parameters;
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (GXCommon.IsDefaultAssembly(asm))
                {
                    continue;
                }
                try
                {
                    foreach (Type type in asm.GetExportedTypes())
                    {
                        if (!type.IsAbstract && type.IsClass && typeof(GXRestService).IsAssignableFrom(type))
                        {
                            foreach (MethodInfo method in type.GetMethods())
                            {
                                parameters = method.GetParameters();
                                if (parameters.Length == 1 &&
                                    (method.Name == "Post" || method.Name == "Get" || method.Name == "Put" || method.Name == "Delete"))
                                {
                                    tp = parameters[0].ParameterType;
                                    string name = tp.Name.ToLower();
                                    RouteAttribute[] ra= (RouteAttribute[]) tp.GetCustomAttributes(typeof(RouteAttribute), true);
                                    if (ra.Length == 1)
                                    {
                                        name = ra[0].Path.ToLower();
                                    }
                                    foreach (var it in tp.GetInterfaces())
                                    {
                                        if (it.IsGenericType && it.GetGenericTypeDefinition() == typeof(IGXRequest<>))
                                        {
                                            GXRestMethodInfo r = messageMap[tp.Name.ToLower()] as GXRestMethodInfo;
                                            if (r == null)
                                            {
                                                r = new GXRestMethodInfo();
                                                r.RestClassType = type;
                                                r.RequestType = tp;
                                                r.ResponseType = it.GetGenericArguments()[0];
                                                //If class uses authentication.                                        
                                                auths = (AuthenticateAttribute[])type.GetCustomAttributes(typeof(AuthenticateAttribute), true);
                                                //Check that method is not marked as anonymous.
                                                if (auths.Length == 1)
                                                {
                                                    r.PostAuthentication = auths[0];
                                                    r.GetAuthentication = auths[0];
                                                    r.PutAuthentication = auths[0];
                                                    r.DeleteAuthentication = auths[0];
                                                }
                                                messageMap.Add(name, r);
                                            }
                                            auths = (AuthenticateAttribute[])method.GetCustomAttributes(typeof(AuthenticateAttribute), true);
                                            if (method.Name == "Post")
                                            {
                                                if (r.Post != null)
                                                {
                                                    throw new Exception(string.Format("Message handler for '{0}' already added.", name));
                                                }
                                                r.Post = GXInternal.CreateMethodHandler(tp, method);
                                                //If method uses authentication.
                                                if (auths.Length == 1)
                                                {
                                                    r.PostAuthentication = auths[0];
                                                }
                                                else if (r.PostAuthentication != null)
                                                {
                                                    if (method.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).Length == 1)
                                                    {
                                                        r.PostAuthentication = null;
                                                    }
                                                }
                                            }
                                            else if (method.Name == "Get")
                                            {
                                                if (r.Get != null)
                                                {
                                                    throw new Exception(string.Format("Message handler for '{0}' already added.", name));
                                                }
                                                r.Get = GXInternal.CreateMethodHandler(tp, method);
                                                //If method uses authentication.
                                                if (auths.Length == 1)
                                                {
                                                    r.GetAuthentication = auths[0];
                                                }
                                                else if (r.GetAuthentication != null)
                                                {
                                                    if (method.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).Length == 1)
                                                    {
                                                        r.GetAuthentication = null;
                                                    }
                                                }
                                            }
                                            else if (method.Name == "Put")
                                            {
                                                if (r.Put != null)
                                                {
                                                    throw new Exception(string.Format("Message handler for '{0}' already added.", name));
                                                }
                                                r.Put = GXInternal.CreateMethodHandler(tp, method);
                                                //If method uses authentication.
                                                if (auths.Length == 1)
                                                {
                                                    r.PutAuthentication = auths[0];
                                                }
                                                else if (r.PutAuthentication != null)
                                                {
                                                    if (method.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).Length == 1)
                                                    {
                                                        r.PutAuthentication = null;
                                                    }
                                                }
                                            }
                                            else if (method.Name == "Delete")
                                            {
                                                if (r.Delete != null)
                                                {
                                                    throw new Exception(string.Format("Message handler for '{0}' already added.", name));
                                                }
                                                r.Delete = GXInternal.CreateMethodHandler(tp, method);
                                                //If method uses authentication.
                                                if (auths.Length == 1)
                                                {
                                                    r.DeleteAuthentication = auths[0];
                                                }
                                                else if (r.DeleteAuthentication != null)
                                                {
                                                    if (method.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).Length == 1)
                                                    {
                                                        r.DeleteAuthentication = null;
                                                    }
                                                }
                                            }
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    continue;
                }
            }
        }

        /// <summary>
        /// Get request and response types.
        /// </summary>
        /// <param name="parser"></param>
        /// <param name="data"></param>        
        public static GXRestMethodInfo GetTypes(Hashtable messageMap, string data, out string method)
        {
            int pos = data.LastIndexOf('/');
            if (pos == -1)
            {
                method = data;
            }
            else
            {
                method = data.Substring(pos + 1);
            }
            GXRestMethodInfo r = messageMap[method.ToLower()] as GXRestMethodInfo;
            if (r != null)
            {
                return r;
            }
            return null;
        }
    }
}
