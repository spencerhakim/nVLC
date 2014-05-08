//    nVLC
//    
//    Author:  Roman Ginzburg
//
//    nVLC is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.
//
//    nVLC is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//    GNU General Public License for more details.
//     
// ========================================================================

using LibVlcWrapper;
using System;
using System.Text;

namespace Implementation.Utils
{
    internal class MiscUtils
    {
        public static string DwordToFourCC(uint fourCC)
        {
            char[] chars = new char[4];
            chars[0] = (char)(fourCC & 0xFF);
            chars[1] = (char)((fourCC >> 8) & 0xFF);
            chars[2] = (char)((fourCC >> 16) & 0xFF);
            chars[3] = (char)((fourCC >> 24) & 0xFF);
            return new string(chars);
        }

        public static string GetMinimalSupportedVersion(EntryPointNotFoundException ex)
        {
            MinimalLibVlcVersion minVersion = (MinimalLibVlcVersion)Attribute.GetCustomAttribute(ex.TargetSite, typeof(MinimalLibVlcVersion));
            if (minVersion != null)
            {
                return minVersion.MinimalVersion;
            }

            return string.Empty;
        }

        public static T FindNestedException<T>(Exception source) where T : Exception
        {
            if (source.GetType() == typeof(T))
            {
                return (T)source;
            }

            while (source.InnerException != null)
            {
                return FindNestedException<T>(source.InnerException);
            }

            return default(T);
        }

        public static string LogNestedException(Exception ex)
        {
            StringBuilder sb = new StringBuilder();
            Exception error = ex;
            do
            {
                sb.AppendLine(error.Message);
                error = error.InnerException;
            }
            while (error.InnerException != null);
            return sb.ToString();
        }
    }
}
