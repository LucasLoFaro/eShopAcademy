using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults()
       .WithSwagger();

// Authentication - Entra ID JWT Bearer
builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration, "EntraId");

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

builder.Services.AddCors(options =>
{
    options.AddPolicy("spa", policy =>
    {
        policy.WithOrigins(
                builder.Configuration.GetValue<string>("SpaOrigin") ?? "http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

app.UseDefaultEndpoints();
app.UseCors("spa");
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapReverseProxy();

app.Run();
