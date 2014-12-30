using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gurux.Common.JSon;

namespace Gurux.Service.Rest
{
    /// <summary>
    /// Application class is singleton.
    /// </summary>
    public abstract class GXAppHost
    {        
        internal static GXAppHost m_Instance;

        public GXAppHost()
        {
            m_Instance = this;
        }       

        public static GXAppHost Instance()
        {
            if (m_Instance == null)
            {
                throw new Exception("Class is not derived from GXAppHost");
            }
            return m_Instance;
        }
    }
}
