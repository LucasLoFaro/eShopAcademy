using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

public static partial class Extensions
{
    public static TBuilder WithSwagger<TBuilder>(
        this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            //c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            //{
            //    In = ParameterLocation.Header,
            //    Description = "Please insert JWT with Bearer into field",
            //    Name = "Authorization",
            //    Type = SecuritySchemeType.ApiKey
            //});

            //c.AddSecurityRequirement(new OpenApiSecurityRequirement
            //{
            //    {
            //        new OpenApiSecurityScheme
            //        {
            //            Reference = new OpenApiReference
            //            {
            //                Type = ReferenceType.SecurityScheme,
            //                Id = "Bearer"
            //            }
            //        },
            //        Array.Empty<string>()
            //    }
            //});
        });

        return builder;
    }
}
