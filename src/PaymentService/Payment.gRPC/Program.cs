using Services;
using Infrastructure.Helpers;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddGrpc();
builder.Services.AddScoped<ISignatureHelper, SignatureHelper>();

var pspUrl = builder.Configuration.GetValue<string>("WireMock:BaseUrl");
builder.Services.AddHttpClient<PaymentService>(client => { client.BaseAddress = new Uri(pspUrl!); });

var app = builder.Build();

app.UseDefaultEndpoints();

app.MapGrpcService<PaymentService>();

app.Run();
