using System;
using System.Security.Permissions;
using System.Runtime.Serialization;

namespace Implementation.Exceptions
{
    /// <summary>
    /// Represents error that occured due to removal of libVLC module
    /// </summary>
    [Serializable]
    public class LibVlcRemovedModuleException : Exception
    {
        /// <summary>
        /// Initializes new instance of the class
        /// </summary>
        /// <param name="libVlcModuleName"></param>
        /// <param name="nVlcModuleName"></param>
        /// <param name="lastWorkingVersion"></param>
        public LibVlcRemovedModuleException(string libVlcModuleName, string nVlcModuleName, string lastWorkingVersion)
        {
            LibVlcModuleName = libVlcModuleName;
            LastWorkingVersion = lastWorkingVersion;
            this.nVlcModuleName = nVlcModuleName;
        }

        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        protected LibVlcRemovedModuleException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            LibVlcModuleName = info.GetString("LibVlcModuleName");
            LastWorkingVersion = info.GetString("LastWorkingVersion");
            nVlcModuleName = info.GetString("nVlcModuleName");
        }

        /// <summary>
        /// Name of the libVLC module
        /// </summary>
        public string LibVlcModuleName { get; private set; }

        /// <summary>
        /// Last version where module was operational
        /// </summary>
        public string LastWorkingVersion { get; private set; }

        /// <summary>
        /// Name of the nVLC object using removed module
        /// </summary>
        public string nVlcModuleName { get; private set; }

        /// <summary>
        /// Gets a message that describes the current exception
        /// </summary>
        public override string Message
        {
            get
            {
                return string.Format("{0} module based on {1} supported up to libVLC version {2}", nVlcModuleName, LibVlcModuleName, LastWorkingVersion);
            }
        }

        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

            info.AddValue("LibVlcModuleName", LibVlcModuleName);
            info.AddValue("LastWorkingVersion", LastWorkingVersion);
            info.AddValue("nVlcModuleName", nVlcModuleName);

            base.GetObjectData(info, context);
        }
    }
}
