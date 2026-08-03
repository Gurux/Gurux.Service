namespace Gurux.Service.Orm.Model
{
    /// <summary>
    /// Import settings.
    /// </summary>
    public class GXGeneratorSettings
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXGeneratorSettings()
        {
            Serializable = true;
        }

        /// <summary>
        /// Directory where tables are generated.
        /// </summary>
        public string Path
        {
            get;
            set;
        }

        /// <summary>
        /// Imported tables.
        /// </summary>
        /// <remarks>
        /// If tables is null all tables are imported.</remarks>
        public string[] Tables
        {
            get;
            set;
        }

        /// <summary>
        /// Namespace
        /// </summary>
        public string Namespace
        {
            get;
            set;
        }

        /// <summary>
        /// Amount of spaces. Tab is used if zero. 
        /// </summary>
        public int Spaces
        {
            get;
            set;
        }

        /// <summary>
        /// Table prefix. 
        /// </summary>
        public string TablePrefix
        {
            get;
            set;
        }

        /// <summary>
        /// Column prefix. 
        /// </summary>
        public string ColumnPrefix
        {
            get;
            set;
        }

        /// <summary>
        /// Is Serializable attribute added.
        /// </summary>
        public bool Serializable
        {
            get;
            set;
        }


    }
}
