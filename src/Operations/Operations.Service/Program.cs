using MassTransit;
using Operations.Application.Repositories;
using Operations.Service.Consumers;
using ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddConsole();

builder.AddServiceDefaults()
    .WithMassTransit((context, cfg) =>
    {   
        cfg.ReceiveEndpoint("prepare-package", e =>
            e.ConfigureConsumer<PreparePackageCommandConsumer>(context));
        cfg.ReceiveEndpoint("operations-seller-stamp", e =>
            e.ConfigureConsumer<PackageSellerStampConsumer>(context));
    }, typeof(PreparePackageCommandConsumer).Assembly);

builder.Services.AddSingleton<IPackageRepository, PackageRepository>();

var host = builder.Build();
host.Run();
