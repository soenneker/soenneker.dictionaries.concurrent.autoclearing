using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Soenneker.Dictionaries.Concurrent.AutoClearing.Abstract;

/// <summary>
/// Represents a concurrent, auto-clearing key/value store whose contents are periodically cleared
/// by atomically swapping the underlying dictionary. Provides common concurrent dictionary
/// operations along with both snapshot and live enumeration options.
/// </summary>
/// <typeparam name="TKey">The type of keys in the dictionary. Must be non-nullable.</typeparam>
/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
public interface IAutoClearingConcurrentDictionary<TKey, TValue> : IAsyncDisposable, IDisposable where TKey : notnull
{
    /// <summary>
    /// Immediately clears the dictionary by atomically swapping in a new empty instance.
    /// This is <c>O(1)</c> and does not iterate over existing entries.
    /// </summary>
    void Clear();

    /// <summary>
    /// Attempts to add the specified key and value to the dictionary.
    /// </summary>
    /// <param name="key">The key of the element to add.</param>
    /// <param name="value">The value of the element to add.</param>
    /// <returns><see langword="true"/> if the key/value pair was added; <see langword="false"/> if the key already exists.</returns>
    bool TryAdd(TKey key, TValue value);

    /// <summary>
    /// Returns the value for the specified key. If the key does not exist, the value is created by
    /// <paramref name="valueFactory"/> and added atomically.
    /// </summary>
    /// <param name="key">The key of the value to get or add.</param>
    /// <param name="valueFactory">A factory to produce the value if the key is not present.</param>
    /// <returns>The existing or newly added value associated with <paramref name="key"/>.</returns>
    TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory);

    /// <summary>
    /// Returns the value for the specified key. If the key does not exist, <paramref name="value"/> is added atomically.
    /// </summary>
    /// <param name="key">The key of the value to get or add.</param>
    /// <param name="value">The value to add if the key is not present.</param>
    /// <returns>The existing or newly added value associated with <paramref name="key"/>.</returns>
    TValue GetOrAdd(TKey key, TValue value);

    /// <summary>
    /// Adds a key/value pair to the dictionary if the key does not already exist, or updates a key/value pair
    /// if the key already exists.
    /// </summary>
    /// <param name="key">The key to add or update.</param>
    /// <param name="addFactory">A function used to generate a value for an absent key.</param>
    /// <param name="updateFactory">A function used to generate a new value for an existing key.</param>
    /// <returns>The new value for the key.</returns>
    TValue AddOrUpdate(TKey key, Func<TKey, TValue> addFactory, Func<TKey, TValue, TValue> updateFactory);

    /// <summary>
    /// Attempts to get the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key of the value to get.</param>
    /// <param name="value">When this method returns, contains the value associated with <paramref name="key"/>,
    /// if the key is found; otherwise, the default value for <typeparamref name="TValue"/>.</param>
    /// <returns><see langword="true"/> if the key was found; otherwise, <see langword="false"/>.</returns>
    bool TryGetValue(TKey key, out TValue value);

    /// <summary>
    /// Attempts to remove and return the value with the specified key.
    /// </summary>
    /// <param name="key">The key of the element to remove.</param>
    /// <param name="value">When this method returns, contains the removed value, if the key was found.</param>
    /// <returns><see langword="true"/> if the element was removed; otherwise, <see langword="false"/>.</returns>
    bool TryRemove(TKey key, out TValue value);

    /// <summary>
    /// Determines whether the dictionary contains the specified key.
    /// </summary>
    /// <param name="key">The key to locate.</param>
    /// <returns><see langword="true"/> if the dictionary contains an element with the specified key; otherwise, <see langword="false"/>.</returns>
    bool ContainsKey(TKey key);

    /// <summary>
    /// Gets the current number of elements contained in the dictionary.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Gets an enumerable view over the dictionary for low-allocation iteration.
    /// <para>
    /// This is a live view (not a snapshot) and may reflect updates occurring during enumeration.
    /// </para>
    /// </summary>
    IEnumerable<KeyValuePair<TKey, TValue>> Items { get; }

    /// <summary>
    /// Returns a point-in-time snapshot of the dictionary as a new array.
    /// </summary>
    /// <returns>An array containing a snapshot of the current key/value pairs.</returns>
    KeyValuePair<TKey, TValue>[] ToArray();

    /// <summary>
    /// Asynchronously disposes the dictionary, stopping the periodic clearing mechanism and promptly
    /// releasing references by swapping in a fresh empty instance.
    /// </summary>
    /// <returns>A task that represents the asynchronous dispose operation.</returns>
    new ValueTask DisposeAsync();
}