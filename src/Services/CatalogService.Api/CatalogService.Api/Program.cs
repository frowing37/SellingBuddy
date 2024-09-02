using CatalogService;
using CatalogService.Extensions;
using CatalogService.Infrastructure.Context;
using Microsoft.AspNetCore;
using Microsoft.Extensions.Logging;
using Serilog;

public class Program
{
    private static string env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
    private static IConfiguration SerilogConfiguration
    {
        get
        {
            return new ConfigurationBuilder()
                .SetBasePath(System.IO.Directory.GetCurrentDirectory())
                .AddJsonFile($"Configurations/serilog.json", optional: false)
                .AddJsonFile($"Configurations/serilog.{env}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();
        }
    }
    public static IWebHost CreateHostBuilder(string[] args)
    {
        return WebHost.CreateDefaultBuilder()
            .UseDefaultServiceProvider((context, options) =>
            {
                options.ValidateOnBuild = false;
                options.ValidateScopes = false;
            })
            //.ConfigureAppConfiguration(i => i.AddConfiguration(configuration))
            .UseStartup<Startup>()
            .UseWebRoot("Pics")
            //.ConfigureLogging(i => i.ClearProviders())
            //.UseSerilog()
            .UseContentRoot(Directory.GetCurrentDirectory())
            .Build();
    }
    public static void Main(string[] args)
    {
        var host = CreateHostBuilder(args);

        host.MigrateDbContext<CatalogContext>((context, services) =>
        {
            var env = services.GetService<IWebHostEnvironment>();
            var logger = services.GetService<ILogger<CatalogContextSeed>>();

            new CatalogContextSeed()
                .SeedAsync(context, env, logger)
                .Wait();
        });

        // Log.Logger = new LoggerConfiguration()
        //     .ReadFrom.Configuration(SerilogConfiguration)
        //     .CreateLogger();
        //
        // Log.Logger.Information("Application is Running....");

        host.Run();
    }
}