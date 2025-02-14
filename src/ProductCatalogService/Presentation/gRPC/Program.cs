using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Azure.Identity;
using gRPC.Services;


var builder = WebApplication.CreateBuilder(args);
builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();

var app = builder.Build();

builder.Configuration.AddAzureAppConfiguration(options =>
    options.Connect(
        new Uri($"https://{Environment.GetEnvironmentVariable("APPCONFIGURATION")}.azconfig.io"),
        new DefaultAzureCredential())
    .ConfigureKeyVault(kv => { kv.SetCredential(new DefaultAzureCredential()); })
    .Select("common:*", LabelFilter.Null)
    .Select("product:*", LabelFilter.Null)
    );

app.MapGrpcService<ProductGrpcService>();
app.MapGrpcReflectionService();

app.Run();