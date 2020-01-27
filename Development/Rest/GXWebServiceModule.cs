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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Web;
using System.Data.Common;
using System.Security.Principal;
using System.Net;
using Gurux.Service.Orm;
using System.Web.Security;

namespace Gurux.Service.Rest
{
    public class GXWebServiceModule : IHttpModule
    {
        /// <summary>
        /// REST message map.
        /// </summary>
        private static Hashtable MessageMap;

        /// <summary>
        /// User database connection.
        /// </summary>
        public GXDbConnection Connection
        {
            get;
            set;
        }

        /// <summary>
        /// Host application.
        /// </summary>
        object Host
        {
            get;
            set;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public GXWebServiceModule()
            : this(null, null)
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="connection">used database connection.</param>
        public GXWebServiceModule(GXDbConnection connection, object host)
        {
            Connection = connection;
            Host = host;
            if (MessageMap == null || MessageMap.Count == 0)
            {
                if (MessageMap == null)
                {
                    MessageMap = new Hashtable();
                }
                GXGeneral.UpdateRestMessageTypes(MessageMap);
            }
        }

        /// <summary>
        /// Deny access.
        /// </summary>
        /// <param name="app"></param>
        private void DenyAccess(HttpApplication app)
        {
            app.Response.StatusCode = 401;
            app.Response.StatusDescription = "Access Denied";
            // error not authenticated
            app.Response.Write("401 Access Denied");
            app.CompleteRequest();
        }

        /// <summary>
        /// Try authenticate if authentication is used.
        /// </summary>
        /// <remarks>
        /// Override this method and implement own authentication.
        /// </remarks>
        /// <param name="userName">User name.</param>
        /// <param name="password">Password.</param>
        /// <returns>Returns true if authentication succeeded.</returns>
        public virtual GenericPrincipal TryAuthenticate(string userName, string password)
        {
            return null;
        }

        /// <summary>
        /// Is authentication needed.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private static bool NeedAuthentication(Hashtable messageMap, string httpMethod, string contentType, string path)
        {
            if (contentType == null || !contentType.Contains("json"))
            {
                return false;
            }
            string method;
            GXRestMethodInfo mi = GXGeneral.GetTypes(messageMap, path.ToLower(), out method);
            if (mi == null)
            {
                return false;
            }
            if (httpMethod == "POST")
            {
                return mi.PostAuthentication != null;
            }
            else if (httpMethod == "GET")
            {
                return mi.GetAuthentication != null;
            }
            else if (httpMethod == "PUT")
            {
                return mi.PutAuthentication != null;
            }
            else if (httpMethod == "DELETE")
            {
                return mi.DeleteAuthentication != null;
            }
            throw new Exception("Invalid http method.");
        }

        internal static bool TryAuthenticate(Hashtable messageMap, HttpListenerRequest request, out string username, out string password)
        {
            string path;
            int pos;
            if ((pos = request.RawUrl.IndexOf('?')) != -1)
            {
                path = request.RawUrl.Substring(0, pos);
            }
            else
            {
                path = request.RawUrl;
            }
            return TryAuthenticate(messageMap, request.HttpMethod, request.ContentType, path, request.Headers["Authorization"], out username, out password);
        }

        public virtual GenericPrincipal TryAuthenticate(HttpRequest request, out bool anonymous)
        {
            string username, password;
            GenericPrincipal user = null;
            if (TryAuthenticate(MessageMap, request.HttpMethod, request.ContentType, request.Path, request.Headers["Authorization"], out username, out password))
            {
                user = TryAuthenticate(username, password);
            }
            if (user != null)
            {
                anonymous = false;
                return user;
            }
            anonymous = username == null && password == null;
            return null;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="httpMethod">HTTP method to execute.</param>
        /// <param name="contentType"></param>
        /// <param name="path"></param>
        /// <param name="authHeader">Authentication header.</param>
        /// <param name="username">User name</param>
        /// <param name="password">Password.</param>
        /// <returns>True, if authentication was successful.</returns>
        /// <remarks>
        /// If authentication failed user name and password are empty strings.
        /// If authentication is not used user name and password are null.
        /// </remarks>
        internal static bool TryAuthenticate(Hashtable messageMap, string httpMethod, string contentType, string path, string authHeader, out string username, out string password)
        {
            if (NeedAuthentication(messageMap, httpMethod, contentType, path))
            {
                //Authorization header is checked if present.
                if (!string.IsNullOrEmpty(authHeader))
                {
                    authHeader = authHeader.Trim();
                    if (authHeader.IndexOf("Basic", 0) != 0)
                    {
                        throw new Exception("Invalid authentication header.");
                    }
                    else
                    {
                        authHeader = authHeader.Trim();
                        string encodedCredentials = authHeader.Substring(6);
                        byte[] decodedBytes = Convert.FromBase64String(encodedCredentials);
                        string s = new ASCIIEncoding().GetString(decodedBytes);
                        string[] userPass = s.Split(new char[] { ':' });
                        username = userPass[0];
                        password = userPass[1];
                        return true;
                    }
                }
                else //If authentication is not given, but it's needed.
                {
                    username = password = "";
                    return false;
                }
            }
            username = password = null;
            return false;
        }

        Dictionary<int, GenericPrincipal> Users = new Dictionary<int, GenericPrincipal>();

        private void OnAuthenticateRequest(object sender, EventArgs evtArgs)
        {
            GenericPrincipal gp;
            FormsAuthenticationTicket identityTicket;
            string identityCookieName = "ASPXIDENT";
            HttpApplication app = (HttpApplication)sender;
            // Get the identity cookie
            HttpCookie identityCookie = app.Context.Request.Cookies[identityCookieName];
            if (identityCookie == null || !Users.ContainsKey(identityCookie.Value.GetHashCode()))
            {
                bool anonymous;
                gp = TryAuthenticate(app.Request, out anonymous);
                if (!anonymous)
                {
                    if (gp != null)
                    {
                        string userName = gp.Identity.Name;
                        var context = HttpContext.Current;
                        identityTicket = new FormsAuthenticationTicket(1, userName,
                                                                    DateTime.Now, DateTime.Now.AddHours(1), true, "");
                        string encryptedIdentityTicket = FormsAuthentication.Encrypt(identityTicket);
                        identityCookie = new HttpCookie(identityCookieName, encryptedIdentityTicket);
                        identityCookie.Expires = DateTime.Now.AddHours(1);
                        HttpContext.Current.Response.Cookies.Add(identityCookie);
                        FormsAuthentication.SetAuthCookie(userName, false);
                        app.Context.User = gp;
                        Users.Add(identityCookie.Value.GetHashCode(), gp);
                    }
                    else
                    {
                        DenyAccess(app);
                        return;
                    }
                }
                else
                {
                    app.Context.User = null;
                    return;
                }
            }

            gp = Users[identityCookie.Value.GetHashCode()];
            // decrypt identity ticket
            identityTicket = (FormsAuthenticationTicket)null;
            try
            {
                identityTicket = FormsAuthentication.Decrypt(identityCookie.Value);
            }
            catch
            {
                app.Context.Request.Cookies.Remove(identityCookieName);
                return;
            }

            HttpCookie authCookie = app.Context.Request.Cookies[FormsAuthentication.FormsCookieName];
            if (authCookie != null)
            {
                try
                {
                    FormsAuthenticationTicket authTicket = FormsAuthentication.Decrypt(authCookie.Value);
                    if (authTicket.Name != identityTicket.Name)
                    {
                        return;
                    }
                    app.Context.User = gp;
                }
                catch
                {
                    app.Context.Request.Cookies.Remove(FormsAuthentication.FormsCookieName);
                    return;
                }
            }
        }

        public void Init(HttpApplication context)
        {
            context.PostMapRequestHandler += new EventHandler(OnPostMapRequestHandler);
            context.BeginRequest += new EventHandler(OnBeginRequest);
            context.EndRequest += new EventHandler(OnEndRequest);
            context.AuthenticateRequest += new EventHandler(this.OnAuthenticateRequest);
        }

        public void OnPostMapRequestHandler(object sender, EventArgs e)
        {
            string command;
            HttpContext context = ((HttpApplication)sender).Context;
            GXWebService s = context.Handler as GXWebService;
            if (s != null && context.Request.ContentType.Contains("json"))
            {
                lock (MessageMap)
                {
                    s.RestMethodInfo = GXGeneral.GetTypes(MessageMap, context.Request.Path, out command);
                    s.Connection = Connection;
                    s.Host = Host;
                }
            }
            else
            {
                context.Response.ContentType = "text/html";
                StringBuilder sb = new StringBuilder();
                sb.Append("<http><body>");
                sb.Append("<h1>Gurux.Service</h1>");
                sb.Append("The following operations are supported:<p/>");
                sb.Append("<h2>Operations:</h2><p/>");
                if (MessageMap == null || MessageMap.Count == 0)
                {
                    if (MessageMap == null)
                    {
                        MessageMap = new Hashtable();
                    }
                    GXGeneral.UpdateRestMessageTypes(MessageMap);
                }
                foreach (DictionaryEntry it in MessageMap)
                {
                    sb.Append(it.Key);
                    sb.Append("<p/>");
                }

                sb.Append("</body></http>");
                context.Response.Write(sb.ToString());
            }
        }

        public void Dispose()
        {

        }


        /// <summary>
        /// Record the time of the begin request event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OnBeginRequest(Object sender, EventArgs e)
        {
            HttpApplication httpApp = (HttpApplication)sender;
            httpApp.Context.Items["beginRequestTime"] = DateTime.Now;
        }

        public void OnEndRequest(Object sender, EventArgs e)
        {
            HttpApplication httpApp = (HttpApplication)sender;
            if (httpApp.Response.StatusCode == 401)
            {
                //If the status is 401 the WWW-Authenticated is added to
                //the response so client knows it needs to send credentials
                HttpContext context = httpApp.Context;// HttpContext.Current;
                context.Response.StatusCode = 401;
                context.Response.AddHeader("WWW-Authenticate", "Basic Realm");
            }
            else
            {
                // Get the time of the begin request event.
                if (httpApp.Context.Items.Contains("beginRequestTime"))
                {
                    DateTime beginRequestTime = (DateTime)httpApp.Context.Items["beginRequestTime"];
                    TimeSpan ts = DateTime.Now - beginRequestTime;
                    httpApp.Context.Response.AppendHeader("TimeSpan", ts.ToString());
                }
            }
        }

    }
}
#endif //!NETCOREAPP2_0 && !NETCOREAPP2_1 && !NETCOREAPP3_1