using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Network.Utilities
{
    /// <summary>
    /// Provides a dictionary capable of looking up both a key or a value. Otherwise it is the same as a regular dictionary.
    /// See https://en.wikipedia.org/wiki/Hash_table for a better explanation of the concepts behind the dictionary.
    /// </summary>
    /// <typeparam name="TKey">The type of the key stored in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of the value stored in the dictionary.</typeparam>
    internal class BiDictionary<TKey, TValue>
    {
        #region Variables

        /// <summary>
        /// The internal dictionary, storing the keys and values for the bi-directional dictionary.
        /// </summary>
        private readonly Dictionary<TKey, TValue> dictionary;

        #endregion Variables

        #region Constructors

        /// <summary>
        /// Constructs an empty dictionary.
        /// </summary>
        internal BiDictionary()
        {
            dictionary = new Dictionary<TKey, TValue>();
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// The keys held in the dictionary.
        /// </summary>
        internal ICollection<TKey> Keys
        {
            get
            {
                return dictionary.Keys;
            }
        }

        /// <summary>
        /// The values held in the dictionary.
        /// </summary>
        internal ICollection<TValue> Values
        {
            get
            {
                return dictionary.Values;
            }
        }

        #endregion Properties

        #region Methods

        #region Checking If Exists

        /// <summary>
        /// Checks whether or not the dictionary contains the given key.
        /// </summary>
        /// <param name="key">The key to check for.</param>
        /// <returns>Whether or not the key exists.</returns>
        internal bool ContainsKey(TKey key)
        {
            return dictionary.ContainsKey(key);
        }

        /// <summary>
        /// Checks whether or not the dictionary contains the given value.
        /// </summary>
        /// <param name="value">The value to check for.</param>
        /// <returns>Whether or not the value exists.</returns>
        internal bool ContainsValue(TValue value)
        {
            return dictionary.ContainsValue(value);
        }

        #endregion Checking If Exists

        #region Getting and Setting

        /// <summary>
        /// Tries to get the value associated with the given key, returning 'true' if a value was found, or 'false' if it wasn't.
        /// </summary>
        /// <param name="key">The key for whose value to search.</param>
        /// <param name="value">The value, if it was found. Otherwise the default for its type.</param>
        /// <returns>Whether or not the given key had a value.</returns>
        internal bool TryGetValue(TKey key, out TValue value)
        {
            try
            {
                value = default(TValue);

                if (dictionary.ContainsKey(key))
                {
                    value = dictionary[key];

                    return true;
                }

                return false;
            }
            catch (Exception)
            {
                value = default(TValue);
                return false;
            }
        }

        /// <summary>
        /// Tries to set the value associated with the given key, returning 'true' if the value was set correctly, or
        /// 'false' if it wasn't.
        /// </summary>
        /// <param name="key">The key whose value to set.</param>
        /// <param name="value">The value to set for the given key.</param>
        /// <returns>Whether or not the given key was set to the given value.</returns>
        internal bool TrySetValue(TKey key, TValue value)
        {
            try
            {
                dictionary[key] = value;

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Tries to get the key associated with the given value, returning 'true' if a value was found, or 'false' if it wasn't.
        /// </summary>
        /// <param name="value">The value for whose key to search.</param>
        /// <param name="key">The key, if it was found. Otherwise the default for its type.</param>
        /// <returns>Whether or not the given value had a key.</returns>
        internal bool TryGetKey(TValue value, out TKey key)
        {
            try
            {
                bool foundKeyForValue = false;
                TKey foundKey = default(TKey);

                if (dictionary.ContainsValue(value))
                {
                    Parallel.ForEach(dictionary.Keys, key_ =>
                    {
                        if (dictionary[key_].Equals(value))
                        {
                            foundKey = key_;

                            foundKeyForValue = true;
                        }
                    });
                }

                key = foundKey;
                return foundKeyForValue;
            }
            catch (Exception)
            {
                key = default(TKey);
                return false;
            }
        }

        /// <summary>
        /// Tries to set the key associated with the given value, returning 'true' if the key was set correctly, or 'false' if it wasn't.
        /// </summary>
        /// <param name="key">The value whose key to set.</param>
        /// <param name="value">The key to set for the given value.</param>
        /// <returns>Whether or not the given value had its key set to the given key.</returns>
        internal bool TrySetKey(TValue value, TKey key)
        {
            try
            {
                bool foundKeyForValue = false;
                TKey foundKey = default(TKey);

                if (dictionary.ContainsValue(value))
                {
                    Parallel.ForEach(dictionary.Keys, key_ =>
                    {
                        if (dictionary[key_].Equals(value))
                        {
                            foundKey = key_;

                            foundKeyForValue = true;
                        }
                    });
                }

                //If the value has a key in the dictionary, we need to remove it and replace it with the new key
                if (foundKeyForValue)
                {
                    dictionary.Remove(foundKey);
                }

                dictionary[key] = value;

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion Getting and Setting

        #endregion Methods

        #region Indexers

        /// <summary>
        /// Allows for the getting and setting of a value via its key.
        /// </summary>
        /// <param name="key_">The key whose value to get or set.</param>
        /// <returns>The value of the specified key.</returns>
        internal TValue this[TKey key_]
        {
            get
            {
                if (TryGetValue(key_, out TValue value_))
                {
                    return value_;
                }

                return default(TValue);
            }
            set
            {
                TrySetValue(key_, value);
            }
        }

        /// <summary>
        /// Allows for the getting and setting of a key via its value.
        /// </summary>
        /// <param name="value_">The value whose key to get or set.</param>
        /// <returns>The key of the specified value.</returns>
        internal TKey this[TValue value_]
        {
            get
            {
                if (TryGetKey(value_, out TKey key_))
                {
                    return key_;
                }

                return default(TKey);
            }
            set
            {
                TrySetKey(value_, value);
            }
        }

        #endregion Indexers
    }
}