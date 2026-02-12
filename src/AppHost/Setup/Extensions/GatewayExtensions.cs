namespace AppHost.Setup.Extensions;

public static class GatewayExtensions
{
    public static void Configure(
        IResourceBuilder<ProjectResource> gateway)
    {
        gateway
            .WithCommonEnvironments()
            .WithHttpEndpoint(port: 5200, name: "gateway")
            .WithEnvironment("SpaOrigin", "http://localhost:5173");
    }
}
