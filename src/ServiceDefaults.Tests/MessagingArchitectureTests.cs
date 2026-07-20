using System.Text.Json;
using System.Text.RegularExpressions;

namespace ServiceDefaults.Tests;

public class MessagingArchitectureTests
{
    [Fact]
    public void Topology_manifest_has_unique_durable_endpoint_names()
    {
        using var document = LoadTopology();
        var endpoints = document.RootElement.GetProperty("endpoints").EnumerateArray().ToArray();
        var names = endpoints.Select(endpoint => endpoint.GetProperty("name").GetString()!).ToArray();

        Assert.All(endpoints, endpoint => Assert.True(endpoint.GetProperty("durable").GetBoolean()));
        Assert.Equal(names.Length, names.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void Every_routed_command_has_one_destination()
    {
        using var document = LoadTopology();
        var routedCommands = document.RootElement.GetProperty("endpoints")
            .EnumerateArray()
            .Where(endpoint => endpoint.TryGetProperty("commands", out _))
            .SelectMany(endpoint => endpoint.GetProperty("commands").EnumerateArray()
                .Select(command => (Command: command.GetString()!, Endpoint: endpoint.GetProperty("name").GetString()!)))
            .ToArray();

        Assert.NotEmpty(routedCommands);
        Assert.All(routedCommands.GroupBy(route => route.Command), group => Assert.Single(group));
    }

    [Fact]
    public void Every_application_consumer_is_present_in_the_topology_contract()
    {
        var root = RepositoryRoot();
        using var document = LoadTopology();
        var manifestConsumers = document.RootElement.GetProperty("consumers")
            .EnumerateArray()
            .Select(item => item.GetProperty("type").GetString()!.Split('.').Last())
            .OrderBy(name => name)
            .ToArray();

        var sourceConsumers = Directory.EnumerateFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsTestOrGeneratedSource(path))
            .SelectMany(path => Regex.Matches(File.ReadAllText(path), @"class\s+(\w+)[^{:]*:\s*IConsumer<")
                .Select(match => match.Groups[1].Value))
            .OrderBy(name => name)
            .ToArray();

        Assert.Equal(sourceConsumers, manifestConsumers);
    }

    [Fact]
    public void MassTransit_package_versions_are_centralized_without_drift()
    {
        var root = RepositoryRoot();
        var props = File.ReadAllText(Path.Combine(root, "Directory.Packages.props"));
        Assert.Contains("<MassTransitVersion>8.5.10</MassTransitVersion>", props);

        var projects = Directory.EnumerateFiles(Path.Combine(root, "src"), "*.csproj", SearchOption.AllDirectories);
        foreach (var project in projects)
        {
            var xml = File.ReadAllText(project);
            var references = Regex.Matches(xml, "<PackageReference\\s+Include=\"MassTransit(?:\\.[^\"]+)?\"[^>]*>");
            foreach (Match reference in references)
            {
                Assert.Contains("Version=\"$(MassTransitVersion)\"", reference.Value);
            }
        }
    }

    [Fact]
    public void Application_code_does_not_reference_transport_specific_apis()
    {
        var root = RepositoryRoot();
        var src = Path.Combine(root, "src");
        var forbidden = new[]
        {
            "MassTransit.RabbitMqTransport",
            "MassTransit.AzureServiceBusTransport",
            "UsingRabbitMq(",
            "UsingAzureServiceBus(",
            "IRabbitMq",
            "IServiceBusBusFactoryConfigurator",
            "Azure.Messaging.ServiceBus"
        };

        var violations = Directory.EnumerateFiles(src, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}ServiceDefaults{Path.DirectorySeparatorChar}"))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}AppHost{Path.DirectorySeparatorChar}"))
            .Where(path => !IsTestOrGeneratedSource(path))
            .SelectMany(path => forbidden
                .Where(token => File.ReadAllText(path).Contains(token, StringComparison.Ordinal))
                .Select(token => $"{Path.GetRelativePath(root, path)}: {token}"))
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void Scheduler_strategies_are_declared_for_both_transports()
    {
        using var document = LoadTopology();
        var scheduler = document.RootElement.GetProperty("scheduler");

        Assert.Equal("Quartz", scheduler.GetProperty("RabbitMq").GetProperty("strategy").GetString());
        Assert.Equal("quartz", scheduler.GetProperty("RabbitMq").GetProperty("endpoint").GetString());
        Assert.Equal("Native", scheduler.GetProperty("AzureServiceBus").GetProperty("strategy").GetString());
    }

    [Fact]
    public void Orchestration_command_destinations_match_the_topology_contract()
    {
        var root = RepositoryRoot();
        using var document = LoadTopology();
        var commandDestinations = document.RootElement.GetProperty("endpoints")
            .EnumerateArray()
            .Where(endpoint => endpoint.TryGetProperty("commands", out _))
            .SelectMany(endpoint => endpoint.GetProperty("commands").EnumerateArray()
                .Select(command => new
                {
                    Command = command.GetString()!.Split('.').Last(),
                    Endpoint = endpoint.GetProperty("name").GetString()!
                }))
            .ToDictionary(route => route.Command, route => route.Endpoint);

        var stateMachine = File.ReadAllText(Path.Combine(
            root,
            "src",
            "Orchestration",
            "Orchestration",
            "Saga",
            "OrderStateMachine.cs"));
        var routes = Regex.Matches(
                stateMachine,
                @"\.Send\(new Uri\(""queue:([^""]+)""\),\s*ctx\s*=>\s*new\s+(\w+Command)")
            .Select(match => new
            {
                Endpoint = match.Groups[1].Value,
                Command = match.Groups[2].Value
            })
            .ToArray();

        Assert.NotEmpty(routes);
        Assert.All(routes, route =>
        {
            Assert.True(
                commandDestinations.TryGetValue(route.Command, out var endpoint),
                $"{route.Command} does not have a destination in the topology manifest.");
            Assert.Equal(endpoint, route.Endpoint);
        });
    }

    [Fact]
    public void Commands_published_inline_are_documented_exceptions()
    {
        var root = RepositoryRoot();
        using var document = LoadTopology();
        var documentedExceptions = document.RootElement.GetProperty("publishedCommandExceptions")
            .EnumerateArray()
            .Select(item => item.GetProperty("contract").GetString()!.Split('.').Last())
            .OrderBy(name => name)
            .ToArray();

        var publishedCommands = Directory.EnumerateFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsTestOrGeneratedSource(path))
            .SelectMany(path => Regex.Matches(
                    File.ReadAllText(path),
                    @"\.Publish\s*\(\s*(?:\w+\s*=>\s*)?new\s+(\w+Command)")
                .Select(match => match.Groups[1].Value))
            .Distinct()
            .OrderBy(name => name)
            .ToArray();

        Assert.Equal(documentedExceptions, publishedCommands);
    }

    [Fact]
    public void Order_status_updates_are_serialized_to_prevent_lost_aggregate_fields()
    {
        var root = RepositoryRoot();
        var ordersMessagingProgram = File.ReadAllText(Path.Combine(
            root,
            "src",
            "Orders",
            "Orders.Messaging",
            "Program.cs"));

        Assert.Matches(
            @"ReceiveEndpoint<UpdateOrderStatusCommandConsumer>\s*\(\s*""update-order-status-command""\s*,\s*endpoint\s*=>\s*endpoint\.ConcurrentMessageLimit\s*=\s*1\s*\)",
            ordersMessagingProgram);
    }

    [Theory]
    [InlineData("https")]
    [InlineData("http")]
    public void Visual_Studio_AppHost_profiles_explicitly_select_RabbitMq(string profileName)
    {
        var launchSettingsPath = Path.Combine(
            RepositoryRoot(),
            "src",
            "AppHost",
            "Properties",
            "launchSettings.json");
        using var document = JsonDocument.Parse(File.ReadAllText(launchSettingsPath));
        var environmentVariables = document.RootElement
            .GetProperty("profiles")
            .GetProperty(profileName)
            .GetProperty("environmentVariables");

        Assert.Equal("RabbitMq", environmentVariables.GetProperty("Messaging__Transport").GetString());
        Assert.False(environmentVariables.TryGetProperty("Messaging__AzureServiceBus__NamespaceUri", out _));
    }

    [Fact]
    public void Non_development_hosts_load_and_unprefix_shared_Azure_App_Configuration()
    {
        var root = RepositoryRoot();
        var configurationExtensions = File.ReadAllText(Path.Combine(
            root,
            "src",
            "ServiceDefaults",
            "Extensions.Configuration.cs"));

        Assert.Contains("if (!builder.Environment.IsDevelopment())", configurationExtensions);
        Assert.Contains(".Select(\"common:*\", LabelFilter.Null)", configurationExtensions);
        Assert.Contains(".TrimKeyPrefix(\"common:\")", configurationExtensions);
        Assert.Contains(".TrimKeyPrefix($\"{builder.Environment.ApplicationName}:\")", configurationExtensions);

        var messagingHosts = Directory.EnumerateFiles(Path.Combine(root, "src"), "Program.cs", SearchOption.AllDirectories)
            .Where(path => !IsTestOrGeneratedSource(path))
            .Where(path => File.ReadAllText(path).Contains("WithMassTransit", StringComparison.Ordinal))
            .ToArray();

        Assert.NotEmpty(messagingHosts);
        Assert.All(messagingHosts, path =>
        {
            var program = File.ReadAllText(path);
            Assert.True(
                program.Contains("AddServiceDefaults", StringComparison.Ordinal) ||
                program.Contains("AddWebServiceDefaults", StringComparison.Ordinal),
                $"{path} must register shared service defaults.");
        });
    }

    private static JsonDocument LoadTopology() => JsonDocument.Parse(File.ReadAllText(Path.Combine(
        RepositoryRoot(),
        "docs",
        "plans",
        "production-readiness",
        "messaging-topology.json")));

    private static bool IsTestOrGeneratedSource(string path) => path
        .Split(
            [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
            StringSplitOptions.RemoveEmptyEntries)
        .Any(segment =>
            segment.Equals("Tests", StringComparison.OrdinalIgnoreCase)
            || segment.EndsWith(".Tests", StringComparison.OrdinalIgnoreCase)
            || segment.Equals("obj", StringComparison.OrdinalIgnoreCase));

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Directory.Packages.props")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root was not found.");
    }
}
