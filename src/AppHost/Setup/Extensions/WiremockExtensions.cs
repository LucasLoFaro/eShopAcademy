using AppHost.Setup.Resources;

namespace Aspire.Hosting;

public static class WiremockExtensions
{
    public static IResourceBuilder<WiremockResource> AddWiremock(
        this IDistributedApplicationBuilder builder, string name = "external-services-mocks")
            => builder.AddResource(new WiremockResource(name))
                    .WithImage("wiremock/wiremock", "latest")
                    .WithBindMount(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "Mocks")), "/home/wiremock/mappings");
}