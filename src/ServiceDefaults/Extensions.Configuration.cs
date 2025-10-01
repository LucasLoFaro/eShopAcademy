using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Azure.Identity;


namespace ServiceDefaults;

public static partial class Extentions
{
    public static TBuilder AddDefaultConfiguration<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        if (!builder.Environment.IsDevelopment())
        {
            var appConfigName = Environment.GetEnvironmentVariable("APPCONFIGURATION");

            if (!string.IsNullOrEmpty(appConfigName))
            {
                builder.Configuration.AddAzureAppConfiguration(options =>
                {
                    options.Connect(
                            new Uri($"https://{appConfigName}.azconfig.io"),
                            new DefaultAzureCredential())
                        .ConfigureKeyVault(kv =>
                        {
                            kv.SetCredential(new DefaultAzureCredential());
                        })
                        .Select("common:*", LabelFilter.Null)
                        .Select($"{builder.Environment.ApplicationName}:*", LabelFilter.Null);
                });
            }
        }

        return builder;
    }
}
