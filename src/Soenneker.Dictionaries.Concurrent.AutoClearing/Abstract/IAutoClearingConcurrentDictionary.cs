using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Soenneker.Dictionaries.Concurrent.AutoClearing.Abstract;

/// <summary>
/// Represents a high-performance concurrent key/value store whose contents are
/// periodically cleared on a timer.
/// <para>
/// Clearing is performed using an adaptive strategy:
/// small dictionaries are cleared in-place, while large dictionaries are
/// atomically replaced to avoid O(N) work on the timer thread.
/// </para>
/// <para>
/// All operations are thread-safe and designed for low allocation overhead.
/// </para>
/// </summary>
/// <typeparam name="TKey">
/// The type of keys in the dictionary. Must be non-null.
/// </typeparam>
/// <typeparam name="TValue">
/// The type of values stored in the dictionary.
/// </typeparam>
public interface IAutoClearingConcurrentDictionary<TKey, TValue> : IDisposable, IAsyncDisposable where TKey : notnull
{
    /// <summary>
    /// Immediately clears the dictionary.
    /// <para>
    /// The implementation may either clear in-place or atomically swap the
    /// underlying dictionary depending on its current size.
    /// </para>
    /// <para>
    /// This method is thread-safe and may run concurrently with other operations.
    /// </para>
    /// </summary>
    void Clear();

    /// <summary>
    /// Attempts to add the specified key and value to the dictionary.
    /// </summary>
    /// <param name="key">The key of the element to add.</param>
    /// <param name="value">The value of the element to add.</param>
    /// <returns>
    /// <see langword="true"/> if the key/value pair was added;
    /// <see langword="false"/> if the key already exists.
    /// </returns>
    bool TryAdd(TKey key, TValue value);

    /// <summary>
    /// Gets the value associated with the specified key. If the key does not exist,
    /// the value is created by <paramref name="valueFactory"/> and added atomically.
    /// </summary>
    /// <param name="key">The key of the value to get or add.</param>
    /// <param name="valueFactory">A factory to produce the value if the key is not present.</param>
    /// <returns>
    /// The existing or newly added value associated with <paramref name="key"/>.
    /// </returns>
    TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory);

    /// <summary>
    /// Gets the value associated with the specified key. If the key does not exist,
    /// <paramref name="value"/> is added atomically.
    /// </summary>
    /// <param name="key">The key of the value to get or add.</param>
    /// <param name="value">The value to add if the key is not present.</param>
    /// <returns>
    /// The existing or newly added value associated with <paramref name="key"/>.
    /// </returns>
    TValue GetOrAdd(TKey key, TValue value);

    /// <summary>
    /// Adds a key/value pair if the key does not already exist, or updates
    /// the value for an existing key.
    /// </summary>
    /// <param name="key">The key to add or update.</param>
    /// <param name="addFactory">A function used to generate a value for an absent key.</param>
    /// <param name="updateFactory">A function used to generate a new value for an existing key.</param>
    /// <returns>The resulting value stored for the specified key.</returns>
    TValue AddOrUpdate(TKey key, Func<TKey, TValue> addFactory, Func<TKey, TValue, TValue> updateFactory);

    /// <summary>
    /// Attempts to retrieve the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key of the value to retrieve.</param>
    /// <param name="value">
    /// When this method returns, contains the value associated with
    /// <paramref name="key"/> if the key is found; otherwise,
    /// the default value for <typeparamref name="TValue"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the key was found; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    bool TryGetValue(TKey key, out TValue value);

    /// <summary>
    /// Attempts to remove the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key of the element to remove.</param>
    /// <param name="value">
    /// When this method returns, contains the removed value if the key was found.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the element was removed;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    bool TryRemove(TKey key, out TValue value);

    /// <summary>
    /// Determines whether the dictionary contains the specified key.
    /// </summary>
    /// <param name="key">The key to locate.</param>
    /// <returns>
    /// <see langword="true"/> if the dictionary contains the specified key;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    bool ContainsKey(TKey key);

    /// <summary>
    /// Gets the approximate number of elements currently contained in the dictionary.
    /// </summary>
    /// <remarks>
    /// The returned value may change immediately due to concurrent operations.
    /// </remarks>
    int Count { get; }

    /// <summary>
    /// Gets a live enumerable view over the dictionary.
    /// </summary>
    /// <remarks>
    /// This is not a snapshot. The sequence may reflect concurrent modifications
    /// and may enumerate elements that are later cleared.
    /// </remarks>
    IEnumerable<KeyValuePair<TKey, TValue>> Items { get; }

    /// <summary>
    /// Returns a point-in-time snapshot of the dictionary as a new array.
    /// </summary>
    /// <returns>
    /// An array containing a snapshot of the current key/value pairs.
    /// </returns>
    KeyValuePair<TKey, TValue>[] ToArray();

    /// <summary>
    /// Asynchronously disposes the dictionary, stopping the periodic clearing mechanism.
    /// </summary>
    /// <returns>
    /// A <see cref="ValueTask"/> representing the asynchronous dispose operation.
    /// </returns>
    new ValueTask DisposeAsync();
}