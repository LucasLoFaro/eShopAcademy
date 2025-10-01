using gRPC.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddGrpc();

var app = builder.Build();
app.UseDefaultEndpoints();

app.MapGrpcService<PaymentService>();
app.MapGet("/", () => "Payment Service Mock");

app.Run();
