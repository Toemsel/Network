using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Network.Utilities
{
    /// <summary>
    /// Provides methods for the generation of unique identifiers for objects.
    /// </summary>
    internal static class UidGenerator
    {
        #region Variables

        /// <summary>
        /// Maps a <see cref="Type"/> to its cached, unique ID via a thread-safe dictionary.
        /// </summary>
        private static readonly ConcurrentDictionary<Type, object> typeToIdMap = new ConcurrentDictionary<Type, object>();

        #endregion Variables

        #region Methods

        /// <summary>
        /// Generates a unique identifier for the given
        /// </summary>
        /// <typeparam name="T">The type for which to generate a unique ID.</typeparam>
        /// <returns>The unique ID.</returns>
        internal static T GenerateUid<T>()
        {
            Type type = typeof(T);

            typeToIdMap.AddOrUpdate(type, default(T), (_, value) =>
                {
                    dynamic currentValue = value;
                    return ++currentValue;
                });

            return (T)typeToIdMap[type];
        }

        /// <summary>
        /// Returns the unique identifier associated with the given type.
        /// </summary>
        /// <typeparam name="T">The type whose ID to get.</typeparam>
        /// <returns>The unique ID.</returns>
        /// <exception cref="KeyNotFoundException">
        /// Thrown if the method is called before an ID is present. This occurs if a previous call to
        /// <see cref="GenerateUid{T}"/> was not made
        /// for the given type, and thus no ID actually exists.
        /// </exception>
        internal static T LastGeneratedUid<T>()
        {
            Type type = typeof(T);

            //If we call the LastGeneratedUid before we actually filled the dictionary, there was no last UID.
            //Therefore exception.
            if (!typeToIdMap.ContainsKey(type))
            {
                throw new KeyNotFoundException();
            }

            return (T)typeToIdMap[type];
        }

        #endregion Methods
    }
}