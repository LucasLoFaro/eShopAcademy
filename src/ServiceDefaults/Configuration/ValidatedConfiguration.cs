using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ServiceDefaults;

public sealed class RequiredConnectionString
{
    public string Value { get; set; } = string.Empty;
}

public static partial class Extentions
{
    public static TBuilder AddRequiredConnectionString<TBuilder>(this TBuilder builder, string name)
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var key = $"ConnectionStrings:{name}";

        builder.Services.AddOptions<RequiredConnectionString>(name)
            .Configure(options => options.Value = builder.Configuration.GetConnectionString(name) ?? string.Empty)
            .Validate(options => !string.IsNullOrWhiteSpace(options.Value), $"Required configuration key {key} is missing.")
            .ValidateOnStart();
        return builder;
    }

    public static TBuilder AddValidatedOptions<TBuilder, TOptions>(
        this TBuilder builder,
        string sectionName,
        Func<TOptions, bool> validate,
        string failureMessage)
        where TBuilder : IHostApplicationBuilder
        where TOptions : class, new()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);
        ArgumentNullException.ThrowIfNull(validate);
        ArgumentException.ThrowIfNullOrWhiteSpace(failureMessage);

        builder.Services.AddOptions<TOptions>()
            .Bind(builder.Configuration.GetSection(sectionName))
            .Validate(validate, failureMessage)
            .ValidateOnStart();
        return builder;
    }
}
