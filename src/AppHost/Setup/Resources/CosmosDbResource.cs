namespace AppHost.Setup.Resources;

public sealed class CosmosDbResource(string name) : ContainerResource(name), IResourceWithConnectionString
{
    internal const string EndpointName = "cosmosdb";
    internal const string defaultAccountKey = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

    private EndpointReference? EndpointReference;
    public EndpointReference Endpoint => this.EndpointReference ??= new(this, EndpointName);

    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create(
            $"AccountEndpoint=http://{this.Endpoint.Property(EndpointProperty.Host)}:{this.Endpoint.Property(EndpointProperty.Port)};" +
            $"AccountKey={defaultAccountKey};"
        );
}