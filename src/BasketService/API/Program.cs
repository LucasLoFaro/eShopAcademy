using Data;
using Data.Interfaces;
using Domain;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

//Inject services
builder.Services.Configure<DatabaseSettings>(builder.Configuration.GetSection("Database"));
builder.Services.AddSingleton<DatabaseClient>();
builder.Services.AddTransient<IBasketRepository, BasketRepository>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();

app.MapGet("/basket/clientId", (Guid clientID, IBasketRepository basketRepository) =>
{
    var basket = basketRepository.GetBasketByClientId(clientID);
    return basket != null ? Results.Ok(basket) : Results.NotFound();
});

app.MapPost("/basket/clientId/add", (Guid clientID, [FromBody] Item item, IBasketRepository basketRepository) =>
{
    return basketRepository.AddProductToBasket(clientID, item) == 0 ? Results.Ok() : Results.NotFound();
});

app.MapPost("/basket/clientId/remove", (Guid clientID, [FromBody] Item item, IBasketRepository basketRepository) =>
{
    return basketRepository.RemoveProductFromBasket(clientID, item) == 0 ? Results.Ok() : Results.NotFound();
});

app.Run();