using Microsoft.EntityFrameworkCore;
using Infrastructure.Services;
using Infrastructure.Data;
using ServiceDefaults;


namespace API;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Environment.ApplicationName = "stock.api";

        builder.AddServiceDefaults()
               .AddSwagger()
               .WithMassTransit();

        builder.Services.AddDbContext<StockDbContext>(options =>
            options.UseMongoDB(builder.Configuration.GetConnectionString("mongodb"), "stock")
        );
        builder.Services.AddScoped<IStockRepository, StockRepository>();
        
        builder.Services.AddTransient<StockMessagingClient>();

        var app = builder.Build();
        app.MapDefaultEndpoints();
        app.Run();
    }
}