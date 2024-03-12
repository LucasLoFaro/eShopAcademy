using Application.Interfaces.Data;
using Application.Interfaces.Services;
using Application.Services;
using Azure.Identity;
using Data;
using Data.Interfaces;
using Data.Repositories;
using Data.Settings;
using Microsoft.Extensions.Azure;
using Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddSingleton<IProductsRepository, ProductsRepository>();
builder.Services.AddSingleton<ICassandraDatabaseClient, DataStaxDatabaseClient>();
builder.Services.AddSingleton<IDatabaseSettingsProvider, FileDataBaseSettings>();
builder.Services.AddSingleton<IBlobStorageClient, BlobStorageClient>();
builder.Services.Configure<DatabaseSettings>(builder.Configuration.GetSection("products:Database"));

builder.Services.AddAzureClients(clientBuilder =>
{
    clientBuilder.UseCredential(new DefaultAzureCredential(new DefaultAzureCredentialOptions()));
    clientBuilder.AddBlobServiceClient(new Uri(builder.Configuration["Uris:BlobStorageUri"]));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
