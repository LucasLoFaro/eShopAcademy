using Microsoft.Extensions.Hosting;
using AppHost.Setup;


var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.API>("api");

if(builder.Environment.IsDevelopment())
    SetupDependencies.AddDevelopmentEnvironment(builder, api);
    
// Add Service Bus emulator
var serviceBus = builder.AddContainer("servicebus", "mcr.microsoft.com/azure-messaging/servicebus-emulator", "latest")
    .WithEndpoint(port: 5672, targetPort: 5672, name: "amqp")  // AMQP
    .WithEndpoint(port: 8081, targetPort: 8081, name: "http"); // Management


// Add Postgres
var postgres = builder.AddPostgres("eshopacademy-ordersdb")
    .AddDatabase("orders");

// Add OrderService, wiring it to Service Bus + Postgres
//builder.AddProject<Projects.OrderService>("orderservice")
//    .WithReference(serviceBus)
//    .WithReference(postgres);


builder.Build().Run();