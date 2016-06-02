using System;
using System.Collections.Generic;

namespace SimpleMapper
{
    public static class Utility
    {
        public static bool IsSimpleType(this Type type)
        {
            if (type.IsPrimitive || type == typeof(Decimal) || type == typeof(String) || type == typeof(string[]))
            {
                return true;
            }

            return false;
        }

        public static bool IsEnumerable(this Type type)
        {
            return type.GetInterface(typeof(IEnumerable<>).FullName) != null;
        }
    }

}
