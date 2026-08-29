[![](https://img.shields.io/nuget/v/soenneker.dictionaries.concurrent.autoclearing.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.dictionaries.concurrent.autoclearing/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.dictionaries.concurrent.autoclearing/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.dictionaries.concurrent.autoclearing/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.dictionaries.concurrent.autoclearing.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.dictionaries.concurrent.autoclearing/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.dictionaries.concurrent.autoclearing/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.dictionaries.concurrent.autoclearing/actions/workflows/codeql.yml)

# Soenneker.Dictionaries.Concurrent.AutoClearing

Represents a high-performance concurrent key/value store whose contents are periodically cleared on a timer. Clearing is performed using an adaptive strategy: small dictionaries are cleared in-place, while large dictionaries are atomically replaced to avoid O(N) work on the timer thread. All operations are thread-safe and designed for low allocation overhead.

## Install

```bash
dotnet add package Soenneker.Dictionaries.Concurrent.AutoClearing
```

## Quick start

```csharp
using Soenneker.Dictionaries.Concurrent.AutoClearing.Abstract;

IAutoClearingConcurrentDictionary<TKey, TValue> autoClearingConcurrentDictionary = /* resolve from DI */;
autoClearingConcurrentDictionary.Clear();
```

Immediately clears the dictionary. The implementation may either clear in-place or atomically swap the underlying dictionary depending on its current size. This method is thread-safe and may run concurrently with other operations.

## What you get

- `IAutoClearingConcurrentDictionary<TKey, TValue>` — Represents a high-performance concurrent key/value store whose contents are periodically cleared on a timer. Clearing is performed using an adaptive strategy: small dictionaries are cleared in-place, while large dictionaries are atomically replaced to avoid O(N) work on the timer thread. All operations are thread-safe and designed for low allocation overhead.

## API at a glance

| API | What it does | Result / important behavior |
| --- | --- | --- |
| `IAutoClearingConcurrentDictionary<TKey, TValue>.Clear()` | Immediately clears the dictionary. The implementation may either clear in-place or atomically swap the underlying dictionary depending on its current size. This method is thread-safe and may run concurrently with other operations. | Returns no value; the requested change is complete when the method returns. |
| `IAutoClearingConcurrentDictionary<TKey, TValue>.TryAdd(key, value)` | Attempts to add the specified key and value to the dictionary. | `true` if the key/value pair was added; `false` if the key already exists. |
| `IAutoClearingConcurrentDictionary<TKey, TValue>.GetOrAdd(key, valueFactory)` | Gets the value associated with the specified key. If the key does not exist, the value is created by `valueFactory` and added atomically. | The existing or newly added value associated with `key`. |
| `IAutoClearingConcurrentDictionary<TKey, TValue>.GetOrAdd(key, value)` | Gets the value associated with the specified key. If the key does not exist, `value` is added atomically. | The existing or newly added value associated with `key`. |
| `IAutoClearingConcurrentDictionary<TKey, TValue>.AddOrUpdate(key, addFactory, updateFactory)` | Adds a key/value pair if the key does not already exist, or updates the value for an existing key. | The resulting value stored for the specified key. |
| `IAutoClearingConcurrentDictionary<TKey, TValue>.TryGetValue(key, value)` | Attempts to retrieve the value associated with the specified key. | `true` if the key was found; otherwise, `false`. |
| `IAutoClearingConcurrentDictionary<TKey, TValue>.TryRemove(key, value)` | Attempts to remove the value associated with the specified key. | `true` if the element was removed; otherwise, `false`. |
| `IAutoClearingConcurrentDictionary<TKey, TValue>.ContainsKey(key)` | Determines whether the dictionary contains the specified key. | `true` if the dictionary contains the specified key; otherwise, `false`. |
| `IAutoClearingConcurrentDictionary<TKey, TValue>.Count` | Gets the approximate number of elements currently contained in the dictionary. | The returned value may change immediately due to concurrent operations. |
| `IAutoClearingConcurrentDictionary<TKey, TValue>.Items` | Gets a live enumerable view over the dictionary. | This is not a snapshot. The sequence may reflect concurrent modifications and may enumerate elements that are later cleared. |
| `IAutoClearingConcurrentDictionary<TKey, TValue>.ToArray()` | Returns a point-in-time snapshot of the dictionary as a new array. | An array containing a snapshot of the current key/value pairs. |
| `IAutoClearingConcurrentDictionary<TKey, TValue>.DisposeAsync()` | Asynchronously disposes the dictionary, stopping the periodic clearing mechanism. | A `ValueTask` representing the asynchronous dispose operation. |

## Important behavior

- `IAutoClearingConcurrentDictionary<TKey, TValue>.Count`: The returned value may change immediately due to concurrent operations.
- `IAutoClearingConcurrentDictionary<TKey, TValue>.Items`: This is not a snapshot. The sequence may reflect concurrent modifications and may enumerate elements that are later cleared.

## Practical notes

- Dispose instances you own when their scope ends so held resources can be released.
