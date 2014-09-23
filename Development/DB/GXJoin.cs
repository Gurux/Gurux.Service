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

using System.Text;
namespace Gurux.Service.Db
{
    /// <summary>
    /// Join helper class for internal use only.
    /// </summary>
    class GXJoin
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXJoin()
        {
        }

        /// <summary>
        /// Join type.
        /// </summary>
        public JoinType Type;
        /// <summary>
        /// Table 1 name.
        /// </summary>
        public string Table1;
        /// <summary>
        /// Table 2 name.
        /// </summary>
        public string Table2;
        /// <summary>
        /// Column 1 name.
        /// </summary>       
        public string Column1;
        /// <summary>
        /// Column 2 name.
        /// </summary>        
        public string Column2;

        /// <summary>
        /// Column 1 alias name.
        /// </summary>        
        public string Alias1;

        /// <summary>
        /// Column 2 alias name.
        /// </summary>        
        public string Alias2;

        /// <summary>
        /// If we have multiple references to same table.
        /// </summary>
        public int Index;

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            if (Alias1 != null)
            {
                sb.Append(Alias1);
                sb.Append('[');
                sb.Append(Table1);
                sb.Append("].");
                sb.Append(Column1);
            }
            else
            {
                sb.Append(Table1);
                sb.Append('.');
                sb.Append(Column1);
            }
            if (Alias2 != null)
            {
                sb.Append(Alias2);
                sb.Append('[');
                sb.Append(Table2);
                sb.Append("].");
                sb.Append(Column2);
            }
            else
            {
                sb.Append(Table2);
                sb.Append('.');
                sb.Append(Column2);
            }
            return sb.ToString();
        }
    }
}
