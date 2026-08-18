using Domain.Common.Events.Orders;
using Domain.Notifications.Entities;
using Domain.Notifications.Enums;
using MassTransit;
using NotificationService.Data;
using NotificationService.Templates;

namespace NotificationService;

public class OrderNotificationConsumer : IConsumer<OrderSubmittedEvent>,
IConsumer<OrderStatusUpdatedEvent>
{
    private readonly IEmailSender _emailSender;
    private readonly IEmailTemplateRenderer _templateRenderer;
    private readonly NotificationDbContext _dbContext;
    private readonly ILogger<OrderNotificationConsumer> _logger;

    public OrderNotificationConsumer(
        IEmailSender emailSender,
        IEmailTemplateRenderer templateRenderer,
        NotificationDbContext dbContext,
        ILogger<OrderNotificationConsumer> logger)
    {
        _emailSender = emailSender;
        _templateRenderer = templateRenderer;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderSubmittedEvent> context)
    {
        var evt = context.Message;
        if (!HasEmail(evt)) return;

        await PersistNotificationAsync(evt.OrderId, evt.CustomerEmail, evt.CustomerName,
            "Order Received",
            $"Your order #{evt.OrderId} has been received and is being processed.",
            "OrderSubmitted", context.CancellationToken);

        var html = _templateRenderer.Render("OrderSubmitted", BuildPlaceholders(evt));
        await SendAsync(evt.CustomerEmail, $"Order #{evt.OrderId} — Received", html, "OrderSubmitted", evt.OrderId, context.CancellationToken);
    }

    public async Task Consume(ConsumeContext<OrderStatusUpdatedEvent> context)
    {
        var evt = context.Message;
        if (!HasEmail(evt)) return;

        var (templateName, subject, notifTitle, notifMessage) = evt.Status switch
        {
            "Paid"           => ("PaymentConfirmed", $"Order #{evt.OrderId} — Payment Confirmed",
                                 "Payment Confirmed", $"Payment for order #{evt.OrderId} has been confirmed."),
            "ReadyForPickup" => ("ReadyForPickup",   $"Order #{evt.OrderId} — Ready for Pickup",
                                 "Ready for Pickup",  $"Your order #{evt.OrderId} is ready for pickup."),
            "Shipped"        => ("OrderShipped",      $"Order #{evt.OrderId} — Shipped",
                                 "Order Shipped",     $"Your order #{evt.OrderId} has been shipped."),
            "Delivered"      => ("OrderDelivered",    $"Order #{evt.OrderId} — Delivered",
                                 "Order Delivered",   $"Your order #{evt.OrderId} has been delivered."),
            "Cancelled"      => ("OrderCancelled",    $"Order #{evt.OrderId} — Cancelled",
                                 "Order Cancelled",   $"Your order #{evt.OrderId} has been cancelled."),
            _ => (string.Empty, string.Empty, string.Empty, string.Empty)
        };

        if (string.IsNullOrEmpty(templateName))
        {
            _logger.LogWarning("[Notification] No template for status '{Status}' on order {OrderId}.", evt.Status, evt.OrderId);
            return;
        }

        await PersistNotificationAsync(evt.OrderId, evt.CustomerEmail, evt.CustomerName,
            notifTitle, notifMessage, templateName, context.CancellationToken);

        var placeholders = BuildPlaceholders(evt);
        placeholders["Amount"] = evt.Amount?.ToString("N2") ?? "";
        placeholders["Currency"] = evt.Currency ?? "USD";
        placeholders["TrackingNumber"] = evt.TrackingNumber ?? "";
        placeholders["Carrier"] = evt.Carrier ?? "";
        placeholders["Reason"] = evt.Reason ?? "No reason provided";

        var html = _templateRenderer.Render(templateName, placeholders);
        await SendAsync(evt.CustomerEmail, subject, html, templateName, evt.OrderId, context.CancellationToken);
    }

    private async Task PersistNotificationAsync(Guid orderId, string email, string name,
        string title, string message, string type, CancellationToken cancellationToken)
    {
        var notification = new NotificationMessage
        {
                Recipient = new NotificationRecipient
                {
                    Name = string.IsNullOrWhiteSpace(name) ? "Customer" : name,
                    Email = email
                },
                Channel = NotificationChannel.InApp,
                Subject = title,
                Body = message,
                Status = NotificationStatus.Sent,
                SentAt = DateTime.UtcNow,
                OrderId = orderId,
                Type = type,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow
        };

        try
        {
            await _dbContext.Notifications.InsertOneAsync(notification, cancellationToken: cancellationToken);
        }
        catch (Exception exception)
        {
            NotificationMetrics.RecordFailure("in-app", exception is MongoDB.Driver.MongoException ? "database" : "unexpected");
            NotificationMetrics.RecordConsumerRetry(nameof(OrderNotificationConsumer), "persistence");
            throw;
        }
        _logger.LogInformation("Persisted notification type {Type} for order {OrderId}.", type, orderId);
    }

    private bool HasEmail(OrderEvent evt)
    {
        if (!string.IsNullOrWhiteSpace(evt.CustomerEmail)) return true;

        _logger.LogWarning("[Notification] Skipping {Event} for order {OrderId} — missing email.",
            evt.GetType().Name, evt.OrderId);
        return false;
    }

    private static Dictionary<string, string> BuildPlaceholders(OrderEvent evt) => new()
    {
        ["CustomerName"] = string.IsNullOrWhiteSpace(evt.CustomerName) ? "Customer" : evt.CustomerName,
        ["OrderNumber"] = evt.OrderId.ToString(),
        ["Date"] = DateTime.UtcNow.ToString("MMMM dd, yyyy")
    };

    private async Task SendAsync(string email, string subject, string html, string template, Guid orderId, CancellationToken cancellationToken)
    {
        try
        {
            await _emailSender.SendAsync(email, subject, html, cancellationToken);
            _logger.LogInformation("Sent notification template {Template} for order {OrderId}.", template, orderId);
        }
        catch (Exception exception)
        {
            NotificationMetrics.RecordFailure("email", exception is HttpRequestException ? "provider" : "unexpected");
            NotificationMetrics.RecordConsumerRetry(nameof(OrderNotificationConsumer), "email");
            _logger.LogWarning(exception, "[Notification] Email send failed for template {Template}, order {OrderId}. Notification was persisted and email delivery will be retried.", template, orderId);
            throw;
        }
    }
}
