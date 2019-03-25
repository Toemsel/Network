using System.Collections.Concurrent;

namespace Network.Extensions
{
    /// <summary>
    /// https://stackoverflow.com/a/48861922/2934290
    /// </summary>
    public static class ConcurrentBagExtensions
    {
        public static void Remove<T>(this ConcurrentBag<T> bag, T item)
        {
            while (bag.Count > 0)
            {
                T result;
                bag.TryTake(out result);

                if (result.Equals(item))
                {
                    break;
                }

                bag.Add(result);
            }
        }
    }
}