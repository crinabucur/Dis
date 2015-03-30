using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudStorage
{
    class Utils
    {
        internal static string FormatQuota(long bytes)
        {
            if (bytes < 1024)
            {
                // B
                return bytes + "B";
            }
            if (bytes >= 1024 && bytes < 1024*1024)
            {
                // kB
                return Math.Round(bytes / (double)(1024), 1) + "kB";
            }
            if (bytes >= 1024*1024 && bytes < 1024*1024*1024)
            {
                // MB
                return Math.Round(bytes / (double)(1024 * 1024), 1) + "MB";
            }
            
            // GB
            return Math.Round(bytes / (double)(1024 * 1024 * 1024), 1) + "GB";
        }
    }
}
