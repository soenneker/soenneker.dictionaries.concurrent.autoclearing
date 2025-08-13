using Soenneker.Extensions.ValueTask;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.Utils.AtomicBool;
using Soenneker.Dictionaries.Concurrent.AutoClearing.Abstract;

namespace Soenneker.Dictionaries.Concurrent.AutoClearing;

///<inheritdoc cref="IAutoClearingConcurrentDictionary{TKey, TValue}"/>
public sealed class AutoClearingConcurrentDictionary<TKey, TValue> : IAutoClearingConcurrentDictionary<TKey, TValue> where TKey : notnull
{
    private ConcurrentDictionary<TKey, TValue> _dict;

    private readonly Timer _timer;
    private readonly IEqualityComparer<TKey>? _comparer;

    private readonly int _concurrencyLevel;
    private readonly int _capacity;
    private readonly AtomicBool _ticking = new();

    // Static callback to avoid per-tick allocations
    private static readonly TimerCallback _sTimerCb = static state => ((AutoClearingConcurrentDictionary<TKey, TValue>) state!).Tick();

    public AutoClearingConcurrentDictionary(TimeSpan clearInterval, int concurrencyLevel = 0, int capacity = 31, IEqualityComparer<TKey>? comparer = null)
    {
        if (clearInterval <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(clearInterval));

        _concurrencyLevel = concurrencyLevel > 0 ? concurrencyLevel : Environment.ProcessorCount;
        _capacity = capacity > 0 ? capacity : 31;
        _comparer = comparer;

        _dict = new ConcurrentDictionary<TKey, TValue>(_concurrencyLevel, _capacity, _comparer);
        _timer = new Timer(_sTimerCb, this, clearInterval, clearInterval);
    }

    // Fast O(1) swap instead of O(N) Clear()
    private void Tick()
    {
        if (!_ticking.TrySetTrue()) // succeeds only for the first entrant
            return;

        try
        {
            var newDict = new ConcurrentDictionary<TKey, TValue>(_concurrencyLevel, _capacity, _comparer);
            Interlocked.Exchange(ref _dict, newDict);
        }
        finally
        {
            _ticking.TrySetFalse();
        }
    }

    public void Clear() => Tick();

    public bool TryAdd(TKey key, TValue value) => _dict.TryAdd(key, value);

    public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory) => _dict.GetOrAdd(key, valueFactory);

    public TValue GetOrAdd(TKey key, TValue value) => _dict.GetOrAdd(key, value);

    public TValue AddOrUpdate(TKey key, Func<TKey, TValue> addFactory, Func<TKey, TValue, TValue> updateFactory) =>
        _dict.AddOrUpdate(key, addFactory, updateFactory);

    public bool TryGetValue(TKey key, out TValue value) => _dict.TryGetValue(key, out value);

    public bool TryRemove(TKey key, out TValue value) => _dict.TryRemove(key, out value);

    public bool ContainsKey(TKey key) => _dict.ContainsKey(key);

    public int Count => _dict.Count;

    public IEnumerable<KeyValuePair<TKey, TValue>> Items => _dict;

    /// Snapshot (allocates). Prefer exposing IEnumerable for allocation-free iteration when possible.
    public KeyValuePair<TKey, TValue>[] ToArray() => _dict.ToArray();

    public async ValueTask DisposeAsync()
    {
        await _timer.DisposeAsync().NoSync();

        Interlocked.Exchange(ref _dict, new ConcurrentDictionary<TKey, TValue>(_concurrencyLevel, _capacity, _comparer));
    }
}