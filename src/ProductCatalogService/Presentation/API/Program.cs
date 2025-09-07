using Core.Application.Interfaces.Services;
using Core.Application.Interfaces.Data;
using Core.Application.Services;
using Data.Repositories;
using Data.Interfaces;
using Data;


var builder = WebApplication.CreateBuilder(args);
builder.Environment.ApplicationName = "product.api";

builder.AddServiceDefaults()
       .AddSwagger();

//Inject services
builder.Services.AddSingleton<ICassandraDatabaseClient>(sp => new DataStaxDatabaseClient(builder.Configuration.GetConnectionString("Cassandra")!, "products"));
builder.Services.AddTransient<IProductsRepository, ProductsRepository>();
builder.Services.AddTransient<IProductService, ProductService>();


var app = builder.Build();
app.MapDefaultEndpoints();

app.Run();