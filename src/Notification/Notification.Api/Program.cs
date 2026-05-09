using Notification.Api.Data;
using ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults()
       .WithSwagger();

builder.Services.AddSingleton(
    new NotificationDbContext(builder.Configuration.GetConnectionString("notifications")!, "notifications"));
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();

var app = builder.Build();

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
