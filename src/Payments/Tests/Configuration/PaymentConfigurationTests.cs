using FluentAssertions;
using Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Payments.Tests.Configuration;

public sealed class PaymentConfigurationTests
{
    [Theory]
    [InlineData("")]
    [InlineData("too-short")]
    public void SignatureSecret_InvalidValue_FailsValidation(string secret)
    {
        var services = new ServiceCollection();
        services.AddOptions<PaymentSecurityOptions>()
            .Configure(options => options.SignatureSecret = secret)
            .ValidateDataAnnotations();

        using var provider = services.BuildServiceProvider();
        var act = () => provider.GetRequiredService<IOptions<PaymentSecurityOptions>>().Value;

        act.Should().Throw<OptionsValidationException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(31)]
    public void PspTimeout_OutOfRange_FailsValidation(int timeoutSeconds)
    {
        var services = new ServiceCollection();
        services.AddOptions<PspClientOptions>()
            .Configure(options => options.TimeoutSeconds = timeoutSeconds)
            .ValidateDataAnnotations();

        using var provider = services.BuildServiceProvider();
        var act = () => provider.GetRequiredService<IOptions<PspClientOptions>>().Value;

        act.Should().Throw<OptionsValidationException>();
    }
}
