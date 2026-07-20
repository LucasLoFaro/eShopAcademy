using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Xunit;

namespace Gateway.Tests;

public sealed class GatewayHealthTests
{
    private static WebApplicationFactory<Program> CreateFactory() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("EntraId:Instance", "https://login.microsoftonline.com/");
            builder.UseSetting("EntraId:TenantId", "00000000-0000-0000-0000-000000000001");
            builder.UseSetting("EntraId:ClientId", "00000000-0000-0000-0000-000000000002");
            builder.UseSetting("EntraId:Audience", "api://00000000-0000-0000-0000-000000000002");
        });

    [Theory]
    [InlineData("/alive")]
    [InlineData("/health")]
    public async Task HealthEndpoints_AreAvailableWithoutCallingDownstreamServices(string path)
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
