[![](https://img.shields.io/nuget/v/soenneker.dictionaries.concurrent.autoclearing.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.dictionaries.concurrent.autoclearing/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.dictionaries.concurrent.autoclearing/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.dictionaries.concurrent.autoclearing/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.dictionaries.concurrent.autoclearing.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.dictionaries.concurrent.autoclearing/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.dictionaries.concurrent.autoclearing/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.dictionaries.concurrent.autoclearing/actions/workflows/codeql.yml)

# Soenneker.Dictionaries.Concurrent.AutoClearing

A concurrent key/value map whose entire contents are periodically discarded as one batch.

## Installation

```bash
dotnet add package Soenneker.Dictionaries.Concurrent.AutoClearing
```

## Usage

```csharp
using Soenneker.Dictionaries.Concurrent.AutoClearing;

await using var recentLookups = new AutoClearingConcurrentDictionary<string, Customer>(
    clearInterval: TimeSpan.FromMinutes(5),
    capacity: 10_000,
    comparer: StringComparer.OrdinalIgnoreCase);

Customer customer = recentLookups.GetOrAdd(customerId, LoadCustomer);

if (recentLookups.TryGetValue(customerId, out Customer? cached))
{
    // Use the value while it remains in the current batch.
}
```

The timer rotates the backing dictionary after the interval when the current batch has been used. All entries are cleared together; an entry added just before a rotation can disappear almost immediately. This is not per-key TTL or sliding expiration.

Call `Clear()` to synchronously rotate the current batch:

```csharp
recentLookups.Clear();
```

Operations racing a rotation retry against the current dictionary, so they do not report success against an abandoned batch.

## Concurrency behavior

- `TryAdd`, `TryGetValue`, `TryRemove`, `ContainsKey`, `GetOrAdd`, and `AddOrUpdate` are safe for concurrent callers.
- As with `ConcurrentDictionary`, value factories can execute more than once under contention or when a rotation races the operation. Keep factories side-effect-free.
- `ToArray()` allocates a point-in-time snapshot. `Items` exposes the current concurrent dictionary for enumeration; it is not a stable snapshot across mutations or rotations.
- `Count` is observational and can change immediately.

The dictionary does not dispose values when a batch is cleared. Use it for values whose lifetime is independent of cache membership, or wrap ownership elsewhere.

Dispose the dictionary to stop its timer. All operations throw `ObjectDisposedException` afterward.
