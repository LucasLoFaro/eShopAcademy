using gRPC.Services;


var builder = WebApplication.CreateBuilder(args);
builder.Environment.ApplicationName = "product.grpc";

builder.AddServiceDefaults();

builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();

var app = builder.Build();

app.MapGrpcService<ProductGrpcService>();
app.MapGrpcReflectionService();

app.Run();