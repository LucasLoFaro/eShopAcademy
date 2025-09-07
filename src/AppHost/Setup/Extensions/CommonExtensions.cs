namespace AppHost.Setup.Extensions;

public static class CommonExtensions
{
    public static IResourceBuilder<ProjectResource> WithCommonEnvironments(this IResourceBuilder<ProjectResource> project)
        => project.WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
                  .WithEnvironment("APPCONFIGURATION", "eShopAcademy")
                  .WithEnvironment("KEYVAULT", "eshopacademy")
                  .WithEnvironment("AZURE_TENANT_ID", "82abd0e4-97e7-4ad4-9b49-4c93188625ed");

    public static IResourceBuilder<ProjectResource> WithCassandra(this IResourceBuilder<ProjectResource> project, IResourceBuilder<ContainerResource> cassandra)
        => project.WaitFor(cassandra)
                  .WithEnvironment("ConnectionStrings__Cassandra", "Contact Points=eshopacademy-cassandra;Port=9042;");
}
