using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Driver;
using NotificationService;
using NotificationService.Data;
using NotificationService.Templates;
using ServiceDefaults;
using OpenTelemetry.Metrics;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenTelemetry().WithMetrics(metrics => metrics.AddMeter(NotificationMetrics.MeterName));

var connectionString = builder.Configuration.GetConnectionString("notifications");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("The notifications MongoDB connection string is not configured.");
if (string.IsNullOrWhiteSpace(builder.Configuration["SendGrid:ApiKey"]))
    throw new InvalidOperationException("The SendGrid API key is not configured.");
if (!Uri.TryCreate(builder.Configuration["SendGrid:FromAddress"] is { Length: > 0 } fromAddress
        ? $"mailto:{fromAddress}"
        : null, UriKind.Absolute, out _))
    throw new InvalidOperationException("The SendGrid from address is not configured as a valid email address.");

var notificationDatabase = new NotificationDbContext(connectionString, "notifications");
builder.Services.AddSingleton(notificationDatabase);
builder.Services.AddSingleton<IMongoClient>(notificationDatabase.Client);
builder.Services.AddSingleton(notificationDatabase.Database);
builder.Services.AddSingleton<IEmailSender, SendGridEmailSender>();
builder.Services.AddSingleton<IEmailTemplateRenderer, EmailTemplateRenderer>();

builder.AddServiceDefaults()
    .WithMassTransit(messaging =>
    {
        messaging.Registration(registration => registration.AddMongoDbOutbox(options =>
        {
            options.ClientFactory(provider => provider.GetRequiredService<IMongoClient>());
            options.DatabaseFactory(provider => provider.GetRequiredService<IMongoDatabase>());
            options.QueryDelay = TimeSpan.FromSeconds(1);
            options.DuplicateDetectionWindow = TimeSpan.FromHours(1);
        }));
    }, typeof(OrderNotificationConsumer).Assembly);

builder.Services.AddHealthChecks().AddAsyncCheck(
    "notifications-database",
    async cancellationToken =>
    {
        await notificationDatabase.PingAsync(cancellationToken);
        return HealthCheckResult.Healthy();
    },
    tags: ["ready"],
    timeout: TimeSpan.FromSeconds(3));

var app = builder.Build();
app.UseDefaultEndpoints();
app.Run();
