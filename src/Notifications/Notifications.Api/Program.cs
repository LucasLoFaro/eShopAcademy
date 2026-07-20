using Notifications.Api.Data;
using ServiceDefaults;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults()
       .WithSwagger();

builder.Services.AddProblemDetails();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
        .AllowAnyHeader()
        .AllowAnyMethod()));

var connectionString = builder.Configuration.GetConnectionString("notifications");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("The notifications MongoDB connection string is not configured.");

var notificationDatabase = new NotificationDbContext(connectionString, "notifications");
builder.Services.AddSingleton(notificationDatabase);
builder.Services.AddHealthChecks().AddAsyncCheck(
    "notifications-database",
    async cancellationToken =>
    {
        await notificationDatabase.PingAsync(cancellationToken);
        return HealthCheckResult.Healthy();
    },
    tags: ["ready"],
    timeout: TimeSpan.FromSeconds(3));
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();

var app = builder.Build();
app.UseExceptionHandler();
app.UseCors();

app.MapGet("/notifications", async (string email, INotificationRepository repo, CancellationToken ct) =>
{
    var notifications = await repo.GetByEmailAsync(email, ct);
    return Results.Ok(notifications);
});

app.MapPatch("/notifications/{id:guid}/read", async (Guid id, INotificationRepository repo, CancellationToken ct) =>
{
    var success = await repo.MarkAsReadAsync(id, ct);
    return success ? Results.NoContent() : Results.NotFound();
});

app.MapPost("/notifications/read-all", async (string email, INotificationRepository repo, CancellationToken ct) =>
{
    var count = await repo.MarkAllAsReadAsync(email, ct);
    return Results.Ok(new { markedRead = count });
});

app.UseDefaultEndpoints();

app.Run();

public partial class Program { }
