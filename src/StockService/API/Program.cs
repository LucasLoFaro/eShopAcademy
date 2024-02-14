using Microsoft.EntityFrameworkCore;
using Infrastructure.Services.Interfaces;
using Infrastructure.Services.Settings;
using Infrastructure.Services;
using Infrastructure.Data;


namespace API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Add services to the container.
            // TODO: Incorporar AutoMapper
            // TODO: Incorporar Serilog
            builder.Services.AddDbContext<StockDbContext>(options =>
                options.UseMongoDB(builder.Configuration.GetConnectionString("MongoDB"), "eShopAcademy")
            );
            builder.Services.AddScoped<IStockRepository, StockRepository>();
            builder.Services.Configure<RabbitMQSettings>(builder.Configuration.GetSection("RabbitMQSettings"));
            builder.Services.AddTransient<IMessagingServiceClient, RabbitMQClient>();

            var app = builder.Build();
            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }
    }
}