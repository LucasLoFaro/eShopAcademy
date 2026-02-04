using Domain.Common.Events.Orders;
using MassTransit;

namespace NotificationService;

public class OrderNotificationConsumer : IConsumer<OrderSubmittedEvent>, 
                                         IConsumer<OrderConfirmedEvent>,
                                         IConsumer<OrderCompletedEvent>,
                                         IConsumer<OrderExpiredEvent>,
                                         IConsumer<OrderCancelledEvent>
{
    private readonly IEmailSender _emailSender;
    private readonly ILogger<OrderNotificationConsumer> _logger;

    public OrderNotificationConsumer(IEmailSender emailSender, ILogger<OrderNotificationConsumer> logger)
    {
        _emailSender = emailSender;
        _logger = logger;
    }

    public Task Consume(ConsumeContext<OrderSubmittedEvent> context) => HandleNotification(context);
    public Task Consume(ConsumeContext<OrderConfirmedEvent> context) => HandleNotification(context);
    public Task Consume(ConsumeContext<OrderCompletedEvent> context) => HandleNotification(context);
    public Task Consume(ConsumeContext<OrderExpiredEvent> context) => HandleNotification(context);
    public Task Consume(ConsumeContext<OrderCancelledEvent> context) => HandleNotification(context);

    private async Task HandleNotification<T>(ConsumeContext<T> context)
        where T : OrderEvent
    {
        var evt = context.Message;
        _logger.LogInformation("[Notification] {Event} received for Order {OrderId}, Customer: {Email}",
            typeof(T).Name, evt.OrderId, evt.CustomerEmail);

        var orderNumber = evt.OrderId.ToString();
        var status = evt.EventType switch
        {
            nameof(OrderSubmittedEvent) => "Submitted",
            nameof(OrderConfirmedEvent) => "Confirmed",
            nameof(OrderCompletedEvent) => "Completed",
            nameof(OrderExpiredEvent) => "Expired",
            nameof(OrderCancelledEvent) => "Cancelled",
            _ => "Updated"
        };

        await _emailSender.SendStatusUpdateAsync(evt.CustomerEmail, orderNumber, status);
        _logger.LogInformation("Email sent to {Email} for {Status}", evt.CustomerEmail, status);
    }    
}