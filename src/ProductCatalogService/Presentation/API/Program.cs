using Core.Application.Interfaces.Data;
using Core.Application.Interfaces.Services;
using Core.Application.Services;
using Data;
using Data.Interfaces;
using Data.Repositories;
using Infrastructure.Services;
using ServiceDefaults;


var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults()
       .WithMassTransit()
       .WithSwagger();

builder.Services.AddControllers();

//Inject services
builder.Services.AddSingleton<ICassandraDatabaseClient>(sp => new DataStaxDatabaseClient(builder.Configuration.GetConnectionString("Cassandra")!, "products"));
builder.Services.AddTransient<IProductMessagingService, ProductMessagingService>();
builder.Services.AddTransient<IProductsRepository, ProductsRepository>();
builder.Services.AddTransient<IProductService, ProductService>();


var app = builder.Build();
app.MapControllers();
app.UseDefaultEndpoints();

app.Run();