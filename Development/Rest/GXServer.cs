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
using System.Net;
using System.Threading;
using System.IO;
using Gurux.Common.JSon;
using System.Web;
using System.Security.Principal;
using System.Text;
using System.Collections;
using Gurux.Service.Orm;
using Gurux.Common.Internal;
using System.ComponentModel;
using System.Reflection;
using System.Web.UI;

namespace Gurux.Service.Rest
{
    public class GXServer
    {
        AutoResetEvent Closing = new AutoResetEvent(false);
        AutoResetEvent Closed = new AutoResetEvent(false);
        /// <summary>
        /// REST message map.
        /// </summary>
        internal Hashtable MessageMap = new Hashtable();


        internal GXJsonParser Parser;

        private Hashtable RestMap;

        public GXDbConnection Connection
        {
            get;
            private set;
        }

        /// <summary>
        /// Host application.
        /// </summary>
        internal object Host
        {
            get;
            private set;
        }

        private CreateObjectEventhandler createObject;
        private ErrorEventHandler onError;

        private HttpListener Listener;

        /// <summary>
        /// Constructor.
        /// </summary>
        public GXServer(string prefixes, GXDbConnection connection, object host) :
            this(new string[] { prefixes }, connection, host)
        {

        }


        public event CreateObjectEventhandler OnCreateObject
        {
            add
            {
                createObject += value;
            }
            remove
            {
                createObject -= value;
            }
        }

        /// <summary>
        /// Listen received errors.
        /// </summary>
        public event ErrorEventHandler OnError
        {
            add
            {
                onError += value;
            }
            remove
            {
                onError -= value;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public GXServer(string[] prefixes, GXDbConnection connection, object host)
        {
            RestMap = new Hashtable();
            Parser = new GXJsonParser();
            Parser.OnCreateObject += new CreateObjectEventhandler(ParserOnCreateObject);
            Connection = connection;
            Host = host;
            if (MessageMap.Count == 0)
            {
                GXGeneral.UpdateRestMessageTypes(MessageMap);
                if (MessageMap.Count == 0)
                {
                    throw new Exception("No REST services available.");
                }
            }
            Listener = new HttpListener();
            foreach (string it in prefixes)
            {
                Listener.Prefixes.Add(it);
            }
            Listener.Start();
            Thread thread = new Thread(new ParameterizedThreadStart(ListenThread));
            thread.Start(this);
        }

        private void ParserOnCreateObject(object sender, GXCreateObjectEventArgs e)
        {
            if (createObject != null)
            {
                createObject(sender, e);
            }
        }
        /// <summary>
        /// Listen clients as long as server is up and running.
        /// </summary>
        /// <param name="parameter"></param>
        void ListenThread(object parameter)
        {
            IPrincipal user;
            HttpListenerContext c = null;
            GXServer tmp = parameter as GXServer;
            HttpListener Listener = tmp.Listener;
            while (Listener.IsListening)
            {
                bool accept = false;
                string username, password;
                AutoResetEvent h = new AutoResetEvent(false);
                IAsyncResult result = Listener.BeginGetContext(delegate (IAsyncResult ListenerCallback)
                {
                    HttpListener listener = (HttpListener)ListenerCallback.AsyncState;
                    //If server is not closed.
                    if (listener.IsListening)
                    {
                        bool html = false;
                        try
                        {
                            c = listener.EndGetContext(ListenerCallback);
                        }
                        catch (Exception ex)
                        {
                            if (onError != null)
                            {
                                onError(this, new ErrorEventArgs(ex));
                            }
                            h.Set();
                            return;
                        }
                        if (c.Request.HttpMethod == "GET" && c.Request.AcceptTypes != null)
                        {
                            foreach (var it in c.Request.AcceptTypes)
                            {
                                if (it == "text/html")
                                {
                                    Thread thread = new Thread(new ParameterizedThreadStart(ShowServices));
                                    thread.Start(new object[] { tmp, c });
                                    html = true;
                                    accept = true;
                                    break;
                                }
                                if (it.Contains("image"))
                                {
                                    html = true;
                                    accept = true;
                                    break;
                                }
                            }
                        }
                        if (!html)
                        {
                            GXWebServiceModule.TryAuthenticate(tmp.MessageMap, c.Request, out username, out password);
                            //Anonymous access is allowed.
                            if (username == null && password == null)
                            {
                                accept = true;
                                user = null;
                            }
                            else
                            {
                                user = TryAuthenticate(username, password);
                                accept = user != null;
                            }

                            if (accept)
                            {
                                Thread thread = new Thread(new ParameterizedThreadStart(Process));
                                thread.Start(new object[] { tmp, c, user });
                            }
                            else
                            {
                                c.Response.StatusCode = 401;
                                c.Response.StatusDescription = "Access Denied";
                                c.Response.AddHeader("WWW-Authenticate", "Basic Realm");
                                GXErrorWrapper err = new GXErrorWrapper(new HttpException(401, "Access Denied"));
                                using (TextWriter writer = new StreamWriter(c.Response.OutputStream, Encoding.ASCII))
                                {
                                    GXJsonParser parser = new GXJsonParser();
                                    string data = parser.Serialize(err);
                                    c.Response.ContentLength64 = data.Length;
                                    writer.Write(data);
                                }
                                c.Response.Close();
                            }
                        }
                        h.Set();
                    }
                }, Listener);
                EventWaitHandle.WaitAny(new EventWaitHandle[] { h, Closing });
                if (!accept || !Listener.IsListening)
                {
                    result.AsyncWaitHandle.WaitOne(1000);
                    Closed.Set();
                    break;
                }
            }
        }

        /// <summary>
        /// Show REST services.
        /// </summary>
        /// <param name="parameter"></param>
        static private void ShowServices(object parameter)
        {
            object[] tmp = parameter as object[];
            GXServer server = tmp[0] as GXServer;
            HttpListenerContext context = tmp[1] as HttpListenerContext;
            StringBuilder writer = new StringBuilder();
            writer.AppendLine("<!DOCTYPE html >");
            writer.AppendLine("<html>");
            writer.AppendLine("<style>");
            writer.AppendLine(".tooltip {");
            writer.AppendLine("position: relative;");
            writer.AppendLine("display: inline-block;");
            writer.AppendLine("border-bottom: 1px dotted black;");
            writer.AppendLine("}");
            writer.AppendLine(".tooltip .tooltiptext {");
            writer.AppendLine("visibility: hidden;");
            writer.AppendLine("width: 600px;");
            writer.AppendLine("background-color: Gray;");
            writer.AppendLine("color: #fff;");
            writer.AppendLine("text-align: left;");
            writer.AppendLine("border-radius: 6px;");
            writer.AppendLine("padding: 5px 0;");
            /* Position the tooltip */
            writer.AppendLine("position: absolute;");
            writer.AppendLine("z-index: 1;");
            writer.AppendLine("}");

            writer.AppendLine(".tooltip:hover .tooltiptext {");
            writer.AppendLine("visibility: visible;");
            writer.AppendLine("}");
            writer.AppendLine("</style>");
            writer.AppendLine("<body>");

            string info = Convert.ToString(server.Host);
            if (info != "")
            {
                writer.AppendLine("<h1>Server information:</h1>");
                writer.AppendLine(info.Replace("\r\n", "<br/>"));
                writer.AppendLine("<hr>");
            }
            writer.Append("<h1>Available REST operations:");
            writer.AppendLine("</h1>");
            if (server.MessageMap.Count == 0)
            {
                GXGeneral.UpdateRestMessageTypes(server.MessageMap);
                if (server.MessageMap.Count == 0)
                {
                    writer.AppendLine("No REST operations available.");
                }
            }
            DescriptionAttribute[] att;
            foreach (GXRestMethodInfo it in server.MessageMap.Values)
            {
                writer.Append("<div class=\"tooltip\">" + it.RequestType.Name);
                writer.Append("<span class=\"tooltiptext\">");
                writer.Append("Method: ");
                if (it.Get != null)
                {
                    writer.Append("Get");
                }
                else if (it.Post != null)
                {
                    writer.Append("Post");
                }
                else if (it.Put != null)
                {
                    writer.Append("Put");
                }
                else if (it.Delete != null)
                {
                    writer.Append("Delete");
                }
                writer.Append("<p></p>");
                att = (DescriptionAttribute[])it.RequestType.GetCustomAttributes(typeof(DescriptionAttribute), true);
                if (att.Length != 0)
                {
                    writer.AppendLine(att[0].Description);
                    writer.AppendLine("<br/>");
                }
                writer.AppendLine("<b>Request:</b><br/>{<br/>");
                foreach (PropertyInfo p in it.RequestType.GetProperties())
                {
                    ShowProperties(p, writer);
                }
                writer.Append("}<p></p>");
                att = (DescriptionAttribute[])it.ResponseType.GetCustomAttributes(typeof(DescriptionAttribute), true);
                if (att.Length != 0)
                {
                    writer.AppendLine(att[0].Description);
                    writer.AppendLine("<br/>");
                }
                writer.AppendLine("<b>Response:</b><br/>{<br/>");
                foreach (PropertyInfo p in it.ResponseType.GetProperties())
                {
                    ShowProperties(p, writer);
                }
                writer.Append("}<br/>");
                writer.Append("</span></div><br/>");
                att = (DescriptionAttribute[])it.RequestType.GetCustomAttributes(typeof(DescriptionAttribute), true);
                if (att.Length != 0)
                {
                    writer.AppendLine(att[0].Description);
                }
                writer.AppendLine("<p></p>");
            }
            writer.AppendLine("</body>");
            writer.AppendLine("</html>");
            using (BufferedStream bs = new BufferedStream(context.Response.OutputStream))
            {
                StreamWriter sw = new StreamWriter(bs);
                sw.Write(writer.ToString());
                sw.Flush();
            }
        }

        static private void ShowProperties(PropertyInfo p, StringBuilder writer)
        {
            bool first = true;
            DescriptionAttribute[] att;
            if (p.PropertyType.IsArray)
            {
                att = (DescriptionAttribute[])p.PropertyType.GetElementType().GetCustomAttributes(typeof(DescriptionAttribute), true);
            }
            else
            {
                att = (DescriptionAttribute[])p.GetCustomAttributes(typeof(DescriptionAttribute), true);
            }
            if (att.Length != 0)
            {
                writer.AppendLine("&nbsp;&nbsp;" + att[0].Description + "<br/>");
            }
            writer.Append("&nbsp;&nbsp;" + p.Name);
            if (p.PropertyType.IsClass)
            {
                if (p.PropertyType.IsArray)
                {
                    Type type = p.PropertyType.GetElementType();
                    if (type.IsClass)
                    {
                        writer.Append("[]<br/>&nbsp;&nbsp;{<br/>");
                        GetProperties(type, writer);
                        writer.Append("<br/>&nbsp;&nbsp;}");
                    }
                    else
                    {
                        GetProperties(type, writer);
                        writer.Append("[]");
                    }
                }
                else
                {
                    writer.Append("<br/>&nbsp;&nbsp;{<br/>");
                    GetProperties(p.PropertyType, writer);
                    writer.Append("<br/>&nbsp;&nbsp;}");
                }
            }
            else if (p.PropertyType.IsArray)
            {
                writer.Append("[]");
            }
            if (first)
            {
                first = false;
            }
            else
            {
                writer.Append(",");
            }
            writer.AppendLine("<br/>");
        }

        static private void GetProperties(Type type, StringBuilder writer)
        {
            bool first = true;
            foreach (var p in type.GetProperties())
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    writer.Append(",");
                    writer.AppendLine("<br/>");
                }
                writer.Append("&nbsp;&nbsp;&nbsp;&nbsp;");
                writer.Append(p.Name);
                DescriptionAttribute[] att = (DescriptionAttribute[])p.GetCustomAttributes(typeof(DescriptionAttribute), true);
                if (att.Length != 0)
                {
                    writer.Append(":&nbsp;&nbsp;&nbsp;&nbsp;" + att[0].Description);
                }
            }
        }

        static private void Process(object parameter)
        {
            object[] tmp = parameter as object[];
            GXServer server = tmp[0] as GXServer;
            HttpListenerContext context = tmp[1] as HttpListenerContext;
            IPrincipal user = tmp[2] as IPrincipal;
            try
            {
                ProcessRequest(server, context, user);
            }
            catch (HttpListenerException)
            {
                //Client has close connection. This is ok.
            }
            catch (Exception ex)
            {
                if (server.onError != null)
                {
                    server.onError(server, new ErrorEventArgs(ex));
                }
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                GXErrorWrapper err = new GXErrorWrapper(ex);
                using (TextWriter writer = new StreamWriter(context.Response.OutputStream, Encoding.ASCII))
                {
                    GXJsonParser parser = new GXJsonParser();
                    string data = parser.Serialize(err);
                    context.Response.ContentLength64 = data.Length;
                    writer.Write(data);
                }
            }
        }

        static void ProcessRequest(GXServer server, HttpListenerContext context,
                IPrincipal user)
        {
            string path, data;
            string method = context.Request.HttpMethod.ToUpper();
            bool content = method == "POST" || method == "PUT";
            if (content)
            {
                int length = (int)context.Request.ContentLength64;
                MemoryStream ms = new MemoryStream(length);
                Stream stream = context.Request.InputStream;
                byte[] buffer = new byte[length == 0 || length > 1024 ? 1024 : length];
                IAsyncResult read = stream.BeginRead(buffer, 0, buffer.Length, null, null);
                while (true)
                {
                    // wait for the read operation to complete
                    if (!read.AsyncWaitHandle.WaitOne())
                    {
                        break;
                    }
                    int count = stream.EndRead(read);
                    ms.Write(buffer, 0, count);
                    // If read is done.
                    if (ms.Position == length || count == 0)
                    {
                        break;
                    }
                    read = stream.BeginRead(buffer, 0, buffer.Length, null, null);
                }
                ms.Position = 0;
                using (StreamReader sr = new StreamReader(ms))
                {
                    data = sr.ReadToEnd();
                }
                path = context.Request.RawUrl;
            }
            else
            {
                int pos = context.Request.RawUrl.IndexOf('?');
                if (pos != -1)
                {
                    path = context.Request.RawUrl.Substring(0, pos);
                    data = context.Request.RawUrl.Substring(pos + 1);
                    data = "{\"" + data.Replace("&", "\",\"").Replace("=", "\":\"") + "\"}";
                }
                else
                {
                    path = context.Request.RawUrl;
                    data = null;
                }
            }
            System.Diagnostics.Debug.WriteLine("-> " + path + " : " + data);

            //If proxy is used.
            string add = null;
            if (HttpContext.Current != null)
            {
                add = HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"].ToString();
            }
            if (add == null)
            {
                add = context.Request.UserHostAddress;
            }
            string reply = GetReply(server.MessageMap, user, server, add, context.Request.HttpMethod, path, data);
            context.Response.ContentType = "application/json";
            context.Response.ContentLength64 = reply.Length;
            System.Diagnostics.Debug.WriteLine("<- " + reply);
            using (BufferedStream bs = new BufferedStream(context.Response.OutputStream))
            {
                bs.Write(ASCIIEncoding.ASCII.GetBytes(reply), 0, reply.Length);
            }
        }

        /// <summary>
        /// Parse query DTO and return response DTO as string.
        /// </summary>
        /// <param name="method">Http method.</param>
        /// <param name="path">Command to execute.</param>
        /// <param name="data">Command data.</param>
        /// <returns>DTO result as string.</returns>
        static string GetReply(Hashtable messageMap, IPrincipal user, GXServer server, string hostAddress, string method, string path, string data)
        {
            InvokeHandler handler;
            string command;
            GXRestMethodInfo mi;
            lock (messageMap)
            {
                mi = GXGeneral.GetTypes(messageMap, path, out command);
            }
            if (mi == null)
            {
                throw new HttpException(405, string.Format("Rest method '{0}' not implemented.", command));
            }
            switch (method.ToUpper())
            {
                case "GET":
                    handler = mi.Get;
                    break;
                case "POST":
                    handler = mi.Post;
                    break;
                case "PUT":
                    handler = mi.Put;
                    break;
                case "DELETE":
                    handler = mi.Delete;
                    break;
                default:
                    handler = null;
                    break;
            }
            if (handler == null)
            {
                throw new HttpException(405, string.Format("Method '{0}' not allowed for {1}", method, command));
            }
            object req = server.Parser.Deserialize("{" + data + "}", mi.RequestType);
            //Get Rest class from cache.
            GXRestService target = server.RestMap[mi.RestClassType] as GXRestService;
            if (target == null)
            {
                target = GXJsonParser.CreateInstance(mi.RestClassType) as GXRestService;
                server.RestMap[mi.RestClassType] = target;
            }
            target.Host = server.Host;
            target.User = user;
            target.Db = server.Connection;
            target.UserHostAddress = hostAddress;
            object tmp = handler(target, req);
            if (tmp == null)
            {
                throw new HttpException(405, string.Format("Command '{0}' returned null.", command));
            }
            return server.Parser.SerializeToHttp(tmp);
        }


        /// <summary>
        /// Try authenticate if authentication is used.
        /// </summary>
        /// <remarks>
        /// Override this method and implement own authentication.
        /// In default authentication fails.
        /// </remarks>
        /// <param name="userName">User name.</param>
        /// <param name="password">User password.</param>
        /// <returns>Returns user identity.</returns>
        public virtual GenericPrincipal TryAuthenticate(string userName, string password)
        {
            return null;
        }

        /// <summary>
        /// Close server.
        /// </summary>
        public void Close()
        {
            Closing.Set();
            Closed.WaitOne(1000);
            Listener.Close();
        }
    }
}
#endif //!NETCOREAPP2_0 && !NETCOREAPP2_1 && !NETCOREAPP3_1