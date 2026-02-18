using Domain.Common.Events.Orders;
using MassTransit;
using NotificationService.Templates;

namespace NotificationService;

public class OrderNotificationConsumer : IConsumer<OrderSubmittedEvent>,
IConsumer<OrderStatusUpdatedEvent>
{
    private readonly IEmailSender _emailSender;
    private readonly IEmailTemplateRenderer _templateRenderer;
    private readonly ILogger<OrderNotificationConsumer> _logger;

    public OrderNotificationConsumer(
        IEmailSender emailSender,
        IEmailTemplateRenderer templateRenderer,
        ILogger<OrderNotificationConsumer> logger)
    {
        _emailSender = emailSender;
        _templateRenderer = templateRenderer;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderSubmittedEvent> context)
    {
        var evt = context.Message;
        if (!HasEmail(evt)) return;

        var html = _templateRenderer.Render("OrderSubmitted", BuildPlaceholders(evt));
        await SendAsync(evt.CustomerEmail, $"Order #{evt.OrderId} — Received", html, "OrderSubmitted", evt.OrderId);
    }

    public async Task Consume(ConsumeContext<OrderStatusUpdatedEvent> context)
    {
        var evt = context.Message;
        if (!HasEmail(evt)) return;

        var (templateName, subject) = evt.Status switch
        {
            "Paid"           => ("PaymentConfirmed", $"Order #{evt.OrderId} — Payment Confirmed"),
            "ReadyForPickup" => ("ReadyForPickup",   $"Order #{evt.OrderId} — Ready for Pickup"),
            "Shipped"        => ("OrderShipped",      $"Order #{evt.OrderId} — Shipped"),
            "Delivered"      => ("OrderDelivered",    $"Order #{evt.OrderId} — Delivered"),
            "Cancelled"      => ("OrderCancelled",    $"Order #{evt.OrderId} — Cancelled"),
            _ => (string.Empty, string.Empty)
        };

        if (string.IsNullOrEmpty(templateName))
        {
            _logger.LogWarning("[Notification] No template for status '{Status}' on order {OrderId}.", evt.Status, evt.OrderId);
            return;
        }

        var placeholders = BuildPlaceholders(evt);
        placeholders["Amount"] = evt.Amount?.ToString("N2") ?? "";
        placeholders["Currency"] = evt.Currency ?? "USD";
        placeholders["TrackingNumber"] = evt.TrackingNumber ?? "";
        placeholders["Carrier"] = evt.Carrier ?? "";
        placeholders["Reason"] = evt.Reason ?? "No reason provided";

        var html = _templateRenderer.Render(templateName, placeholders);
        await SendAsync(evt.CustomerEmail, subject, html, templateName, evt.OrderId);
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