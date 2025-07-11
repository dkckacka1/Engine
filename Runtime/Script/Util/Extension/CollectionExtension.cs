using System.Collections.Generic;

namespace Engine.Util.Extension
{
    public static class CollectionExtension
    {
        public static IEnumerable<T> ListLoop<T>(this IList<T> values)
        {
            while (values.Count > 0)
            {
                foreach (var value in values)
                {
                    yield return value;
                }
            }
        }

        public static IEnumerable<T> ListLoop<T>(this IList<T> values, int loopCount)
        {
            var count = 0;

            while (count < loopCount)
            {
                foreach (var value in values)
                {
                    yield return value;
                }

                ++count;
            }
        }
    }
}