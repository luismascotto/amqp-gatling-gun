using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueueProcessor.Worker.Extension;

internal static class Strings
{
    public static bool IsNullOrEmpty(this string str)
    {
        return string.IsNullOrEmpty(str);
    }
    public static bool IsNullOrWhiteSpace(this string str)
    {
        return string.IsNullOrWhiteSpace(str);
    }
    public static bool IsSomething(this string str)
    {
        return !string.IsNullOrWhiteSpace(str);
    }

    public static int LenghtSafe(this string str)
    {
        if (str is null)
        {
            return 0;
        }

        return str.Length;
    }
   
}
