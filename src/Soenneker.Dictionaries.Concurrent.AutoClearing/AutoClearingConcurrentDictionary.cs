using Soenneker.Atomics.ValueBools;
using Soenneker.Dictionaries.Concurrent.AutoClearing.Abstract;
using Soenneker.Extensions.ValueTask;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Dictionaries.Concurrent.AutoClearing;

/// <inheritdoc cref="IAutoClearingConcurrentDictionary{TKey, TValue}"/>
public sealed class AutoClearingConcurrentDictionary<TKey, TValue> : IAutoClearingConcurrentDictionary<TKey, TValue> where TKey : notnull
{
    private const int _defaultCapacity = 31;
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

        _dict = CreateDictionary();
        _timer = new Timer(_sTimerCb, this, clearInterval, clearInterval);
    }

    private void MarkDirty() => _dirty.TrySetTrue();

    private void Tick()
    {
        if (_disposed.Value)
            return;

        if (!_ticking.TrySetTrue())
            return;

        try
        {
            if (!_dirty.TrySetFalse())
                return;

            Interlocked.Exchange(ref _dict, CreateDictionary());
        }
        finally
        {
            _ticking.TrySetFalse();
        }
    }

    public void Clear()
    {
        ThrowIfDisposed();

        while (!_ticking.TrySetTrue())
        {
            ThrowIfDisposed();
            Thread.Yield();
        }

        try
        {
            _dirty.TrySetFalse();
            Interlocked.Exchange(ref _dict, CreateDictionary());
        }
        finally
        {
            _ticking.TrySetFalse();
        }
    }

    public bool TryAdd(TKey key, TValue value)
    {
        while (true)
        {
            ThrowIfDisposed();
            ConcurrentDictionary<TKey, TValue> dict = Volatile.Read(ref _dict);
            bool added = dict.TryAdd(key, value);

            if (!ReferenceEquals(dict, Volatile.Read(ref _dict)))
                continue;

            if (added)
                MarkDirty();

            return added;
        }
    }

    public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
    {
        while (true)
        {
            ThrowIfDisposed();
            ConcurrentDictionary<TKey, TValue> dict = Volatile.Read(ref _dict);
            TValue value = dict.GetOrAdd(key, valueFactory);

            if (!ReferenceEquals(dict, Volatile.Read(ref _dict)))
                continue;

            MarkDirty();
            return value;
        }
    }

    public TValue GetOrAdd(TKey key, TValue value)
    {
        while (true)
        {
            ThrowIfDisposed();
            ConcurrentDictionary<TKey, TValue> dict = Volatile.Read(ref _dict);
            TValue result = dict.GetOrAdd(key, value);

            if (!ReferenceEquals(dict, Volatile.Read(ref _dict)))
                continue;

            MarkDirty();
            return result;
        }
    }

    public TValue AddOrUpdate(TKey key, Func<TKey, TValue> addFactory, Func<TKey, TValue, TValue> updateFactory)
    {
        while (true)
        {
            ThrowIfDisposed();
            ConcurrentDictionary<TKey, TValue> dict = Volatile.Read(ref _dict);
            TValue value = dict.AddOrUpdate(key, addFactory, updateFactory);

            if (!ReferenceEquals(dict, Volatile.Read(ref _dict)))
                continue;

            MarkDirty();
            return value;
        }
    }

    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        while (true)
        {
            ThrowIfDisposed();
            ConcurrentDictionary<TKey, TValue> dict = Volatile.Read(ref _dict);
            bool found = dict.TryGetValue(key, out value);

            if (ReferenceEquals(dict, Volatile.Read(ref _dict)))
                return found;
        }
    }

    public bool TryRemove(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        while (true)
        {
            ThrowIfDisposed();
            ConcurrentDictionary<TKey, TValue> dict = Volatile.Read(ref _dict);
            bool removed = dict.TryRemove(key, out value);

            if (!ReferenceEquals(dict, Volatile.Read(ref _dict)))
                continue;

            if (removed)
                MarkDirty();

            return removed;
        }
    }

    public bool ContainsKey(TKey key)
    {
        while (true)
        {
            ThrowIfDisposed();
            ConcurrentDictionary<TKey, TValue> dict = Volatile.Read(ref _dict);
            bool found = dict.ContainsKey(key);

            if (ReferenceEquals(dict, Volatile.Read(ref _dict)))
                return found;
        }
    }

    public int Count
    {
        get
        {
            ThrowIfDisposed();
            ConcurrentDictionary<TKey, TValue> dict = Volatile.Read(ref _dict);
            return dict.Count;
        }
    }

    public IEnumerable<KeyValuePair<TKey, TValue>> Items
    {
        get
        {
            ThrowIfDisposed();
            return Volatile.Read(ref _dict);
        }
    }

    public KeyValuePair<TKey, TValue>[] ToArray()
    {
        ThrowIfDisposed();
        ConcurrentDictionary<TKey, TValue> dict = Volatile.Read(ref _dict);
        return dict.ToArray();
    }

    private ConcurrentDictionary<TKey, TValue> CreateDictionary() => new(_concurrencyLevel, _capacity, _comparer);

    private void ThrowIfDisposed()
    {
        if (_disposed.Value)
            throw new ObjectDisposedException(nameof(AutoClearingConcurrentDictionary<TKey, TValue>));
    }

    /// <summary>
    /// Releases resources used by the current instance.
    /// </summary>
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
        Interlocked.Exchange(ref _dict, CreateDictionary()).Clear();
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
        Interlocked.Exchange(ref _dict, CreateDictionary()).Clear();
    }
}
