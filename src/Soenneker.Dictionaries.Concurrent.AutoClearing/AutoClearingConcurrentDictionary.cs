using Soenneker.Atomics.ValueBools;
using Soenneker.Dictionaries.Concurrent.AutoClearing.Abstract;
using Soenneker.Extensions.ValueTask;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Dictionaries.Concurrent.AutoClearing;

///<inheritdoc cref="IAutoClearingConcurrentDictionary{TKey, TValue}"/>
public sealed class AutoClearingConcurrentDictionary<TKey, TValue> : IAutoClearingConcurrentDictionary<TKey, TValue> where TKey : notnull
{
    private const int _defaultCapacity = 31;
    private const int _clearInPlaceThreshold = 256;

    private ConcurrentDictionary<TKey, TValue> _dict;

    private readonly Timer _timer;
    private readonly IEqualityComparer<TKey>? _comparer;
    private readonly int _concurrencyLevel;
    private readonly int _capacity;

    private ValueAtomicBool _ticking;
    private ValueAtomicBool _disposed;
    private ValueAtomicBool _dirty;

    // Static callback avoids per-tick allocations
    private static readonly TimerCallback _sTimerCb = static state => ((AutoClearingConcurrentDictionary<TKey, TValue>)state!).Tick();

    public AutoClearingConcurrentDictionary(TimeSpan clearInterval, int concurrencyLevel = 0, int capacity = _defaultCapacity,
        IEqualityComparer<TKey>? comparer = null)
    {
        if (clearInterval <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(clearInterval));

        _concurrencyLevel = concurrencyLevel > 0 ? concurrencyLevel : Environment.ProcessorCount;
        _capacity = capacity > 0 ? capacity : _defaultCapacity;
        _comparer = comparer;

        _dict = new ConcurrentDictionary<TKey, TValue>(_concurrencyLevel, _capacity, _comparer);
        _timer = new Timer(_sTimerCb, this, clearInterval, clearInterval);
    }

    private void MarkDirty() => _dirty.TrySetTrue();

    private void Tick()
    {
        if (_disposed.Value)
            return;

        // If nothing changed since last tick, do nothing.
        if (!_dirty.TrySetFalse())
            return;

        if (!_ticking.TrySetTrue())
            return;

        try
        {
            ConcurrentDictionary<TKey, TValue> dict = _dict;

            if (dict.IsEmpty)
                return;

            if (dict.Count <= _clearInPlaceThreshold)
            {
                dict.Clear();
                return;
            }

            var newDict = new ConcurrentDictionary<TKey, TValue>(_concurrencyLevel, _capacity, _comparer);

            Interlocked.Exchange(ref _dict, newDict);
        }
        finally
        {
            _ticking.TrySetFalse();
        }
    }

    public void Clear()
    {
        _dirty.TrySetTrue();
        Tick();
    }

    public bool TryAdd(TKey key, TValue value)
    {
        ConcurrentDictionary<TKey, TValue> dict = _dict;

        if (!dict.TryAdd(key, value))
            return false;

        MarkDirty();
        return true;
    }

    public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
    {
        ConcurrentDictionary<TKey, TValue> dict = _dict;

        return dict.GetOrAdd(key, k =>
        {
            MarkDirty();
            return valueFactory(k);
        });
    }

    public TValue GetOrAdd(TKey key, TValue value)
    {
        ConcurrentDictionary<TKey, TValue> dict = _dict;

        return dict.GetOrAdd(key, _ =>
        {
            MarkDirty();
            return value;
        });
    }

    public TValue AddOrUpdate(TKey key, Func<TKey, TValue> addFactory, Func<TKey, TValue, TValue> updateFactory)
    {
        ConcurrentDictionary<TKey, TValue> dict = _dict;

        return dict.AddOrUpdate(key, k =>
        {
            MarkDirty();
            return addFactory(k);
        }, (k, existing) =>
        {
            MarkDirty();
            return updateFactory(k, existing);
        });
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
        ConcurrentDictionary<TKey, TValue> dict = _dict;
        return dict.TryGetValue(key, out value);
    }

    public bool TryRemove(TKey key, out TValue value)
    {
        ConcurrentDictionary<TKey, TValue> dict = _dict;

        if (!dict.TryRemove(key, out value))
            return false;

        MarkDirty();
        return true;
    }

    public bool ContainsKey(TKey key)
    {
        ConcurrentDictionary<TKey, TValue> dict = _dict;
        return dict.ContainsKey(key);
    }

    public int Count
    {
        get
        {
            ConcurrentDictionary<TKey, TValue> dict = _dict;
            return dict.Count;
        }
    }

    public IEnumerable<KeyValuePair<TKey, TValue>> Items => _dict;

    public KeyValuePair<TKey, TValue>[] ToArray()
    {
        ConcurrentDictionary<TKey, TValue> dict = _dict;
        return dict.ToArray();
    }

    public void Dispose()
    {
        if (!_disposed.TrySetTrue())
            return;

        try
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
        }
        catch
        {
        }

        _timer.Dispose();
        _dict.Clear();
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed.TrySetTrue())
            return;

        try
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
        }
        catch
        {
        }

        await _timer.DisposeAsync()
                    .NoSync();
        _dict.Clear();
    }
}