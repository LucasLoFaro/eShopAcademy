using Infrastructure.Helpers;
using Infrastructure.Messaging;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using ServiceDefaults;
using Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults()
       .WithMassTransit();

builder.Services.AddGrpc();
builder.Services.AddScoped<ISignatureHelper, SignatureHelper>();
builder.Services.AddTransient<IPaymentMessagingClient, PaymentMessagingClient>();

var pspUrl = builder.Configuration.GetConnectionString("external-services-mocks")!;
//var pspUrl = "http://external-services-mocks:8080";
builder.Services.AddHttpClient<PaymentService>(client => { client.BaseAddress = new Uri(pspUrl); });
builder.WebHost.ConfigureKestrel(o =>
{
    o.ConfigureEndpointDefaults(lo => lo.Protocols = HttpProtocols.Http2);
});

var app = builder.Build();

app.UseDefaultEndpoints();
app.MapGrpcService<PaymentService>();
app.MapGet("/", () => "OK");
app.Run();
