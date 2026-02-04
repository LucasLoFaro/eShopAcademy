using MassTransit;
using Operations.Service.Consumers;
using ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddConsole();

builder.AddServiceDefaults()
    .WithMassTransit((context, cfg) =>
    {   
        cfg.ReceiveEndpoint("prepare-package", e =>
            e.ConfigureConsumer<PreparePackageCommandConsumer>(context));
    }, typeof(PreparePackageCommandConsumer).Assembly);

var host = builder.Build();
host.Run();
