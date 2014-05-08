using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Implementation.Exceptions
{
    /// <summary>
    /// Represents error that occured due to removal of libVLC module
    /// </summary>
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
    }
}
