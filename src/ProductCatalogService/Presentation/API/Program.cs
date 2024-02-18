using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Azure.Identity;
using Core.Application.Interfaces.Data;
using Core.Application.Interfaces.Services;
using Core.Application.Services;
using Data;
using Data.Interfaces;
using Data.Repositories;
using Data.Settings;
using Services;
using Services.Settings;


var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Configuration.AddAzureAppConfiguration(options =>
    options.Connect(
        new Uri($"https://{Environment.GetEnvironmentVariable("APPCONFIGURATION")}.azconfig.io"),
        new DefaultAzureCredential())
    .ConfigureKeyVault(kv =>{kv.SetCredential(new DefaultAzureCredential());})
    .Select("common:*", LabelFilter.Null)
    .Select("product:*", LabelFilter.Null)
    );

//Inject services
builder.Services.Configure<DatabaseSettings>(builder.Configuration.GetSection("product:Database"));
builder.Services.AddSingleton<ICassandraDatabaseClient, DataStaxDatabaseClient>();
builder.Services.AddTransient<IProductsRepository, ProductsRepository>();
builder.Services.AddTransient<IProductService, ProductService>();
builder.Services.Configure<RabbitMQSettings>(builder.Configuration.GetSection("common:RabbitMQSettings"));
builder.Services.AddTransient<IMessagingServiceClient, RabbitMQClient>();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
