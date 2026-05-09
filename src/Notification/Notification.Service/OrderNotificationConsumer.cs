using Domain.Common.Events.Orders;
using Domain.Notification.Entities;
using Domain.Notification.Enums;
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
            "OrderSubmitted");

        var html = _templateRenderer.Render("OrderSubmitted", BuildPlaceholders(evt));
        await SendAsync(evt.CustomerEmail, $"Order #{evt.OrderId} — Received", html, "OrderSubmitted", evt.OrderId);
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
            notifTitle, notifMessage, templateName);

        var placeholders = BuildPlaceholders(evt);
        placeholders["Amount"] = evt.Amount?.ToString("N2") ?? "";
        placeholders["Currency"] = evt.Currency ?? "USD";
        placeholders["TrackingNumber"] = evt.TrackingNumber ?? "";
        placeholders["Carrier"] = evt.Carrier ?? "";
        placeholders["Reason"] = evt.Reason ?? "No reason provided";

        var html = _templateRenderer.Render(templateName, placeholders);
        await SendAsync(evt.CustomerEmail, subject, html, templateName, evt.OrderId);
    }

    private async Task PersistNotificationAsync(Guid orderId, string email, string name,
        string title, string message, string type)
    {
        try
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

            await _dbContext.Notifications.InsertOneAsync(notification);
            _logger.LogInformation("[Notification] Persisted '{Type}' notification for {Email}, order {OrderId}.",
                type, email, orderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Notification] Failed to persist '{Type}' notification for {Email}, order {OrderId}.",
                type, email, orderId);
        }
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

    private async Task SendAsync(string email, string subject, string html, string template, Guid orderId)
    {
        try
        {
            await _emailSender.SendAsync(email, subject, html);
            _logger.LogInformation("[Notification] Sent '{Template}' email to {Email} for order {OrderId}.", template, email, orderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Notification] Failed to send '{Template}' email to {Email} for order {OrderId}.", template, email, orderId);
        }
    }
}