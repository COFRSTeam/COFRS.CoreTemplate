using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COFRSCoreCommon.Utilities
{
    public static class StringExtensions
    {
        public static string ToCSV(this string[] input)
        {
            StringBuilder result = new StringBuilder();
            bool first = true;

            foreach ( var str in input)
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
