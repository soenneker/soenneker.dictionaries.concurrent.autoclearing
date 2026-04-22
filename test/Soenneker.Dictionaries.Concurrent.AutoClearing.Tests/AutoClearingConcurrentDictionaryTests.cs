using Soenneker.Tests.HostedUnit;

namespace Soenneker.Dictionaries.Concurrent.AutoClearing.Tests;

[ClassDataSource<Host>(Shared = SharedType.PerTestSession)]
public sealed class AutoClearingConcurrentDictionaryTests : HostedUnitTest
{
    public AutoClearingConcurrentDictionaryTests(Host host) : base(host)
    {

    }

    [Test]
    public void Default()
    {

    }
}
