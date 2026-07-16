using Payments.Messaging;
using Payments.Messaging.Consumers;
using ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults()
       .WithMassTransit(messaging =>
        {
            messaging.ReceiveEndpoint<RefundPaymentCommandConsumer>("refund-payment");
        }, typeof(RefundPaymentCommandConsumer).Assembly);

var host = builder.Build();
host.Run();
