namespace AppHost.Setup.Extensions;

public static class GatewayExtensions
{
    public static void Configure(
        IResourceBuilder<ProjectResource> gateway)
    {
        gateway
            .WithCommonEnvironments()
            .WithHttpEndpoint(port: 5200, name: "gateway")
            .WithEnvironment("SpaOrigin", "http://localhost:5173")
            .WithEnvironment("EntraId__Instance", "https://login.microsoftonline.com/")
            .WithEnvironment("EntraId__TenantId", "82abd0e4-97e7-4ad4-9b49-4c93188625ed")
            .WithEnvironment("EntraId__ClientId", "f54e2efa-6412-44d1-a342-34229767db5c")
            .WithEnvironment("EntraId__Audience", "api://f54e2efa-6412-44d1-a342-34229767db5c");
    }
}
