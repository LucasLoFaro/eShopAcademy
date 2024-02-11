using Application.Interfaces.Data;
using Data;
using Data.Interfaces;
using Data.Repositories;
using Data.Settings;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//Inject services
builder.Services.Configure<DatabaseSettings>(builder.Configuration.GetSection("Database"));
builder.Services.AddSingleton<ICassandraDatabaseClient, DataStaxDatabaseClient>();
builder.Services.AddTransient<IProductsRepository, ProductsRepository>();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
