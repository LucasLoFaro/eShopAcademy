using gRPC.Services;
using Infrastructure.Data;


var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddProductStorage(builder.Configuration);
builder.Services.AddTransient<Core.Application.Interfaces.Services.IProductService, gRPC.Services.ProductQueryService>();

builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();

var app = builder.Build();

app.MapGrpcService<ProductGrpcService>();
app.MapGrpcReflectionService();
app.UseDefaultEndpoints();

app.Run();

public partial class Program;
