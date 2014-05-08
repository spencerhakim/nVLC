using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Declarations.Structures
{
    /// <summary>
    /// 
    /// </summary>
    public class SlaveMedia
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="mrl"></param>
        /// <param name="caching"></param>
        /// <param name="startTime"></param>
        public SlaveMedia(string mrl, int caching = 0, double startTime = 0)
        {
            MRL = mrl;
            Caching = caching;
            StartTime = startTime;
        }
        /// <summary>
        /// 
        /// </summary>
        public int Caching { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public double StartTime { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public string MRL { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat(":input-slave={0}", MRL);
            if (Caching > 0)
            {
                sb.AppendFormat(" :file-caching={0}", Caching);
            }
            if (StartTime > 0)
            {
                sb.AppendFormat(" :start-time={0}", StartTime);
            }

            return sb.ToString();
        }
    }
}
