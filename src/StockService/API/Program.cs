using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            // TODO: Incorporar AutoMapper
            // TODO: Incorporar Serilog
            builder.Services.AddDbContext<StockDbContext>(options =>
                options.UseMongoDB(builder.Configuration.GetConnectionString("MongoDB"), "eShopAcademy")
            );
            builder.Services.AddScoped<IStockRepository, StockRepository>();

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

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