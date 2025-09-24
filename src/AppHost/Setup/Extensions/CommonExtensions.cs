namespace AppHost.Setup.Extensions;

public static class CommonExtensions
{
    public static IResourceBuilder<ProjectResource> WithCommonEnvironments(this IResourceBuilder<ProjectResource> project)
        => project.WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
                  .WithEnvironment("DOTNET_ENVIRONMENT", "Development")
                  .WithEnvironment("APPCONFIGURATION", "eShopAcademy")
                  .WithEnvironment("KEYVAULT", "eshopacademy")
                  .WithEnvironment("AZURE_TENANT_ID", "82abd0e4-97e7-4ad4-9b49-4c93188625ed");
}
