using Infrastructure.Development;

var builder = WebApplication.CreateBuilder(args);

// Determine the execution environment and configure services accordingly.  In
// Development we avoid connecting to Azure services and instead use an in‑memory
// message broker and stub messaging clients.  In other environments we
// configure Azure App Configuration, Key Vault and the real Service Bus client.
if (builder.Environment.IsDevelopment())
{
    Infrastructure.Development.DevelopmentServiceConfiguration.ConfigureServices(builder.Services, builder.Configuration);
}
else
{
    // Load configuration from Azure App Configuration.  This allows the service
    // bus settings to be centrally managed.  Keys prefaced with "common:"
    // apply to all services, while keys prefaced with "order:" apply specifically
    // to the order service.
    builder.Configuration.AddAzureAppConfiguration(options =>
        options.Connect(
            new Uri($"https://{Environment.GetEnvironmentVariable("APPCONFIGURATION")}.azconfig.io"),
            new Azure.Identity.DefaultAzureCredential())
        .ConfigureKeyVault(kv => { kv.SetCredential(new Azure.Identity.DefaultAzureCredential()); })
        .Select("common:*", Microsoft.Extensions.Configuration.AzureAppConfiguration.LabelFilter.Null)
        .Select("order:*", Microsoft.Extensions.Configuration.AzureAppConfiguration.LabelFilter.Null)
        );

    // Register the service bus settings and messaging client used to publish
    // SubmitOrder commands.  The settings section is loaded above from
    // App Configuration.
    builder.Services.Configure<Infrastructure.Services.Settings.ServiceBusSettings>(builder.Configuration.GetSection("common:ServiceBusSettings"));
    builder.Services.AddSingleton<Infrastructure.Services.Interfaces.IOrderMessagingService, Infrastructure.Services.OrderMessageClient>();
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