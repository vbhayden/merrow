using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Merrow.Util
{
    public static class StringUtil
    {
        public static string ReplaceAt(this string str, int index, string replacement)
        {
            var replacementLength = replacement.Length;
            return str.Remove(index, replacementLength).Insert(index, replacement);
        }
    }
}
