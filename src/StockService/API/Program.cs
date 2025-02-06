using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Infrastructure.Services.Interfaces;
using Infrastructure.Services.Settings;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Services;
using Infrastructure.Data;
using Azure.Identity;



namespace API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Configuration.AddAzureAppConfiguration(options =>
                options.Connect(
                    new Uri($"https://{Environment.GetEnvironmentVariable("APPCONFIGURATION")}.azconfig.io"),
                    new DefaultAzureCredential())
                .ConfigureKeyVault(kv => { kv.SetCredential(new DefaultAzureCredential()); })
                .Select("common:*", LabelFilter.Null)
                .Select("stock:*", LabelFilter.Null)
                );

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Add services to the container.
            // TODO: Add AutoMapper
            // TODO: Add Serilog
            builder.Services.AddDbContext<StockDbContext>(options =>
                options.UseMongoDB(builder.Configuration["stock:ConnectionStrings:MongoDB"], "eShopAcademy")
            );
            builder.Services.AddScoped<IStockRepository, StockRepository>();
            builder.Services.Configure<RabbitMQSettings>(builder.Configuration.GetSection("common:RabbitMQSettings"));
            //builder.Services.AddTransient<IMessagingServiceClient, RabbitMQClient>();

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