var builder = WebApplication.CreateBuilder(args);

builder.Environment.ApplicationName = "shipping.grpc";

builder.AddServiceDefaults();
builder.Services.AddGrpc();

var app = builder.Build();

app.MapGrpcService<ShippingService>();
app.MapGet("/", () => "Shipping service mock");

app.Run();
