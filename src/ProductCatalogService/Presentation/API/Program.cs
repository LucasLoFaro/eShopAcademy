using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Core.Application.Interfaces.Services;
using Core.Application.Interfaces.Data;
using Infrastructure.Services.Settings;
using Core.Application.Services;
using Infrastructure.Services;
using Data.Repositories;
using Data.Interfaces;
using Azure.Identity;
using Data.Settings;
using Data;


var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Configuration.AddAzureAppConfiguration(options =>
    options.Connect(
        new Uri($"https://{Environment.GetEnvironmentVariable("APPCONFIGURATION")}.azconfig.io"),
        new DefaultAzureCredential())
    .ConfigureKeyVault(kv => { kv.SetCredential(new DefaultAzureCredential()); })
    .Select("common:*", LabelFilter.Null)
    .Select("product:*", LabelFilter.Null)
    );

//Inject services
builder.Services.Configure<DatabaseSettings>(builder.Configuration.GetSection("product:Database"));
builder.Services.AddSingleton<ICassandraDatabaseClient, DataStaxDatabaseClient>();
builder.Services.AddTransient<IProductsRepository, ProductsRepository>();
builder.Services.AddTransient<IProductService, ProductService>();
builder.Services.Configure<ServiceBusSettings>(builder.Configuration.GetSection("common:ServiceBusSettings"));
builder.Services.AddTransient<IMessagingServiceClient, ServiceBusClient>();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
