using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COFRS.Template.Common.ServiceUtilities
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
    }
}
