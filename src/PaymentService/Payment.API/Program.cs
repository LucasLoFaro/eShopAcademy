using Domain.Payment.Contracts;
using Infrastructure.Messaging;
using Infrastructure.Helpers;
using ServiceDefaults;


var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults()
       .WithMassTransit()
       .WithSwagger();

builder.Services.AddScoped<IPaymentMessagingClient, PaymentMessagingClient>();
builder.Services.AddScoped<ISignatureHelper, SignatureHelper>();

var app = builder.Build();

app.UseDefaultEndpoints();

app.MapPost("/api/payments/webhook", async (
    PaymentNotification notification,
    HttpRequest req,
    ISignatureHelper signatureHelper,
    IPaymentMessagingClient messaging,
    CancellationToken ct) =>
{
    //var sig = signatureHelper.SignNotificationRequest(notification); // For testing purposes only

    if (!req.Headers.TryGetValue("X-Signature", out var headerSignature))
        return Results.Unauthorized();

    var isValid = signatureHelper.VerifyWebhookSignature(notification, headerSignature!);
    if (!isValid)
        return Results.Unauthorized();


    switch (notification.Status.ToLowerInvariant())
    {
        case "success":
            await messaging.SendPaymentCompleted(new Guid(notification.ExternalId), notification.Id, ct);
            break;

        case "failed":
        case "cancelled":
            await messaging.SendPaymentFailed(new Guid(notification.ExternalId), notification.Id, notification.FailureReason ?? "Unknown", ct);
            break;

        default:
            Console.WriteLine($"Notification ignored. Order: {notification.ExternalId}. Status: {notification.Status}.");
            break;
    }

    return Results.Ok();
}).WithName("ProcessPaymentNotification");

app.Run();
