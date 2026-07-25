using FiapGames.Shared.Infrastructure.Migrations;
using FiapGames.Shared.Infrastructure.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;

namespace FiapGames.Shared.Infrastructure.Extensions;

public static class MongoExtensions
{
    public static IServiceCollection AddMongoDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var mongoSettings = configuration.GetSection(MongoSettings.SectionName).Get<MongoSettings>()
            ?? throw new InvalidOperationException($"Missing '{MongoSettings.SectionName}' configuration section.");

        services.Configure<MongoSettings>(configuration.GetSection(MongoSettings.SectionName));

        services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoSettings.ConnectionString));
        services.AddSingleton(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(mongoSettings.DatabaseName));
        services.AddSingleton<MongoMigrationRunner>();

        return services;
    }

    /// <summary>
    /// Registers the hosted service that applies all migrations discovered
    /// across every module at startup. Call once from the API host.
    /// </summary>
    public static IServiceCollection AddMongoMigrations(this IServiceCollection services)
    {
        services.AddHostedService<MongoMigrationsHostedService>();
        return services;
    }
}
