using System.Net;
using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ServiceDefaults;

public sealed class WebSecurityOptions
{
    public const string SectionName = "WebSecurity";
    public string[] AllowedCorsOrigins { get; set; } = [];
    public string[] AllowedCorsMethods { get; set; } = ["GET", "HEAD", "OPTIONS"];
    public string[] AllowedCorsHeaders { get; set; } = ["Accept", "Content-Type", "Authorization", "X-Correlation-Id"];
    public bool EnableForwardedHeaders { get; set; }
    public string[] KnownProxies { get; set; } = [];
}

public static class WebServiceDefaultExtensions
{
    public const string RestrictiveCorsPolicy = "platform-restrictive";

    public static WebApplicationBuilder AddWebServiceDefaults(this WebApplicationBuilder builder)
    {
        builder.AddServiceDefaults();
        var security = builder.Configuration.GetSection(WebSecurityOptions.SectionName).Get<WebSecurityOptions>() ?? new();
        ValidateSecurityOptions(security);
        builder.Services.AddSingleton(Options.Create(security));

        builder.Services.AddProblemDetails();
        builder.Services.AddCors(cors => cors.AddPolicy(RestrictiveCorsPolicy, policy =>
        {
            if (security.AllowedCorsOrigins.Length == 0)
            {
                policy.SetIsOriginAllowed(_ => false);
                return;
            }

            policy.WithOrigins(security.AllowedCorsOrigins)
                .WithMethods(security.AllowedCorsMethods)
                .WithHeaders(security.AllowedCorsHeaders);
        }));

        builder.Services.Configure<ForwardedHeadersOptions>(forwarded =>
        {
            forwarded.ForwardedHeaders = security.EnableForwardedHeaders
                ? ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost
                : ForwardedHeaders.None;
            forwarded.ForwardLimit = 1;
            forwarded.KnownProxies.Clear();
            foreach (var proxy in security.KnownProxies)
            {
                forwarded.KnownProxies.Add(IPAddress.Parse(proxy));
            }
        });

        return builder;
    }

    public static WebApplication UseWebServiceDefaults(this WebApplication app)
    {
        var security = app.Services.GetRequiredService<IOptions<WebSecurityOptions>>().Value;
        if (security.EnableForwardedHeaders)
        {
            app.UseForwardedHeaders();
        }

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler();
        }

        app.Use(async (context, next) =>
        {
            context.Response.Headers.XContentTypeOptions = "nosniff";
            context.Response.Headers.XFrameOptions = "DENY";
            context.Response.Headers.ContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'";
            context.Response.Headers["Referrer-Policy"] = "no-referrer";
            context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

            var correlationId = GetCorrelationId(context);
            context.Response.Headers["X-Correlation-Id"] = correlationId;
            Activity.Current?.SetTag("correlation.id", correlationId);
            using (context.RequestServices.GetRequiredService<ILoggerFactory>()
                       .CreateLogger("Correlation")
                       .BeginScope(new Dictionary<string, object?> { ["CorrelationId"] = correlationId }))
            {
                await next(context).ConfigureAwait(false);
            }
        });

        app.UseRouting();
        app.UseCors(RestrictiveCorsPolicy);
        return app;
    }

    private static string GetCorrelationId(HttpContext context)
    {
        var supplied = context.Request.Headers["X-Correlation-Id"].ToString();
        return Guid.TryParse(supplied, out var parsed) ? parsed.ToString("D") : Guid.NewGuid().ToString("D");
    }

    private static void ValidateSecurityOptions(WebSecurityOptions options)
    {
        if (options.AllowedCorsOrigins.Any(origin => origin.Contains('*')))
        {
            throw new OptionsValidationException(
                WebSecurityOptions.SectionName,
                typeof(WebSecurityOptions),
                ["WebSecurity:AllowedCorsOrigins must contain exact origins; wildcards are forbidden."]);
        }

        if (options.EnableForwardedHeaders && options.KnownProxies.Length == 0)
        {
            throw new OptionsValidationException(
                WebSecurityOptions.SectionName,
                typeof(WebSecurityOptions),
                ["WebSecurity:KnownProxies is required when forwarded headers are enabled."]);
        }

        if (options.KnownProxies.Any(proxy => !IPAddress.TryParse(proxy, out _)))
        {
            throw new OptionsValidationException(
                WebSecurityOptions.SectionName,
                typeof(WebSecurityOptions),
                ["WebSecurity:KnownProxies contains an invalid IP address."]);
        }
    }
}
