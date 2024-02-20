using Application.IntegrationEvents.Messages;
using Data;
using Data.Interfaces;
using Domain.Entities;
using MassTransit;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

//Inject services
builder.Services.Configure<DatabaseSettings>(builder.Configuration.GetSection("Database"));
builder.Services.AddSingleton<DatabaseClient>();
builder.Services.AddTransient<IBasketCache, BasketCache>();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<StockChangedEvent>();
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.ConfigureEndpoints(context);
    });
});
builder.Services.AddMassTransitHostedService();

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