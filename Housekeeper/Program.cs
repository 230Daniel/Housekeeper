using System;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot.Hosting;
using Disqord.Gateway;
using Disqord.Gateway.Default;
using Housekeeper.Database;
using Housekeeper.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Housekeeper;

internal static class Program
{
    private static async Task Main()
    {
        var host = Host.CreateDefaultBuilder()
            .UseSystemd()
            .UseSerilog()
            .ConfigureServices(ConfigureServices)
            .ConfigureDiscordBot<HousekeeperBot>((context, bot) =>
            {
                bot.Token = context.Configuration.GetValue<string>("Discord:Token");
                bot.ReadyEventDelayMode = ReadyEventDelayMode.Guilds;
                bot.Intents |= GatewayIntents.Members;
            })
            .Build();

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(host.Services.GetRequiredService<IConfiguration>())
            .CreateLogger();

        try
        {
            Log.Information("Migrating database");
            using (var scope = host.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
                await db.Database.MigrateAsync();
            }

            Log.Information("Running host");
            await host.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Crashed");
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    private static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        services.AddSingleton(typeof(Microsoft.Extensions.Logging.ILogger<>), typeof(Logger<>));
        // services.AddSingleton<JobService>();
        services.AddDbContext<DatabaseContext>();
        services.Configure<DefaultGatewayCacheProviderConfiguration>(x => x.MessagesPerChannel = 1);
    }
}
