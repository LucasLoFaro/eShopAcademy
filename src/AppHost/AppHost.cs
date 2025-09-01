using Microsoft.Extensions.Hosting;
using AppHost.Setup;
using Aspire.Hosting.MongoDB;
using Aspire.Hosting.Postgres;
using Aspire.Hosting.Redis;

var builder = DistributedApplication.CreateBuilder(args);

// Add API Gateway or top-level API if present
var api = builder.AddProject<Projects.API>("api");

if(builder.Environment.IsDevelopment())
    EnvironmentSetup.AddProjectDependencies(builder, api);


// Build and run the distributed application
builder.Build().Run();
