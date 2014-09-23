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
            List<Guid> skipped = new List<Guid>();
            skipped.Add(new Guid("d0df9ca6-b65b-4ffb-869d-2dbc42cc00fa"));//System.Windows.Forms.
            skipped.Add(new Guid("8f5e3cae-3c7a-4750-b280-71828933bb6c"));//System.Dll
            skipped.Add(new Guid("b46af7fe-f908-49cb-8f6b-df42664164c2"));//System.Drawing
            skipped.Add(new Guid("82fb3f30-43db-42a8-9992-226042ad96e5"));//System.Configuration
            skipped.Add(new Guid("8f4ad706-9e29-4df0-9004-4220a2c66a47"));//System.Xml
            skipped.Add(new Guid("5b91143f-4abb-4980-a4bb-eac5ee6c451f"));//Microsoft.VisualStudio.Debugger.Runtime
            skipped.Add(new Guid("26922389-81b1-4617-a702-cdbb11572673"));//Common.Logging
            skipped.Add(new Guid("69dab073-0d24-46d2-a7c0-b6e370f4029d"));//System.Security
            skipped.Add(new Guid("2c34f48b-c05f-470b-8abf-d6cb4dabc839"));//Accessibility
            skipped.Add(new Guid("ce36a65f-23ff-440a-b97c-a8b8854f21d8"));//System.Data.SqlXml
            skipped.Add(new Guid("6dd3b4a4-0fea-4450-ab8b-15a096196f59"));//System.Design
            skipped.Add(new Guid("ee0634e3-cb32-41c6-94b9-5177445ec82d"));//System.Windows.Forms.resources
            string skipPath = Path.GetDirectoryName(System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory());
            
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.GetName().Name == "Anonymously Hosted DynamicMethods Assembly" ||
                    string.IsNullOrEmpty(asm.Location) ||
                    string.Compare(skipPath, Path.GetDirectoryName(asm.Location)) == 0 ||
                    skipped.Contains(asm.ManifestModule.ModuleVersionId))
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
                                    foreach (var it in tp.GetInterfaces())
                                    {
                                        if (it.IsGenericType && it.GetGenericTypeDefinition() == typeof(IGXRequest<>))
                                        {
                                            GXRestMethodInfo r = messageMap[tp.Name] as GXRestMethodInfo;
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
                                                messageMap.Add(tp.Name, r);
                                            }
                                            auths = (AuthenticateAttribute[])method.GetCustomAttributes(typeof(AuthenticateAttribute), true);
                                            if (method.Name == "Post")
                                            {
                                                if (r.Post != null)
                                                {
                                                    throw new Exception(string.Format("Message handler for '{0}' already added.", tp.Name));
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
                                                    throw new Exception(string.Format("Message handler for '{0}' already added.", tp.Name));
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
                                                    throw new Exception(string.Format("Message handler for '{0}' already added.", tp.Name));
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
                                                    throw new Exception(string.Format("Message handler for '{0}' already added.", tp.Name));
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
            GXRestMethodInfo r = messageMap[method] as GXRestMethodInfo;
            if (r != null)
            {
                return r;
            }
            return null;
        }
    }
}
