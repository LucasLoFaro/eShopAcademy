namespace AppHost.Setup.Resources;

public sealed class WiremockResource(string name) : ContainerResource(name), IResourceWithConnectionString
{
    internal const string EndpointName = "external-services-mocks";

    private EndpointReference? EndpointReference;
    public EndpointReference Endpoint => this.EndpointReference ??= new(this, EndpointName);

    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create(
            $"http://{this.Endpoint.Property(EndpointProperty.Host)}:{this.Endpoint.Property(EndpointProperty.Port)}"
        );
}