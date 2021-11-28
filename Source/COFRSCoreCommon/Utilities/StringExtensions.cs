using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COFRSCoreCommon.Utilities
{
    public static class StringExtensions
    {
        public static int CountOf(this string str, char c)
        {
            var theCount = 0;

            foreach (var chr in str)
                if (chr == c)
                    theCount++;

            return theCount;
        }

        public static string GetBaseColumn(this string str)
        {
            var parts = str.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            StringBuilder theBase = new StringBuilder();

            for (int i = 0; i < parts.Count() - 1; i++)
            {
                if (theBase.Length > 0)
                    theBase.Append(".");
                theBase.Append(parts[i]);
            }

            return theBase.ToString();
        }

        public static string ToCSV(this string[] input)
        {
            StringBuilder result = new StringBuilder();
            bool first = true;

            foreach (var str in input)
            {
                if (first)
                    first = false;
                else
                    result.Append(',');

                result.Append(str);
            }

            return result.ToString();
        }
    }
}
