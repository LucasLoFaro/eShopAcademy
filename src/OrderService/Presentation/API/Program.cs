using Infrastructure.Services.Setup;

var builder = WebApplication.CreateBuilder(args);

// Determine the execution environment and configure services accordingly.  In
// Development we avoid connecting to Azure services and instead use an in‑memory
// message broker and stub messaging clients.  In other environments we
// configure Azure App Configuration, Key Vault and the real Service Bus client.
if (builder.Environment.IsDevelopment())
{
    // In development use the in-memory transport and local messaging service
    DevelopmentServiceConfiguration.ConfigureServices(builder.Services, builder.Configuration);
}
else
{
    // In production, load configuration from Azure App Configuration and register
    // the Azure Service Bus based messaging service.
    builder.Configuration.AddAzureAppConfiguration(options =>
        options.Connect(
            new Uri($"https://{Environment.GetEnvironmentVariable("APPCONFIGURATION")}.azconfig.io"),
            new Azure.Identity.DefaultAzureCredential())
        .ConfigureKeyVault(kv => { kv.SetCredential(new Azure.Identity.DefaultAzureCredential()); })
        .Select("common:*", Microsoft.Extensions.Configuration.AzureAppConfiguration.LabelFilter.Null)
        .Select("order:*", Microsoft.Extensions.Configuration.AzureAppConfiguration.LabelFilter.Null)
        );

    builder.Services.Configure<Infrastructure.Services.Settings.ServiceBusSettings>(builder.Configuration.GetSection("common:ServiceBusSettings"));
    ProductionServiceConfiguration.ConfigureServices(builder.Services, builder.Configuration);
}

// Add services to the container.
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