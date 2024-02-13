using Data;
using Data.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;


var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(s =>
    {
        s.AddSingleton<DatabaseClient>(new DatabaseClient(System.Environment.GetEnvironmentVariable("REDIS_URL")));
        s.AddTransient<IProductCache, ProductCache>();
        
    })
    .Build();

host.Run();
