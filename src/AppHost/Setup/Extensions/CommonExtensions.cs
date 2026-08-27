namespace AppHost.Setup.Extensions;

public static class CommonExtensions
{
    public static IResourceBuilder<ProjectResource> WithCommonEnvironments(this IResourceBuilder<ProjectResource> project)
        => project.WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
                  .WithEnvironment("DOTNET_ENVIRONMENT", "Development")
                  .WithEnvironment("APPCONFIGURATION", "eShopAcademy")
                  .WithEnvironment("KEYVAULT", "eshopacademy");
}
