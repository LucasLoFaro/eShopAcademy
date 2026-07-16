using Operations.Application.Repositories;
using Operations.Service.Consumers;
using ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddConsole();

builder.AddServiceDefaults()
    .WithMassTransit(messaging =>
    {
        messaging.ReceiveEndpoint<PreparePackageCommandConsumer>("prepare-package");
        messaging.ReceiveEndpoint<PackageSellerStampConsumer>("operations-seller-stamp");
    }, typeof(PreparePackageCommandConsumer).Assembly);

builder.Services.AddSingleton<IPackageRepository, PackageRepository>();

var host = builder.Build();
host.Run();
