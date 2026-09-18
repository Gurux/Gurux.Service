//
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

namespace Gurux.Service.Orm.Common.Enums
{
    /// <Summary>
    /// Available database types.
    /// </Summary>
    public enum DatabaseType
    {
        /// <Summary>
        /// Target database is MySQL or Maria DB.
        /// </Summary>
        MySQL,
        /// <Summary>
        /// Target database is Microsoft SQL.
        /// </Summary>
        MSSQL,
        /// <Summary>
        /// Target database is SQLite.
        /// </Summary>
        /// <remarks>
        /// http://www.sqlite.org
        /// </remarks>
        SqLite,
        /// <Summary>
        /// Target database is Oracle.
        /// </Summary>        
        Oracle,
        /// <Summary>
        /// Target database is PostgreSQL.
        /// </Summary>
        PostgreSQL,
        /// <Summary>
        /// Target database is MariaDB.
        /// </Summary>
        MariaDB,
        /// <Summary>
        /// Target database is IBM DB2.
        /// </Summary>
        DB2,
        /// <Summary>
        /// Target database is SAP HANA.
        /// </Summary>
        SapHana
    }
}
