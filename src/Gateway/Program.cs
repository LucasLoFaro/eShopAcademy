using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults()
       .WithSwagger();
builder.Services.AddProblemDetails();

// Authentication - Entra ID JWT Bearer
builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration, "EntraId");

// Allow token from query string for SSE (EventSource doesn't support custom headers)
builder.Services.Configure<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions>(
    Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme,
    options =>
    {
        var existingOnMessageReceived = options.Events?.OnMessageReceived;
        options.Events ??= new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents();
        options.Events.OnMessageReceived = async context =>
        {
            if (string.IsNullOrEmpty(context.Token)
                && context.Request.Query.TryGetValue("access_token", out var token)
                && !string.IsNullOrEmpty(token))
            {
                context.Token = token;
            }

            if (existingOnMessageReceived != null)
                await existingOnMessageReceived(context);
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("consumer", policy => policy.RequireAuthenticatedUser())
    .AddPolicy("admin", policy => policy.RequireRole("admin"));

builder.Services.AddReverseProxy()
.LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
.AddServiceDiscoveryDestinationResolver();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("fixed", limiter =>
    {
        limiter.PermitLimit = 100;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiter.QueueLimit = 10;
    });
});

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? (builder.Environment.IsDevelopment() ? ["http://localhost:5173", "http://localhost:5174"] : []);
builder.Services.AddCors(options => options.AddPolicy("gateway", policy =>
    policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseExceptionHandler();
app.UseRouting();
app.UseCors("gateway");
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.UseDefaultEndpoints();
app.MapReverseProxy();

app.Run();

public partial class Program { }
