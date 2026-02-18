using MassTransit;
using Payments.Messaging;
using Payments.Messaging.Consumers;
using ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults()
       .WithMassTransit((context, cfg) =>
        {
            cfg.ReceiveEndpoint("refund-payment", e =>
                e.ConfigureConsumer<RefundPaymentCommandConsumer>(context));
        }, typeof(RefundPaymentCommandConsumer).Assembly);

var host = builder.Build();
host.Run();
