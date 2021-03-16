using System;
using System.IO;

namespace TSMPModdedSIILauncher
{
    public static class Utils
    {
        public static string Substring(this string str, string from, string to)
        {
            int pFrom = str.IndexOf(from) + from.Length;
            int pTo = str.IndexOf(to);
            return str.Substring(pFrom, pTo - pFrom);
        }
    }
}
