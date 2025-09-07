using gRPC.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Environment.ApplicationName = "payments.grpc";

builder.AddServiceDefaults();

builder.Services.AddGrpc();

var app = builder.Build();
app.MapDefaultEndpoints();

app.MapGrpcService<PaymentService>();
app.MapGet("/", () => "Payment Service Mock");

app.Run();
