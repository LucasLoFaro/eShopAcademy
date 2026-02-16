using Customers.Infrastructure.Data;
using Customers.Messaging.Consumers;
using ServiceDefaults;
using System.Reflection;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults()
       .WithMassTransit(assemblies: Assembly.GetExecutingAssembly());

// Register Customer Repository and Database
builder.Services.AddSingleton(sp =>
    new CustomerDbContext(builder.Configuration.GetConnectionString("customers")!, "customers"));
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();

var host = builder.Build();
host.Run();
