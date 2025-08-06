using Infrastructure.Services.Setup;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
if (builder.Environment.IsDevelopment())
{
    DevelopmentServiceConfiguration.ConfigureServices(builder.Services, builder.Configuration);
}
else
{
    builder.Configuration.AddAzureAppConfiguration(options =>
    options.Connect(
        new Uri($"https://{Environment.GetEnvironmentVariable("APPCONFIGURATION")}.azconfig.io"),
        new Azure.Identity.DefaultAzureCredential())
    .ConfigureKeyVault(kv => { kv.SetCredential(new Azure.Identity.DefaultAzureCredential()); })
    .Select("common:*", Microsoft.Extensions.Configuration.AzureAppConfiguration.LabelFilter.Null)
    .Select("order:*", Microsoft.Extensions.Configuration.AzureAppConfiguration.LabelFilter.Null)
    );

    ProductionServiceConfiguration.ConfigureServices(builder.Services, builder.Configuration);
}

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }