using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmplaceParameters.Extensions
{
    public static class LinqExtensions
    {
        public static int IndexOf<T>(this IEnumerable<T> source, Predicate<T> filter)
        {
            var index = 0;
            foreach (var item in source)
            {
                if (filter(item))
                    return index;

                ++index;
            }

            return -1;
        }

        public static int IndexOf<T>(this IEnumerable<T> source, T value)
        {
            if (source is List<T> list)
                return list.IndexOf(value);

            var index = 0;
            foreach (var item in source)
            {
                if (EqualityComparer<T>.Default.Equals(item, value))
                    return index;

                ++index;
            }

            return -1;
        }
    }
}
