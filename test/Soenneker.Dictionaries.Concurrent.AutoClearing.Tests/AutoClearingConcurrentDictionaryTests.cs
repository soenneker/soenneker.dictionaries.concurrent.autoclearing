using System;
using AwesomeAssertions;
using Soenneker.Tests.HostedUnit;

namespace Soenneker.Dictionaries.Concurrent.AutoClearing.Tests;

[ClassDataSource<Host>(Shared = SharedType.PerTestSession)]
public sealed class AutoClearingConcurrentDictionaryTests : HostedUnitTest
{
    public AutoClearingConcurrentDictionaryTests(Host host) : base(host)
    {

    }

    [Test]
    public void Clear_preserves_comparer_and_dispose_rejects_operations()
    {
        var dictionary = new AutoClearingConcurrentDictionary<string, int>(
            TimeSpan.FromHours(1), comparer: StringComparer.OrdinalIgnoreCase);

        dictionary.TryAdd("Alpha", 1).Should().BeTrue();
        dictionary.ContainsKey("alpha").Should().BeTrue();

        dictionary.Clear();
        dictionary.ContainsKey("alpha").Should().BeFalse();
        dictionary.TryAdd("Beta", 2).Should().BeTrue();
        dictionary.ContainsKey("BETA").Should().BeTrue();

        dictionary.Dispose();
        Action action = () => dictionary.TryAdd("gamma", 3);
        action.Should().Throw<ObjectDisposedException>();
    }
}
