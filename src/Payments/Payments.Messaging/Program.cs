using Infrastructure.Idempotency;
using Infrastructure.Observability;
using OpenTelemetry.Metrics;
using Payments.Messaging.Consumers;
using ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults()
    .WithMassTransit(messaging =>
    {
        messaging.ReceiveEndpoint<RefundPaymentCommandConsumer>("refund-payment");
    }, typeof(RefundPaymentCommandConsumer).Assembly);

builder.Services.AddOpenTelemetry().WithMetrics(metrics => metrics.AddMeter(PaymentTelemetry.MeterName));
builder.Services.AddSingleton<IPaymentOperationRegistry, PaymentOperationRegistry>();

var app = builder.Build();
app.UseDefaultEndpoints();
app.Run();

public partial class Program { }
