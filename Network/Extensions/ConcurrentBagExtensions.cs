using System.Collections.Concurrent;

namespace Network.Extensions
{
    /// <summary>
    /// Provides additional functionality to the <see cref="ConcurrentBag{T}"/>
    /// class.
    /// </summary>
    /// <remarks>
    /// See https://stackoverflow.com/a/48861922/2934290 for the original question
    /// and the relevant code.
    /// </remarks>
    public static class ConcurrentBagExtensions
    {
        #region Methods

        /// <summary>
        /// Removes the given item from the <see cref="ConcurrentBag{T}"/>.
        /// </summary>
        /// <typeparam name="T">
        /// The type for the elements held in the <see cref="ConcurrentBag{T}"/>.
        /// </typeparam>
        /// <param name="bag">
        /// The <see cref="ConcurrentBag{T}"/> instance that the extension method
        /// should affect.
        /// </param>
        /// <param name="item">
        /// The item to remove from the bag.
        /// </param>
        public static void Remove<T>(this ConcurrentBag<T> bag, T item)
        {
            while (bag.Count > 0)
            {
                bag.TryTake(out T result);

                if (result.Equals(item))
                {
                    break;
                }

                bag.Add(result);
            }
        }

        #endregion Methods
    }
}