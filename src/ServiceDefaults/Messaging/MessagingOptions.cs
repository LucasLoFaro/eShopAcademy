using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ServiceDefaults;

public enum MessagingTransport
{
    RabbitMq,
    AzureServiceBus
}

public sealed class MessagingOptions
{
    public const string SectionName = "Messaging";

    public MessagingTransport Transport { get; set; }

    public RabbitMqMessagingOptions RabbitMq { get; set; } = new();

    public AzureServiceBusMessagingOptions AzureServiceBus { get; set; } = new();
}

public sealed class RabbitMqMessagingOptions
{
    public string? ConnectionString { get; set; }
}

public sealed class AzureServiceBusMessagingOptions
{
    public string? NamespaceUri { get; set; }

    public bool CreateTopology { get; set; } = true;
}

internal static class MessagingOptionsResolver
{
    public static MessagingOptions Resolve(IConfiguration configuration, IHostEnvironment environment)
    {
        var section = configuration.GetSection(MessagingOptions.SectionName);
        var options = section.Get<MessagingOptions>() ?? new MessagingOptions();

        if (string.IsNullOrWhiteSpace(section[nameof(MessagingOptions.Transport)]))
        {
            options.Transport = environment.IsDevelopment()
                ? MessagingTransport.RabbitMq
                : MessagingTransport.AzureServiceBus;
        }

        options.RabbitMq.ConnectionString ??= configuration.GetConnectionString("rabbit");

        var failures = Validate(options);
        if (failures.Count > 0)
        {
            throw new OptionsValidationException(
                MessagingOptions.SectionName,
                typeof(MessagingOptions),
                failures);
        }

        return options;
    }

    internal static IReadOnlyList<string> Validate(MessagingOptions options)
    {
        var failures = new List<string>();

        if (!Enum.IsDefined(options.Transport))
        {
            failures.Add("Messaging:Transport must be RabbitMq or AzureServiceBus.");
            return failures;
        }

        if (options.Transport == MessagingTransport.RabbitMq)
        {
            if (!Uri.TryCreate(options.RabbitMq.ConnectionString, UriKind.Absolute, out var rabbitUri) ||
                (rabbitUri.Scheme != "amqp" && rabbitUri.Scheme != "amqps"))
            {
                failures.Add("Messaging:RabbitMq:ConnectionString (or ConnectionStrings:rabbit) must be an absolute amqp/amqps URI.");
            }
        }
        else if (!Uri.TryCreate(options.AzureServiceBus.NamespaceUri, UriKind.Absolute, out var namespaceUri) ||
                 (namespaceUri.Scheme != Uri.UriSchemeHttps && namespaceUri.Scheme != "sb"))
        {
            failures.Add("Messaging:AzureServiceBus:NamespaceUri must be an absolute https or sb namespace URI.");
        }

        return failures;
    }
}

