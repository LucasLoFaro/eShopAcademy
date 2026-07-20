using FluentAssertions;
using Shipping.Application.Options;
using Xunit;

namespace Shipping.Tests;

public sealed class ShippingProviderOptionsTests
{
    [Theory]
    [InlineData("")]
    [InlineData("not-a-uri")]
    [InlineData("file:///tmp/provider")]
    public void IsValid_RejectsUnsafeOrMissingBaseAddress(string baseUrl)
    {
        ShippingProviderOptions.IsValid(new ShippingProviderOptions { BaseUrl = baseUrl }).Should().BeFalse();
    }

    [Theory]
    [InlineData("http://shipping-provider")]
    [InlineData("https://shipping.example")]
    public void IsValid_AcceptsHttpProviderAddress(string baseUrl)
    {
        ShippingProviderOptions.IsValid(new ShippingProviderOptions { BaseUrl = baseUrl }).Should().BeTrue();
    }
}
