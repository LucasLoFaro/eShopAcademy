using Data;
using Azure.Identity;
using Data.Interfaces;
using Core.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;


var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddAzureAppConfiguration(options =>
    options.Connect(
        new Uri($"https://{Environment.GetEnvironmentVariable("APPCONFIGURATION")}.azconfig.io"),
        new DefaultAzureCredential())
    .ConfigureKeyVault(kv => { kv.SetCredential(new DefaultAzureCredential()); })
    .Select("common:*", LabelFilter.Null)
    .Select("basket:*", LabelFilter.Null)
    );

//Inject services
builder.Services.Configure<DatabaseSettings>(builder.Configuration.GetSection("basket:Database"));
builder.Services.AddSingleton<DatabaseClient>();
builder.Services.AddTransient<IBasketCache, BasketCache>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();

app.MapGet("/basket/clientId", async Task<IResult> (Guid clientID, IBasketCache basketRepository) =>
{
    var basket = await basketRepository.GetBasketByClientId(clientID);
    return basket != null ? Results.Ok(basket) : Results.NotFound();
});

app.MapPost("/basket/clientId/add", async (Guid clientID, [FromBody] Item item, IBasketCache basketRepository) =>
{
    return await basketRepository.AddProductToBasket(clientID, item) ? Results.Ok() : Results.NotFound();
});

app.MapPost("/basket/clientId/remove", async (Guid clientID, [FromBody] Item item, IBasketCache basketRepository) =>
{
    return await basketRepository.RemoveProductFromBasket(clientID, item) ? Results.Ok() : Results.NotFound();
});

app.Run();