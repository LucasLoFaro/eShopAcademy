using Application.Interfaces.Data;
using Application.Interfaces.Services;
using Application.Services;
using Data;
using Data.Interfaces;
using Data.Repositories;
using Data.Settings;
using Domain.Entities;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IProductsRepository, ProductsRepository>();
builder.Services.AddSingleton<ICassandraDatabaseClient, DataStaxDatabaseClient>();
builder.Services.AddSingleton<IDatabaseSettingsProvider, FileDataBaseSettings>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
