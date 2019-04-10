using System.Collections;
using System.Collections.Generic;

namespace Network.Extensions
{
    /// <summary>
    /// Provides additional functionality to the <see cref="IEnumerator"/> interface.
    /// </summary>
    internal static class EnumeratorExtensions
    {
        #region Methods

        /// <summary>
        /// Adds each item in the <see cref="IEnumerator"/> into a <see cref="List{T}"/> and return the new <see cref="List{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the <see cref="List{T}"/>.</typeparam>
        /// <param name="enumerator">The <see cref="IEnumerator"/> instance that the extension method affects.</param>
        /// <returns>The <see cref="List{T}"/> instance with the elements of the <see cref="IEnumerator"/>.</returns>
        internal static List<T> ToList<T>(this IEnumerator enumerator)
        {
            List<T> collection = new List<T>();

            while (enumerator.MoveNext())
            {
                collection.Add((T)enumerator.Current);
            }

            return collection;
        }

        #endregion Methods
    }
}