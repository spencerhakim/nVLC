using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Declarations
{
    /// <summary>
    /// 
    /// </summary>
    public class AspectRatio
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="numerator"></param>
        /// <param name="denumerator"></param>
        public AspectRatio(int numerator, int denumerator)
        {
            Numerator = numerator;
            Denumerator = denumerator;
        }

        /// <summary>
        /// 
        /// </summary>
        public int Numerator { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public int Denumerator { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public double Ratio
        {
            get
            {
                if (Numerator == 0 || Denumerator == 0)
                {
                    return 0;
                }

                return Numerator / (double)Denumerator;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static AspectRatio Default 
        { 
            get
            {
                return new AspectRatio(0, 0);
            }
        }
    }
}
