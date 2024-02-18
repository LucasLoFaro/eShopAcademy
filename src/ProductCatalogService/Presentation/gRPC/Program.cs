using Azure.Identity;
using gRPC.Services;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddGrpc();

var app = builder.Build();

builder.Configuration.AddAzureAppConfiguration(options =>
    options.Connect(
        new Uri($"https://{Environment.GetEnvironmentVariable("APPCONFIGURATION")}.azconfig.io"),
        new DefaultAzureCredential())
    .ConfigureKeyVault(kv => { kv.SetCredential(new DefaultAzureCredential()); })
    .Select("common:*", LabelFilter.Null)
    .Select("product:*", LabelFilter.Null)
    );

// Configure the HTTP request pipeline.
app.MapGrpcService<GreeterService>();
//remove this
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();
