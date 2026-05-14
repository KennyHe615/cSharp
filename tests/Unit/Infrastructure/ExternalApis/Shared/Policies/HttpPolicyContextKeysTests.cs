using Infrastructure.ExternalApis.Shared.Policies;

using Xunit;


namespace tests.Unit.Infrastructure.ExternalApis.Shared.Policies;

public sealed class HttpPolicyContextKeysTests
{
    [Fact]
    public void Lob_HasExpectedValue()
    {
        Assert.Equal("lob", HttpPolicyContextKeys.Lob);
    }

    [Fact]
    public void RefreshFunc_HasExpectedValue()
    {
        Assert.Equal("RefreshTokenFunc", HttpPolicyContextKeys.RefreshFunc);
    }

    [Fact]
    public void Keys_AreDistinct_AndNonEmpty()
    {
        Assert.False(string.IsNullOrWhiteSpace(HttpPolicyContextKeys.Lob));
        Assert.False(string.IsNullOrWhiteSpace(HttpPolicyContextKeys.RefreshFunc));
        Assert.NotEqual(HttpPolicyContextKeys.Lob, HttpPolicyContextKeys.RefreshFunc);
    }
}
