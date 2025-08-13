using Soenneker.Tests.FixturedUnit;
using Xunit;

namespace Soenneker.Dictionaries.Concurrent.AutoClearing.Tests;

[Collection("Collection")]
public sealed class AutoClearingConcurrentDictionaryTests : FixturedUnitTest
{
    public AutoClearingConcurrentDictionaryTests(Fixture fixture, ITestOutputHelper output) : base(fixture, output)
    {

    }

    [Fact]
    public void Default()
    {

    }
}
