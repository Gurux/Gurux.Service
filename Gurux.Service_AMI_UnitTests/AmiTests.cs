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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Gurux.Service.Orm;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs;

namespace Gurux.Service_Test
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class AMITest
    {
        private TestContext testContextInstance;

        public AMITest()
        {
            GXDbConnection.DefaultDatabaseType = DatabaseType.MSSQL;
        }

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
        }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        /// <summary>
        /// Update User test.
        /// </summary>
        [TestMethod]
        public void UpdateUserTest()
        {
            GXUser user = new GXUser();
            user.Id = "1";
            user.UserName = "Gurux";
            GXUpdateArgs arg2 = GXUpdateArgs.Update(user);
            arg2.Exclude<GXUser>(q => new { q.PasswordHash, q.SecurityStamp, q.CreationTime });
            Assert.AreEqual("UPDATE [GXUser] SET [UserName] = 'Gurux', [NormalizedUserName] = NULL, [Email] = NULL, [NormalizedEmail] = NULL, [EmailConfirmed] = 0, [ConcurrencyStamp] = NULL, [PhoneNumber] = NULL, [PhoneNumberConfirmed] = 0, [TwoFactorEnabled] = 0, [LockoutEnd] = '00010101 00:00:00 +00:00', [LockoutEnabled] = 0, [AccessFailedCount] = 0, [Updated] = '00010101 00:00:00', [Detected] = '00010101 00:00:00', [Removed] = '00010101 00:00:00', [DateOfBirth] = NULL WHERE [ID] = '1'", arg2.ToString());
        }

        /// <summary>
        /// Add new User roles test.
        /// </summary>
        [TestMethod]
        public void AddUserRolesTest()
        {
            GXUser user = new GXUser();
            user.Id = "642c8f77-aeeb-4e86-86db-1a8b1b2fc982";
            user.UserName = "Gurux";
            List<string> roles = new List<string>();
            roles.Add("Admin");
            roles.Add("User");
            roles.Add("DeviceManager");
            roles.Add("SystemErrorManager");
            GXSelectArgs sel = GXSelectArgs.Select<GXRole>(q => q.Id);
            GXSelectArgs sel2 = GXSelectArgs.Select<GXUserRole>(q => q.UserId, q => q.UserId == "642c8f77-aeeb-4e86-86db-1a8b1b2fc982");
            sel.Where.And<GXRole>(q => !GXSql.Exists<GXRole, GXUserRole>(q => q.Id, q => q.RoleId, sel2));
            sel.Where.And<GXRole>(q => roles.Contains(q.Name));
            GXUserRole ur = new GXUserRole();
            ur.UserId = user.Id;
            GXInsertArgs i = GXInsertArgs.Insert(ur);
            i.Add<GXUserRole>(sel, q => q.RoleId);
            Assert.AreEqual("", i.ToString());
        }
        /// <summary>
        /// Remove User roles test.
        /// </summary>
        [TestMethod]
        public void RemoveUserRolesTest()
        {
            GXUser user = new GXUser();
            user.Id = "642c8f77-aeeb-4e86-86db-1a8b1b2fc982";
            user.UserName = "Gurux";
            List<string> roles = new List<string>();
            roles.Add("Admin");
            roles.Add("User");
            roles.Add("DeviceManager");
            roles.Add("SystemErrorManager");
            GXSelectArgs sel = GXSelectArgs.Select<GXRole>(q => GXSql.One);
            sel.Where.And<GXUserRole>(q => q.UserId == user.Id);
            sel.Where.And<GXRole>(q => !roles.Contains(q.Name));
            GXDeleteArgs d = GXDeleteArgs.Delete<GXUserRole>(q => q.UserId == user.Id);
            d.Where.And<GXRole>(q => GXSql.Exists<GXUserRole, GXRole>(q => q.RoleId, q => q.Id, sel));

            /*
            GXSelectArgs sel = GXSelectArgs.Select<GXUserRole>(q => q.UserId, q => q.UserId == "642c8f77-aeeb-4e86-86db-1a8b1b2fc982");
            sel.Joins.AddInnerJoin<GXUserRole, GXRole>(a => a.RoleId, b => b.Id);
            sel.Where.And<GXRole>(q => !roles.Contains(q.Name));
            GXDeleteArgs d = GXDeleteArgs.Delete<GXUserRole>(q => GXSql.Exists(sel));
            */
            Assert.AreEqual("", d.ToString());
        }

        /// <summary>
        /// Remove Users test.
        /// </summary>
        [TestMethod]
        public void RemoveUsersTest()
        {
            RemoveUser req = new RemoveUser();
            req.Ids = new string[] { Guid.Empty.ToString(), Guid.Empty.ToString() };
            //Set removed time for all the removed users.
            GXUser u = new GXUser() { Removed = DateTime.Now };
            GXUpdateArgs update = GXUpdateArgs.Update(u, q => q.Removed);
            update.Where.And<GXUser>(q => req.Ids.Contains(q.Id));
            Assert.AreEqual("", update.ToString());
        }

        /// <summary>
        /// Add settings test.
        /// </summary>
        [TestMethod]
        public void InsertSettingsTest()
        {
            List<GXConfigurationValue> list = new List<GXConfigurationValue>();
            list.Add(new GXConfigurationValue() { Name = "SiteName", });
            list.Add(new GXConfigurationValue() { Name = "Email", });
            list.Add(new GXConfigurationValue() { Name = "Slogan", });
            //            GXInsertArgs insert = GXInsertArgs.InsertRange(list, c => new { c.Id, c.Generation });
            GXInsertArgs insert = GXInsertArgs.InsertRange(list);
            insert.Exclude<GXConfigurationValue>(e => e.Group);
            Assert.AreEqual("Mikko", insert.ToString());
        }

        /// <summary>
        /// Update configuration test.
        /// </summary>
        [TestMethod]
        public void UpdateConfigurationTest()
        {
            List<GXConfigurationValue> list = new List<GXConfigurationValue>();
            list.Add(new GXConfigurationValue() { Id = Guid.NewGuid(), Name = "SiteName", });
            list.Add(new GXConfigurationValue() { Id = Guid.NewGuid(), Name = "Email", });
            list.Add(new GXConfigurationValue() { Id = Guid.NewGuid(), Name = "Slogan", });
            //            GXInsertArgs insert = GXInsertArgs.InsertRange(list, c => new { c.Id, c.Generation });
            GXUpdateArgs args = GXUpdateArgs.UpdateRange(list);
            args.Exclude<GXConfigurationValue>(e => e.Group);
            Assert.AreEqual("Mikko", args.ToString());
        }


        /// <summary>
        /// Add settings test.
        /// </summary>
        [TestMethod]
        public void CultureTest()
        {
            List<GXLanguage> list = new List<GXLanguage>();
            list.Add(new GXLanguage() { Id = "fi", EnglishName = "Finland", });
            GXInsertArgs insert = GXInsertArgs.InsertRange(list);
            insert.Exclude<GXConfigurationValue>(e => e.Group);
            Assert.AreEqual("Mikko", insert.ToString());
        }

        /// <summary>
        /// Add settings test.
        /// </summary>
        [TestMethod]
        public void CultureUpdateTest()
        {
            GXLanguage item = new GXLanguage() { Id = "fi", EnglishName = "Finland", };
            GXUpdateArgs args = GXUpdateArgs.Update(item);
            args.Exclude<GXLanguage>(e => e.EnglishName);
            Assert.AreEqual("Mikko", args.ToString());
        }

        /// <summary>
        /// GXUserGroup exclude test.
        /// </summary>
        [TestMethod]
        public void ExcludeTest()
        {
            GXUser user = new GXUser() { Id = "Default" };
            GXUserGroup item = new GXUserGroup() { Name = "Default"};
            item.Users.Add(user);
            GXInsertArgs insert = GXInsertArgs.Insert(item);
            insert.Exclude<GXUserGroup>(e => e.CreationTime);
            insert.Exclude<GXUserGroup>(e => e.Users);
            Assert.AreEqual("Mikko", insert.ToString());
        }

        /// <summary>
        /// GXUserGroup exclude test.
        /// </summary>
        [TestMethod]
        public void FilterTest()
        {
            GXUser user = new GXUser() { Id = "Gurux" };
            GXUserGroup userGroup = new GXUserGroup();
            userGroup.Users.Add(user);
            GXSelectArgs arg = GXSelectArgs.Select<GXUserGroup>(s => s.Id, where => where.Removed == null);
            arg.Where.FilterBy(userGroup, false);
            arg.Joins.AddInnerJoin<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupUser, GXUser>(j => j.UserId, j => j.Id);
            string[] userIds = new string[] {"Gurux"};
            arg.Where.And<GXUser>(where => where.Removed == null && userIds.Contains(where.Id));
            Assert.AreEqual("Mikko", arg.ToString());
        }

        /// <summary>
        /// GXUserGroup exclude test.
        /// </summary>
        [TestMethod]
        public void MultipleTablesTest()
        {
            GXUser user = new GXUser() { Id = "Gurux" };
            GXUserGroup userGroup = new GXUserGroup();
            userGroup.Users.Add(user);
            GXSelectArgs arg = GXSelectArgs.Select<GXUserGroup>(s => s.Id, where => where.Removed == null);
            arg.Where.FilterBy(userGroup, false);
            arg.Joins.AddInnerJoin<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupUser, GXUser>(j => j.UserId, j => j.Id);
            string[] userIds = new string[] { "Gurux" };
            arg.Where.And<GXUser>(where => where.Removed == null && userIds.Contains(where.Id));
            Assert.AreEqual("Mikko", arg.ToString());
        }

        /// <summary>
        /// IP test.
        /// </summary>
        [TestMethod]
        public void IPTest()
        {
            GXUser user = new GXUser();
            user.Id = "Gurux";
            //GXSelectArgs args = GXSelectArgs.Select<GXIpAddress>(s => s.Id, where => where.User == user && where.IPAddress == 0);
            GXSelectArgs args = GXSelectArgs.Select<GXIpAddress>(s => s.Id, where => where.User == user);
            Assert.AreEqual("Mikko", args.ToString());

        }

        [TestMethod]
        public void MikkoTest()
        {
            GXWorkflow workflow = new GXWorkflow();
            workflow.Id = Guid.Parse("17f9f275-7dba-44a8-b9c5-5c404283cf7e");
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXJob>(q => q.Workflow == workflow && q.Removed == null);
            arg.Columns.Add<GXModule>();
            arg.Columns.Exclude<GXModule>(e => e.Jobs);
            arg.Joins.AddLeftJoin<GXModule, GXJob>(j => j.Id, j => j.Module);
            Assert.AreEqual("Mikko", arg.ToString());
        }

        [TestMethod]
        public void Mikko2Test()
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXScript>(q => q.Removed == null);
            Assert.AreEqual("Mikko", arg.ToString());
        }
    }
}
