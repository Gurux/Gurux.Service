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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Gurux.Common.JSon;
using System.Security.Principal;
using System.Web;
using Gurux.Service.Rest;
#if !NETSTANDARD2_0 && !NETSTANDARD2_1 && !NETCOREAPP2_0 && !NETCOREAPP2_1 && !NETCOREAPP3_1
namespace Gurux.Service_Test
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class GXServerTest
    {
        GXServer Server;

        public GXServerTest()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test
        [TestInitialize()]
        public void MyTestInitialize()
        {
            Server = new GXServer("http://localhost:6786/", null, this);
        }

        //
        // Use TestCleanup to run code after each test has run
        [TestCleanup()]
        public void MyTestCleanup()
        {
            Server.Close();
        }
        //
        #endregion

        /// <summary>
        /// Post test.
        /// </summary>
        [TestMethod]
        public void PostTest()
        {
            GXJsonClient cl = new GXJsonClient("http://localhost:6786/");
            GXEchoRequest expected = new GXEchoRequest();
            expected.Id = new Random().Next();
            GXEchoResponse actual = cl.Post(expected);
            Assert.AreEqual(expected.Id, actual.Id);
        }

        /// <summary>
        /// Get test.
        /// </summary>
        [TestMethod]
        public void GetTest()
        {
            GXJsonClient cl = new GXJsonClient("http://localhost:6786/");
            GXEchoRequest expected = new GXEchoRequest();
            expected.Id = new Random().Next();
            GXEchoResponse actual = cl.Get(expected);
            Assert.AreEqual(expected.Id, actual.Id);
        }

        /// <summary>
        /// Put test.
        /// </summary>
        [TestMethod]
        public void PutTest()
        {
            GXJsonClient cl = new GXJsonClient("http://localhost:6786/");
            GXEchoRequest expected = new GXEchoRequest();
            expected.Id = new Random().Next();
            GXEchoResponse actual = cl.Put(expected);
            Assert.AreEqual(expected.Id, actual.Id);
        }

        /// <summary>
        /// Delete test.
        /// </summary>
        [TestMethod]
        public void DeleteTest()
        {
            Server.Close();
            Server = new GXAuthenticationServer("http://localhost:6786/");
            GXJsonClient cl = new GXJsonClient("http://localhost:6786/", "Gurux", "Gurux");
            GXEchoRequest expected = new GXEchoRequest();
            expected.Id = new Random().Next();
            GXEchoResponse actual = cl.Delete(expected);
            Assert.AreEqual(expected.Id, actual.Id);
        }

        class GXAuthenticationServer : GXServer
        {
            public GXAuthenticationServer(string prefixes)
                : base(prefixes, null, null)
            {
            }

            public override GenericPrincipal TryAuthenticate(string userName, string password)
            {
                if (userName == "Gurux" && password == "Gurux")
                {
                    return new GenericPrincipal(new GenericIdentity("Gurux"), null);
                }
                return null;
            }
        }

        /// <summary>
        /// Authentication failure test.
        /// </summary>
        [TestMethod, ExpectedException(typeof(HttpException))]
        public void AuthenticationFailureTest()
        {
            GXJsonClient cl = new GXJsonClient("http://localhost:6786/");
            GXEchoRequest expected = new GXEchoRequest();
            expected.Id = new Random().Next();
            GXEchoResponse actual = cl.Delete(expected);
            Assert.AreEqual(expected.Id, actual.Id);
        }

        /// <summary>
        /// Argument fail test.
        /// </summary>
        [TestMethod, ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ArgumentOutOfRangeExceptionTest()
        {
            GXJsonClient cl = new GXJsonClient("http://localhost:6786/");
            GXEchoRequest expected = new GXEchoRequest();
            expected.Id = -1;
            GXEchoResponse actual = cl.Put(expected);
        }
    }
}
#endif //!NETCOREAPP2_0 && !NETCOREAPP2_1 && !NETCOREAPP3_1